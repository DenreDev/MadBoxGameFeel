using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFlow;
using Game.GamePlay.Enemies;
using Game.JoystickInput;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public class HeroController
	{
		// Services
		private EnemiesController _enemiesController;
		private JoystickInputService _joystickInputService;
		private WeaponsService _weaponsService;
		private GameFlowService _gameFlowService;

		// Internal State
		private CancellationTokenSource _cancellationTokenSource;
		private HeroState _currentState;
		private bool _isSwinging;
		private readonly List<EnemyState> _hitTargets = new List<EnemyState>(); // reused by every swing

		// Public State
		public HeroState CurrentState => _currentState;

		// Events
		public event Action<HeroState> OnStateChanged;
		public event Action<int> OnAttackStarted; // target enemy id: the swing begins, the damage lands later
		public event Action<Vector3> OnAttacked; // position of the first enemy hit: the slash reached its high point (the damage lands now, if an enemy is still in range)
		public event Action OnDamaged; // hit taken and survived (death is reported through OnStateChanged)
		public event Action OnDied; // the hit that killed the hero, raised right after OnStateChanged

		public UniTask<bool> Initialize(EnemiesController enemiesController, JoystickInputService joystickInputService, WeaponsService weaponsService, GameFlowService gameFlowService)
		{
			_enemiesController = enemiesController;
			_joystickInputService = joystickInputService;
			_weaponsService = weaponsService;
			_gameFlowService = gameFlowService;

			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth, 0f);
			_cancellationTokenSource = new CancellationTokenSource();
			_isSwinging = false;

			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public void TakeHit(int damage)
		{
			if (_currentState.IsDead) return;

			int newHealth = Mathf.Max(0, _currentState.Health - damage);
			Debug.Log($"Hero is taking a hit. Health : {_currentState.Health} -> {newHealth}");
			_currentState = new HeroState(_currentState.Position, newHealth, _currentState.LastAttackTime);
			OnStateChanged?.Invoke(_currentState);

			if (_currentState.IsDead)
			{
				Debug.Log("Hero is dead!");
				OnDied?.Invoke();
			}
			else
			{
				OnDamaged?.Invoke();
			}
		}

		public void Restart()
		{
			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth, 0f);
			OnStateChanged?.Invoke(_currentState);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();

			return UniTask.CompletedTask;
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				// The hero neither moves nor attacks outside of gameplay (start screen, game over).
				if (_gameFlowService.IsPlaying && !_currentState.IsDead)
				{
					// Move and auto-attack at the same time (survivor-like): moving never disables the weapon.
					if (_joystickInputService.CurrentState.IsActive) UpdatePosition();
					AttackClosestEnemy();
				}
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdatePosition()
		{
			Vector2 currentMovementInput = _joystickInputService.CurrentState.IsActive ? _joystickInputService.CurrentState.MovementVector : Vector2.zero;
			if (currentMovementInput.sqrMagnitude <= 0.01f) return;

			Vector3 movement = new Vector3(-currentMovementInput.x, 0f, -currentMovementInput.y);
			Vector3 newPosition = _currentState.Position + movement * (HeroConfig.Instance.MoveSpeed * Time.deltaTime);

			_currentState = new HeroState(newPosition, _currentState.Health, _currentState.LastAttackTime);
			OnStateChanged?.Invoke(_currentState);
		}

		private void AttackClosestEnemy()
		{
			WeaponConfig weapon = _weaponsService.CurrentWeapon;
			if (weapon == null || _isSwinging) return;
			if (Time.time - _currentState.LastAttackTime < weapon.Cooldown) return;

			if (TryFindClosestEnemy(out EnemyState closestEnemy))
			{
				Swing(weapon, closestEnemy, _cancellationTokenSource.Token).Forget();
			}
		}

		// The swing is not instant: the animation starts now, the damage lands when the slash reaches its high point,
		// and the next swing waits for the animation to finish so it always plays completely.
		private async UniTaskVoid Swing(WeaponConfig weapon, EnemyState target, CancellationToken cancellationToken)
		{
			_isSwinging = true;
			_currentState = new HeroState(_currentState.Position, _currentState.Health, Time.time);
			OnAttackStarted?.Invoke(target.Id);

			try
			{
				await UniTask.Delay(TimeSpan.FromSeconds(weapon.HitDelay), cancellationToken: cancellationToken);
				Slash(weapon, target);

				await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, weapon.SwingDuration - weapon.HitDelay)), cancellationToken: cancellationToken);
			}
			finally
			{
				_isSwinging = false;
			}
		}

		// Every enemy in range takes the hit, up to the weapon's max targets: the one the hero aimed at first (unless it died or left the range
		// during the windup), then the closest ones. The slash is announced even when nothing is left in range, so a missed swing still looks like a swing.
		private void Slash(WeaponConfig weapon, EnemyState swingTarget)
		{
			if (_currentState.IsDead || !_gameFlowService.IsPlaying) return;

			FindTargetsInRange(weapon, swingTarget.Id);
			Vector3 slashPosition = _hitTargets.Count > 0 ? _hitTargets[0].Position : swingTarget.Position;

			// Iterating our own list: killing an enemy removes it from the controller, not from here.
			foreach (EnemyState target in _hitTargets)
			{
				bool isCritical = UnityEngine.Random.value < weapon.CritChance;
				int damage = isCritical ? Mathf.RoundToInt(weapon.Damage * weapon.CritMultiplier) : weapon.Damage;
				_enemiesController.AttackEnemy(target, damage, isCritical);
			}

			OnAttacked?.Invoke(slashPosition);
		}

		private void FindTargetsInRange(WeaponConfig weapon, int aimedEnemyId)
		{
			Vector3 heroPosition = _currentState.Position;
			_hitTargets.Clear();

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				if (Vector3.Distance(heroPosition, enemy.Position) < weapon.Range) _hitTargets.Add(enemy);
			}

			// The aimed enemy sorts first, the others by distance.
			_hitTargets.Sort((a, b) => SortKey(a).CompareTo(SortKey(b)));
			if (_hitTargets.Count > weapon.MaxTargets) _hitTargets.RemoveRange(weapon.MaxTargets, _hitTargets.Count - weapon.MaxTargets);

			float SortKey(EnemyState enemy) => enemy.Id == aimedEnemyId ? -1f : Vector3.Distance(heroPosition, enemy.Position);
		}

		private bool TryFindClosestEnemy(out EnemyState closestEnemy)
		{
			closestEnemy = default;
			if (_weaponsService.CurrentWeapon == null) return false;
			float closestDistance = _weaponsService.CurrentWeapon.Range;
			bool found = false;

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				float distance = Vector3.Distance(_currentState.Position, enemy.Position);
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