using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFlow;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// Owns the living enemies and their behaviour (chase, attack, take damage). EnemyWaveSpawner decides when, what and where they spawn.
	public class EnemiesController
	{
		// Lunging enemies stop this far from where the hero stood, so the bodies do not overlap.
		private const float LungeStopDistance = 0.8f;

		private HeroController _heroController;
		private GameFlowService _gameFlowService;

		// Events
		public event Action<EnemyState> OnEnemySpawned;
		public event Action<int> OnEnemyRemoved;
		public event Action<EnemyState> OnEnemyPositionChanged;
		public event Action<int, float> OnEnemyWindupStarted; // enemy id, windup duration: hero in range, the enemy telegraphs its attack
		public event Action<int> OnEnemyStrikeStarted; // windup over, the attack animation starts
		public event Action<int> OnEnemyAttacked; // the strike ended and the hero was still in range: he took the hit
		public event Action<int, int, bool> OnEnemyDamaged; // enemy id, damage dealt, was it a critical strike: hit by the hero and survived
		public event Action<int, int, bool> OnEnemyKilled; // enemy id, damage dealt by the killing blow, was it a critical strike: raised just before OnEnemyRemoved
		public event Action<int> OnEnemyExploded; // a kamikaze enemy reached the hero and hit him: it dies without being killed, raised just before OnEnemyRemoved

		// State
		private Dictionary<int, EnemyState> _enemies;
		private CancellationTokenSource _cancellationTokenSource;
		private int _nextEnemyId;

		public IReadOnlyDictionary<int, EnemyState> Enemies => _enemies;

		public UniTask<bool> Initialize(HeroController heroController, GameFlowService gameFlowService)
		{
			_heroController = heroController;
			_gameFlowService = gameFlowService;

			_enemies = new Dictionary<int, EnemyState>();
			_nextEnemyId = 0;
			_cancellationTokenSource = new CancellationTokenSource();

			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_enemies.Clear();

			return UniTask.CompletedTask;
		}

		public void SpawnEnemy(EnemyConfig enemyConfig, Vector3 position)
		{
			int enemyId = _nextEnemyId++;
			EnemyState newEnemy = new EnemyState(enemyId, position, enemyConfig.InitialHealth, enemyConfig);

			_enemies[enemyId] = newEnemy;
			OnEnemySpawned?.Invoke(newEnemy);
		}

		public void ClearAllEnemies()
		{
			List<int> enemyIds = new List<int>(_enemies.Keys);
			foreach (int enemyId in enemyIds)
			{
				RemoveEnemy(enemyId);
			}
		}

		public void RemoveEnemy(int enemyId)
		{
			if (_enemies.Remove(enemyId))
			{
				OnEnemyRemoved?.Invoke(enemyId);
			}
		}

		public void AttackEnemy(EnemyState enemyState, int damage, bool isCritical = false)
		{
			if (!_enemies.ContainsKey(enemyState.Id)) return;

			int newHealth = enemyState.Health - damage;

			Debug.Log($"Attacked enemy id°{enemyState.Id}. Health : {enemyState.Health} -> {newHealth}");

			if (newHealth <= 0)
			{
				Debug.Log($"Enemy id°{enemyState.Id} is dead. Removing it.");
				OnEnemyKilled?.Invoke(enemyState.Id, damage, isCritical);
				RemoveEnemy(enemyState.Id);
			}
			else
			{
				// Re-read the state: the caller's copy may predate a phase change. Damage does not interrupt an attack in progress.
				_enemies[enemyState.Id] = _enemies[enemyState.Id].WithHealth(newHealth);
				OnEnemyDamaged?.Invoke(enemyState.Id, damage, isCritical);
			}
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (!_gameFlowService.IsPlaying || _heroController.CurrentState.IsDead)
				{
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
					continue;
				}

				List<int> enemiesToUpdate = new List<int>(_enemies.Keys);
				for (int i = 0; i < enemiesToUpdate.Count; i++)
				{
					int enemyId = enemiesToUpdate[i];
					if (!_enemies.TryGetValue(enemyId, out EnemyState enemy)) continue;

					UpdateEnemy(enemy);
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		// Chase until in range, then Windup (telegraph) -> Strike (attack animation, with a lunge) -> hit if the hero is still in range.
		// Once the windup starts the enemy is committed: it stops chasing, so the hero can dodge by walking out of range.
		// A kamikaze enemy skips all that: it hits the moment it reaches its range and dies.
		private void UpdateEnemy(EnemyState enemy)
		{
			switch (enemy.AttackPhase)
			{
				case EnemyAttackPhase.Windup:
					if (Time.time >= enemy.PhaseEndTime) StartStrike(enemy);
					break;
				case EnemyAttackPhase.Strike:
					if (Time.time >= enemy.PhaseEndTime) FinishStrike(enemy);
					else Lunge(enemy);
					break;
				default:
					ChaseOrAttack(enemy);
					break;
			}
		}

		private void ChaseOrAttack(EnemyState enemy)
		{
			Vector3 heroPosition = _heroController.CurrentState.Position;
			float distanceToHero = Vector3.Distance(enemy.Position, heroPosition);

			if (distanceToHero > enemy.Config.AttackRange)
			{
				Vector3 direction = (heroPosition - enemy.Position).normalized;
				Vector3 newPosition = enemy.Position + direction * (enemy.Config.Speed * Time.deltaTime);

				EnemyState updatedEnemy = enemy.WithPosition(newPosition);
				_enemies[enemy.Id] = updatedEnemy;
				OnEnemyPositionChanged?.Invoke(updatedEnemy);
			}
			else if (enemy.Config.ExplodesOnContact)
			{
				Explode(enemy);
			}
			else if (Time.time - enemy.LastAttackTime >= enemy.Config.AttackCooldown)
			{
				_enemies[enemy.Id] = enemy.WithPhase(EnemyAttackPhase.Windup, Time.time + enemy.Config.AttackWindupDuration);
				OnEnemyWindupStarted?.Invoke(enemy.Id, enemy.Config.AttackWindupDuration);
			}
		}

		// The lunge aims at where the hero stands when the strike starts, not where he goes next: stepping aside still dodges it.
		private void StartStrike(EnemyState enemy)
		{
			Vector3 toHero = _heroController.CurrentState.Position - enemy.Position;
			toHero.y = 0f;
			float lunge = Mathf.Clamp(toHero.magnitude - LungeStopDistance, 0f, enemy.Config.LungeDistance);
			Vector3 lungeTarget = enemy.Position + toHero.normalized * lunge;

			_enemies[enemy.Id] = enemy.WithStrike(Time.time + enemy.Config.AttackDuration, lungeTarget);
			OnEnemyStrikeStarted?.Invoke(enemy.Id);
		}

		// The dash takes the first half of the attack animation: a quick jump forward, then the bite.
		private void Lunge(EnemyState enemy)
		{
			if (enemy.Position == enemy.LungeTarget) return;

			float lungeSpeed = enemy.Config.LungeDistance / Mathf.Max(0.01f, enemy.Config.AttackDuration * 0.5f);
			EnemyState updatedEnemy = enemy.WithPosition(Vector3.MoveTowards(enemy.Position, enemy.LungeTarget, lungeSpeed * Time.deltaTime));
			_enemies[enemy.Id] = updatedEnemy;
			OnEnemyPositionChanged?.Invoke(updatedEnemy);
		}

		// The end of the attack animation is the moment of impact: the hero is hit only if he is still in range.
		private void FinishStrike(EnemyState enemy)
		{
			_enemies[enemy.Id] = enemy.WithAttackFinished(Time.time);

			float distanceToHero = Vector3.Distance(enemy.Position, _heroController.CurrentState.Position);
			if (distanceToHero > enemy.Config.AttackRange) return;

			_heroController.TakeHit(enemy.Config.AttackDamage);
			OnEnemyAttacked?.Invoke(enemy.Id);
		}

		// Kamikaze: the hit and the death are the same moment. No OnEnemyKilled, so the hero gets no kill feedback nor reward for it.
		private void Explode(EnemyState enemy)
		{
			_heroController.TakeHit(enemy.Config.AttackDamage);
			OnEnemyAttacked?.Invoke(enemy.Id);
			OnEnemyExploded?.Invoke(enemy.Id);
			RemoveEnemy(enemy.Id);
		}
	}
}
