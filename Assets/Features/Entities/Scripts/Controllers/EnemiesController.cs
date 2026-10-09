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
		private readonly List<int> _bodyIds = new List<int>(); // reused by ResolveOverlaps and ShoveNeighbours
		private readonly HashSet<int> _pushedIds = new HashSet<int>();

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
				ShoveNeighbours(_enemies[enemyState.Id]);
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

				ResolveOverlaps();

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		// Chase until in range, then Windup (telegraph) -> Strike (attack animation, with a lunge) -> hit if the hero is still in range.
		// Once the windup starts the enemy is committed: it stops chasing, so the hero can dodge by walking out of range.
		// A kamikaze enemy skips all that: it hits the moment it reaches its range and dies.
		private void UpdateEnemy(EnemyState enemy)
		{
			if (enemy.Knockback != Vector3.zero) enemy = Slide(enemy);

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
				// Chase while steering away from close neighbours: the crowd spreads and flows around itself instead of queuing in one line.
				// Clamped, not normalized: an enemy boxed in by its neighbours slows down instead of jittering at full speed.
				Vector3 toHero = heroPosition - enemy.Position;
				toHero.y = 0f;
				Vector3 steering = toHero.normalized + AvoidNeighbours(enemy) * EnemiesConfig.Instance.AvoidanceStrength;
				Vector3 newPosition = enemy.Position + Vector3.ClampMagnitude(steering, 1f) * (enemy.Config.Speed * Time.deltaTime);

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

		// The killing blow carries through the corpse into the crowd: each neighbour is shoved away from the corpse and along the blow
		// (enemies behind it fly back, enemies beside it are pushed aside). The speed follows a billiard-ball (elastic) collision,
		// 2 × corpse mass / (corpse mass + neighbour mass), so a light bee flies further and a big corpse shoves harder.
		private void ShoveNeighbours(EnemyState corpse)
		{
			EnemiesConfig config = EnemiesConfig.Instance;
			if (config.DeathShoveRadius <= 0f) return;

			Vector3 blow = corpse.Position - _heroController.CurrentState.Position;
			blow.y = 0f;
			blow = blow.normalized;
			float corpseMass = corpse.Config.BodyRadius * corpse.Config.BodyRadius;

			_bodyIds.Clear();
			_bodyIds.AddRange(_enemies.Keys);
			foreach (int enemyId in _bodyIds)
			{
				if (enemyId == corpse.Id) continue;

				EnemyState other = _enemies[enemyId];
				Vector3 away = other.Position - corpse.Position;
				away.y = 0f;
				float distance = away.magnitude;
				if (distance >= config.DeathShoveRadius) continue;

				float otherMass = other.Config.BodyRadius * other.Config.BodyRadius;
				float transfer = 2f * corpseMass / (corpseMass + otherMass);
				float falloff = 1f - distance / config.DeathShoveRadius;
				Vector3 direction = (Direction(away, distance, enemyId) + blow).normalized;
				Vector3 knockback = other.Knockback + direction * (config.DeathShoveSpeed * transfer * falloff);

				// Capped so a cleave killing several bees at once cannot fling a survivor across the arena.
				_enemies[enemyId] = other.WithKnockback(Vector3.ClampMagnitude(knockback, config.DeathShoveSpeed * 2f));
			}
		}

		// A shoved enemy slides on top of whatever it is doing, slowed by friction. Attacking enemies slide too:
		// knocking back a bee that is about to bite can carry it out of range, so its hit misses.
		private EnemyState Slide(EnemyState enemy)
		{
			Vector3 knockback = Vector3.MoveTowards(enemy.Knockback, Vector3.zero, EnemiesConfig.Instance.KnockbackFriction * Time.deltaTime);
			EnemyState updatedEnemy = enemy.WithPosition(enemy.Position + enemy.Knockback * Time.deltaTime).WithKnockback(knockback);
			_enemies[enemy.Id] = updatedEnemy;
			OnEnemyPositionChanged?.Invoke(updatedEnemy);
			return updatedEnemy;
		}

		// Sum of the pushes away from the neighbours within the avoidance margin: 0 at the edge of the margin, 1 when the bodies touch.
		private Vector3 AvoidNeighbours(EnemyState enemy)
		{
			float margin = Mathf.Max(0.01f, EnemiesConfig.Instance.AvoidanceMargin);
			Vector3 avoidance = Vector3.zero;

			foreach (EnemyState other in _enemies.Values)
			{
				if (other.Id == enemy.Id) continue;

				Vector3 away = enemy.Position - other.Position;
				away.y = 0f;
				float distance = away.magnitude;
				float gap = distance - (enemy.Config.BodyRadius + other.Config.BodyRadius);
				if (gap >= margin) continue;

				avoidance += Direction(away, distance, enemy.Id) * (1f - Mathf.Clamp01(gap / margin));
			}

			return avoidance;
		}

		// The collision step, run once per frame after everyone moved: whatever the steering could not avoid (a lunge, a crowd pressing in)
		// is pushed apart until the bodies just touch. Mass is the body area, so a big bee shoves a small one more than it gets shoved.
		// An enemy committed to an attack is planted: a chaser cannot push it, so its telegraph and lunge stay where the player saw them.
		// Two planted enemies (one lunging into the other) are split by mass like two chasers.
		private void ResolveOverlaps()
		{
			_bodyIds.Clear();
			_bodyIds.AddRange(_enemies.Keys);
			_pushedIds.Clear();

			for (int i = 0; i < _bodyIds.Count; i++)
			{
				for (int j = i + 1; j < _bodyIds.Count; j++)
				{
					EnemyState a = _enemies[_bodyIds[i]];
					EnemyState b = _enemies[_bodyIds[j]];

					Vector3 offset = b.Position - a.Position;
					offset.y = 0f;
					float distance = offset.magnitude;
					float overlap = a.Config.BodyRadius + b.Config.BodyRadius - distance;
					if (overlap <= 0f) continue;

					bool aPlanted = a.AttackPhase != EnemyAttackPhase.None;
					bool bPlanted = b.AttackPhase != EnemyAttackPhase.None;
					float massA = a.Config.BodyRadius * a.Config.BodyRadius;
					float massB = b.Config.BodyRadius * b.Config.BodyRadius;
					float aShare = aPlanted == bPlanted ? massB / (massA + massB) : aPlanted ? 0f : 1f; // share of the push that moves a
					Vector3 push = Direction(offset, distance, b.Id) * overlap;

					_enemies[a.Id] = a.WithPosition(a.Position - push * aShare);
					_enemies[b.Id] = b.WithPosition(b.Position + push * (1f - aShare));
					if (aShare > 0f) _pushedIds.Add(a.Id);
					if (aShare < 1f) _pushedIds.Add(b.Id);
				}
			}

			foreach (int enemyId in _pushedIds) OnEnemyPositionChanged?.Invoke(_enemies[enemyId]);
		}

		// Normalized offset. Two bodies on the exact same spot (a group spawn) get a fixed direction per id, so they still split apart.
		private static Vector3 Direction(Vector3 offset, float distance, int enemyId)
		{
			if (distance > 0.001f) return offset / distance;
			return Quaternion.Euler(0f, enemyId * 137.5f, 0f) * Vector3.forward;
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
