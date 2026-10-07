using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFlow;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesController
	{
		private HeroController _heroController;
		private GameFlowService _gameFlowService;

		// Events
		public event Action<EnemyState> OnEnemySpawned;
		public event Action<int> OnEnemyRemoved;
		public event Action<EnemyState> OnEnemyPositionChanged;
		public event Action<int, float> OnEnemyWindupStarted; // enemy id, windup duration: hero in range, the enemy telegraphs its attack
		public event Action<int> OnEnemyStrikeStarted; // windup over, the attack animation starts
		public event Action<int> OnEnemyAttacked; // the strike ended and the hero was still in range: he took the hit
		public event Action<int, int> OnEnemyDamaged; // enemy id, damage dealt: hit by the hero and survived
		public event Action<int, int> OnEnemyKilled; // enemy id, damage dealt by the killing blow: raised just before OnEnemyRemoved

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

			SpawnLoop(_cancellationTokenSource.Token).Forget();
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

		public void AttackEnemy(EnemyState enemyState, int damage)
		{
			if (!_enemies.ContainsKey(enemyState.Id)) return;

			int newHealth = enemyState.Health - damage;

			Debug.Log($"Attacked enemy id°{enemyState.Id}. Health : {enemyState.Health} -> {newHealth}");

			if (newHealth <= 0)
			{
				Debug.Log($"Enemy id°{enemyState.Id} is dead. Removing it.");
				OnEnemyKilled?.Invoke(enemyState.Id, damage);
				RemoveEnemy(enemyState.Id);
			}
			else
			{
				// Re-read the state: the caller's copy may predate a phase change. Damage does not interrupt an attack in progress.
				_enemies[enemyState.Id] = _enemies[enemyState.Id].WithHealth(newHealth);
				OnEnemyDamaged?.Invoke(enemyState.Id, damage);
			}
		}

		private async UniTaskVoid SpawnLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				// Nothing spawns outside of gameplay. Polling each frame keeps the first bee immediate when a round starts.
				if (!_gameFlowService.IsPlaying)
				{
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
					continue;
				}

				if (!_heroController.CurrentState.IsDead && _enemies.Count < EnemiesConfig.Instance.MaxEnemies)
				{
					SpawnEnemy();
				}

				await UniTask.Delay(TimeSpan.FromSeconds(EnemiesConfig.Instance.SpawnInterval), cancellationToken: cancellationToken);
			}
		}

		private void SpawnEnemy()
		{
			if (EnemiesConfig.Instance.Enemies.Count == 0) return;

			Vector3 playerPosition = _heroController.CurrentState.Position;
			Vector3 spawnPosition = GetRandomPositionAroundPlayer(playerPosition);

			int enemyId = _nextEnemyId++;
			EnemyConfig enemyConfig = EnemiesConfig.Instance.Enemies[0];
			EnemyState newEnemy = new EnemyState(enemyId, spawnPosition, enemyConfig.InitialHealth, enemyConfig);

			_enemies[enemyId] = newEnemy;
			OnEnemySpawned?.Invoke(newEnemy);
		}

		private Vector3 GetRandomPositionAroundPlayer(Vector3 playerPosition)
		{
			float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
			float x = playerPosition.x + EnemiesConfig.Instance.SpawnRadius * Mathf.Cos(angle);
			float z = playerPosition.z + EnemiesConfig.Instance.SpawnRadius * Mathf.Sin(angle);

			return new Vector3(x, playerPosition.y, z);
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

		// Chase until in range, then Windup (telegraph) -> Strike (attack animation) -> hit if the hero is still in range.
		// Once the windup starts the enemy is committed: it does not move, so the hero can dodge by walking out of range.
		private void UpdateEnemy(EnemyState enemy)
		{
			switch (enemy.AttackPhase)
			{
				case EnemyAttackPhase.Windup:
					if (Time.time >= enemy.PhaseEndTime) StartStrike(enemy);
					break;
				case EnemyAttackPhase.Strike:
					if (Time.time >= enemy.PhaseEndTime) FinishStrike(enemy);
					break;
				default:
					ChaseOrStartWindup(enemy);
					break;
			}
		}

		private void ChaseOrStartWindup(EnemyState enemy)
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
			else if (Time.time - enemy.LastAttackTime >= enemy.Config.AttackCooldown)
			{
				_enemies[enemy.Id] = enemy.WithPhase(EnemyAttackPhase.Windup, Time.time + enemy.Config.AttackWindupDuration);
				OnEnemyWindupStarted?.Invoke(enemy.Id, enemy.Config.AttackWindupDuration);
			}
		}

		private void StartStrike(EnemyState enemy)
		{
			_enemies[enemy.Id] = enemy.WithPhase(EnemyAttackPhase.Strike, Time.time + enemy.Config.AttackDuration);
			OnEnemyStrikeStarted?.Invoke(enemy.Id);
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
	}
}