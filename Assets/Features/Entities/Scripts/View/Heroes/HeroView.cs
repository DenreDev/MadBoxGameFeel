using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.GameFeel;
using Game.GameFlow;
using Game.HealthBar;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public class HeroView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int RunSpeedHash = Animator.StringToHash("RunSpeed");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int HitHash = Animator.StringToHash("Hit");
		private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField, Tooltip("Ground speed (units per second) the Run clip covers when played at normal speed. The clip plays faster or slower in proportion to the hero's real move speed, so the feet do not slide. Raise it if the legs look too fast.")]
		private float runClipGroundSpeed = 2f;
		[SerializeField] private Transform weaponSlot;
		[SerializeField, Tooltip("Minimum seconds between two hit reactions (flinch, flash), so a crowd hitting the hero does not lock the pose. A swing in progress is never flinched out of.")]
		private float hitReactionCooldown = 0.5f;
		[SerializeField, Tooltip("Screen-space height above the hero pivot where the health bar floats")]
		private float healthBarHeight = 2.1f;
		[SerializeField] private HitFeedbackView hitFeedback;
		[SerializeField] private FootstepFeedbackView footsteps;
		[SerializeField] private DeathLaunchView deathLaunch;

		private HealthBarView _healthBar;
		private JoystickInputService _joystickInputService;
		private HeroController _heroController;
		private EnemiesController _enemiesController;
		private WeaponsService _weaponsService;
		private GameFlowService _gameFlowService;
		private CurrencyFeedbackService _currency;
		private Vector2 _currentMovementInput;
		private WeaponView _currentWeaponView;
		private float _lastHitReactionTime = float.NegativeInfinity;
		private bool _isDead;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_heroController = entitiesService.HeroController;
			_enemiesController = entitiesService.EnemiesController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_currency = ServicesLocator.Instance.GetService<CurrencyFeedbackService>();
			if (_currency != null) _currency.Target = transform; // the currency orbs fly to the hero

			// The hero's feedback components get the configs HeroConfig plugs in.
			if (hitFeedback != null) hitFeedback.Bind(HeroConfig.Instance.HitFeedback);
			if (footsteps != null) footsteps.Bind(HeroConfig.Instance.Footsteps);

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.Attack.OnAttackStarted += OnHeroAttackStarted;
			_heroController.OnDamaged += OnHeroDamaged;
			_heroController.OnDied += OnHeroDied;
			_weaponsService.OnWeaponChanged += OnWeaponChanged;
			_gameFlowService.OnStateChanged += OnGameStateChanged;

			_healthBar = ServicesLocator.Instance.GetService<HealthBarService>().Create(transform, healthBarHeight, HealthBarTeam.Hero);

			OnJoystickStateChanged(_joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
			OnGameStateChanged(_gameFlowService.State);
			SpawnCurrentWeapon();
		}

		// The health bar has no place on the start screen, it would sit over the hero's face. Back in gameplay it works out its own visibility again.
		private void OnGameStateChanged(GameState state)
		{
			if (_healthBar == null) return;

			if (state == GameState.Start) _healthBar.Hide();
			else _healthBar.SetHealth(_heroController.CurrentState.Health, HeroConfig.Instance.InitialHealth);
		}

		private void OnDestroy()
		{
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_joystickInputService != null)
			{
				_joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			}
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_heroController.Attack.OnAttackStarted -= OnHeroAttackStarted;
				_heroController.OnDamaged -= OnHeroDamaged;
				_heroController.OnDied -= OnHeroDied;
			}
			if (_weaponsService != null)
			{
				_weaponsService.OnWeaponChanged -= OnWeaponChanged;
			}
			if (_gameFlowService != null)
			{
				_gameFlowService.OnStateChanged -= OnGameStateChanged;
			}
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
			}
		}

		private void OnJoystickStateChanged(JoystickState state)
		{
			_currentMovementInput = state.IsActive ? state.MovementVector : Vector2.zero;
			UpdateAnimator();
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			transform.position = heroState.Position;
			if (_healthBar != null) _healthBar.SetHealth(heroState.Health, HeroConfig.Instance.InitialHealth);

			// Restart: bring the launched model back before the Death animation blends out.
			if (_isDead && !heroState.IsDead && deathLaunch != null) deathLaunch.Restore();
			_isDead = heroState.IsDead;

			if (animator == null) return;

			animator.SetBool(IsDeadHash, heroState.IsDead);
			if (heroState.IsDead)
			{
				// Drop a swing/flinch queued in the same frame as the death, otherwise it plays on revive.
				animator.ResetTrigger(AttackHash);
				animator.ResetTrigger(HitHash);
			}
		}

		private void OnHeroDamaged()
		{
			if (Time.time - _lastHitReactionTime < hitReactionCooldown) return;

			_lastHitReactionTime = Time.time;

			// The flinch would cut the attack short: the flash and punch still play, the animation does not.
			if (animator != null && !_heroController.Attack.IsAttacking) animator.SetTrigger(HitHash);
			if (hitFeedback != null) hitFeedback.Play();
		}

		private void OnHeroDied()
		{
			if (hitFeedback != null) hitFeedback.Play();
			if (deathLaunch != null) deathLaunch.Play(DirectionAwayFromClosestEnemy());
			if (_currency != null) _currency.Clear();
		}

		// The closest enemy is the one that landed the killing blow, so the hero is knocked away from it.
		// Zero when no enemy is left, the launch then falls back to straight behind the model.
		private Vector3 DirectionAwayFromClosestEnemy()
		{
			Vector3 away = Vector3.zero;
			float closestSqrDistance = float.MaxValue;

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				Vector3 offset = transform.position - enemy.Position;
				if (offset.sqrMagnitude >= closestSqrDistance) continue;

				closestSqrDistance = offset.sqrMagnitude;
				away = offset;
			}

			return away;
		}

		// The attack starts: play the animation. The turn toward the target is handled in Update, for as long as the attack lasts.
		private void OnHeroAttackStarted(int targetEnemyId)
		{
			if (animator != null) animator.SetTrigger(AttackHash);
		}

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			// On the start screen the hero turns to the camera, so the player sees his face instead of his back.
			if (_gameFlowService.State == GameState.Start)
			{
				FaceCamera();
				return;
			}

			// An attack wins over the movement direction until it is over, so the hero visibly aims at what he hits.
			if (_heroController.Attack.IsAttacking)
			{
				TurnToward(_heroController.Attack.AimDirection);
				return;
			}

			if (_currentMovementInput.sqrMagnitude <= 0.01f) return;

			TurnToward(new Vector3(-_currentMovementInput.x, 0f, -_currentMovementInput.y));
		}

		private void FaceCamera()
		{
			Camera mainCamera = Camera.main;
			if (mainCamera == null) return;

			Vector3 toCamera = mainCamera.transform.position - transform.position;
			toCamera.y = 0f;
			if (toCamera.sqrMagnitude <= 0.01f) return;

			TurnToward(toCamera);
		}

		// Every turn goes through here so the hero never snaps. The model faces the root's -Z, so look away from the direction to face it.
		private void TurnToward(Vector3 direction)
		{
			Quaternion targetRotation = Quaternion.LookRotation(-direction);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		}

		private void UpdateAnimator()
		{
			if (animator == null) return;
			if (_heroController is { CurrentState: { IsDead: true } })
			{
				animator.SetFloat(SpeedHash, 0f);
				return;
			}

			float speed = _currentMovementInput.magnitude;
			animator.SetFloat(SpeedHash, speed);

			// Same speed the controller moves the hero at. Left untouched when stopped, so the run pose does not freeze while it blends out to idle.
			if (speed > 0.01f) animator.SetFloat(RunSpeedHash, HeroConfig.Instance.MoveSpeed * speed / runClipGroundSpeed);
		}

		private void OnWeaponChanged(WeaponConfig newWeapon)
		{
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
				_currentWeaponView = null;
			}

			SpawnCurrentWeapon();
		}

		private void SpawnCurrentWeapon()
		{
			if (_weaponsService.CurrentWeapon == null) return;

			Transform parent = weaponSlot != null ? weaponSlot : transform;
			_currentWeaponView = Instantiate(_weaponsService.CurrentWeapon.Prefab, parent);
			_currentWeaponView.transform.localPosition = Vector3.zero;
			_currentWeaponView.transform.localRotation = Quaternion.identity;
		}
	}
}