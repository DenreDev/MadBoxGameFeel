using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFlow;
using Game.GamePlay.Enemies;
using Game.JoystickInput;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	// The hero's entry point: owns his state (position, health), moves him and ticks his HeroAttack. What he does with his weapon lives there.
	public class HeroController
	{
		// Services
		private JoystickInputService _joystickInputService;
		private GameFlowService _gameFlowService;

		// Internal State
		private CancellationTokenSource _cancellationTokenSource;
		private HeroState _currentState;

		// Public State
		public HeroState CurrentState => _currentState;
		public HeroAttack Attack { get; private set; }

		// Events
		public event Action<HeroState> OnStateChanged;
		public event Action OnDamaged; // hit taken and survived (death is reported through OnStateChanged)
		public event Action OnDied; // the hit that killed the hero, raised right after OnStateChanged

		public UniTask<bool> Initialize(EnemiesController enemiesController, JoystickInputService joystickInputService, WeaponsService weaponsService, GameFlowService gameFlowService)
		{
			_joystickInputService = joystickInputService;
			_gameFlowService = gameFlowService;

			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth);
			_cancellationTokenSource = new CancellationTokenSource();
			Attack = new HeroAttack(this, enemiesController, weaponsService, gameFlowService, _cancellationTokenSource.Token);

			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public void TakeHit(int damage)
		{
			if (_currentState.IsDead) return;

			int newHealth = Mathf.Max(0, _currentState.Health - damage);
			Debug.Log($"Hero is taking a hit. Health : {_currentState.Health} -> {newHealth}");
			_currentState = new HeroState(_currentState.Position, newHealth);
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
			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth);
			Attack.Restart();
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
					Attack.Tick();
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

			_currentState = new HeroState(newPosition, _currentState.Health);
			OnStateChanged?.Invoke(_currentState);
		}
	}
}
