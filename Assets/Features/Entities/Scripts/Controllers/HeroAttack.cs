using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFlow;
using Game.GamePlay.Enemies;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	// Decides WHEN the hero attacks: the weapon is ready (cooldown) and an enemy is in range. What the attack is (a slash, ...) belongs to the weapon:
	// HeroAttack starts it with weapon.Attack.Perform and gives it, as an IAttackContext, what it needs from the hero and the enemies.
	// Created, ticked and restarted by HeroController, which keeps the hero's own state (position, health). The views read the aim and the attack state from here.
	public class HeroAttack : IAttackContext
	{
		// Services
		private readonly HeroController _hero;
		private readonly EnemiesController _enemiesController;
		private readonly WeaponsService _weaponsService;
		private readonly GameFlowService _gameFlowService;
		private readonly CancellationToken _lifetime;

		// Internal State
		private readonly List<EnemyState> _hitTargets = new List<EnemyState>(); // reused by every attack
		private WeaponConfig _attackWeapon; // the weapon performing the attack: the player may switch weapons while it runs
		private Vector3 _aimDirection = Vector3.back;
		private Vector3 _targetPosition;
		private int _targetId;
		private float _lastAttackTime;
		private bool _isAttacking;

		// Public State
		public bool IsAttacking => _isAttacking;
		public Vector3 AimDirection => _aimDirection; // flat, normalized: where the attack points, toward the enemy the hero aims at (back = the model's front at rest)

		// Events
		public event Action<int> OnAttackStarted; // target enemy id: the attack begins, the damage lands later
		public event Action<Vector3> OnAttacked; // position of the first enemy hit: the weapon hit the cone (the damage lands now, if an enemy is still in range)

		public HeroAttack(HeroController hero, EnemiesController enemiesController, WeaponsService weaponsService, GameFlowService gameFlowService, CancellationToken lifetime)
		{
			_hero = hero;
			_enemiesController = enemiesController;
			_weaponsService = weaponsService;
			_gameFlowService = gameFlowService;
			_lifetime = lifetime;
			Restart();
		}

		// A new round: the weapon is ready at once.
		public void Restart()
		{
			_lastAttackTime = float.NegativeInfinity;
		}

		// Called every frame by HeroController while the hero is alive and the game is playing.
		public void Tick()
		{
			WeaponConfig weapon = _weaponsService.CurrentWeapon;
			if (weapon == null || weapon.Attack == null) return;

			if (_isAttacking)
			{
				TrackTarget();
				return;
			}

			if (Time.time - _lastAttackTime < weapon.Cooldown) return;

			if (TryFindClosestEnemy(weapon, out EnemyState closestEnemy)) Attack(weapon, closestEnemy).Forget();
		}

		// The attack is not instant: the animation starts now, the weapon lands its hits when it decides to, and the next attack waits for it to finish.
		private async UniTaskVoid Attack(WeaponConfig weapon, EnemyState target)
		{
			_isAttacking = true;
			_attackWeapon = weapon;
			_targetId = target.Id;
			_targetPosition = target.Position;
			_lastAttackTime = Time.time;
			UpdateAim(target.Position);
			OnAttackStarted?.Invoke(target.Id);

			try
			{
				await weapon.Attack.Perform(this, weapon, _lifetime);
			}
			finally
			{
				_isAttacking = false;
			}
		}

		// What the weapon's attack may ask of the hero (see IAttackContext). Kept off the public surface: only weapons call these.
		bool IAttackContext.CanHit => !_hero.CurrentState.IsDead && _gameFlowService.IsPlaying;

		// The aim follows the target while the hero walks and the enemy chases: a direction fixed at the start would be stale when the weapon hits.
		// A target that died keeps the last aim.
		void IAttackContext.TrackTarget() => TrackTarget();

		void IAttackContext.HitCone(float range, float arcAngle, int maxTargets)
		{
			FindTargetsInRange(range, arcAngle, maxTargets);
			Vector3 impactPosition = _hitTargets.Count > 0 ? _hitTargets[0].Position : _targetPosition;

			// Iterating our own list: killing an enemy removes it from the controller, not from here.
			foreach (EnemyState target in _hitTargets)
			{
				bool isCritical = UnityEngine.Random.value < _attackWeapon.CritChance;
				int damage = isCritical ? Mathf.RoundToInt(_attackWeapon.Damage * _attackWeapon.CritMultiplier) : _attackWeapon.Damage;
				_enemiesController.AttackEnemy(target, damage, isCritical);
			}

			OnAttacked?.Invoke(impactPosition);
		}

		private void TrackTarget()
		{
			if (!_enemiesController.Enemies.TryGetValue(_targetId, out EnemyState target)) return;

			_targetPosition = target.Position;
			UpdateAim(target.Position);
		}

		// The aimed enemy first (unless it died or left the range meanwhile), then the closest ones. Nothing behind the hero is hit.
		private void FindTargetsInRange(float range, float arcAngle, int maxTargets)
		{
			Vector3 heroPosition = _hero.CurrentState.Position;
			float halfArc = arcAngle * 0.5f;
			_hitTargets.Clear();

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				if (Vector3.Distance(heroPosition, enemy.Position) < range && IsInsideArc(enemy.Position - heroPosition, halfArc)) _hitTargets.Add(enemy);
			}

			// The aimed enemy sorts first, the others by distance.
			_hitTargets.Sort((a, b) => SortKey(a).CompareTo(SortKey(b)));
			if (_hitTargets.Count > maxTargets) _hitTargets.RemoveRange(maxTargets, _hitTargets.Count - maxTargets);

			float SortKey(EnemyState enemy) => enemy.Id == _targetId ? -1f : Vector3.Distance(heroPosition, enemy.Position);
		}

		// An enemy on top of the hero has no direction, he is always hit.
		private bool IsInsideArc(Vector3 offset, float halfArc)
		{
			offset.y = 0f;
			if (offset.sqrMagnitude < 0.0001f) return true;

			return Vector3.Angle(_aimDirection, offset) <= halfArc;
		}

		private void UpdateAim(Vector3 targetPosition)
		{
			Vector3 direction = targetPosition - _hero.CurrentState.Position;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0.01f) _aimDirection = direction.normalized;
		}

		private bool TryFindClosestEnemy(WeaponConfig weapon, out EnemyState closestEnemy)
		{
			closestEnemy = default;
			float closestDistance = weapon.Range;
			bool found = false;

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				float distance = Vector3.Distance(_hero.CurrentState.Position, enemy.Position);
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closestEnemy = enemy;
					found = true;
				}
			}

			return found;
		}
	}
}
