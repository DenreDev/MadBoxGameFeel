using Game.GamePlay.Entities;
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
		[SerializeField, Tooltip("Screen-space height above the hero pivot where the health bar floats")]
		private float healthBarHeight = 2.1f;

		private HealthBarView _healthBar;
		private JoystickInputService _joystickInputService;
		private HeroController _heroController;
		private WeaponsService _weaponsService;
		private Vector2 _currentMovementInput;
		private WeaponView _currentWeaponView;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.OnAttacked += OnHeroAttacked;
			_heroController.OnDamaged += OnHeroDamaged;
			_weaponsService.OnWeaponChanged += OnWeaponChanged;

			_healthBar = ServicesLocator.Instance.GetService<HealthBarService>().Create(transform, healthBarHeight, HealthBarTeam.Hero);

			OnJoystickStateChanged(_joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
			SpawnCurrentWeapon();
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_joystickInputService != null)
			{
				_joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			}
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_heroController.OnAttacked -= OnHeroAttacked;
				_heroController.OnDamaged -= OnHeroDamaged;
			}
			if (_weaponsService != null)
			{
				_weaponsService.OnWeaponChanged -= OnWeaponChanged;
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
			if (animator != null) animator.SetTrigger(HitHash);
		}

		private void OnHeroAttacked(Vector3 targetPosition)
		{
			// Face the target (the model faces -Z, same convention as the movement rotation) then swing.
			Vector3 direction = targetPosition - transform.position;
			direction.y = 0f;
			if (direction.sqrMagnitude > 0.01f)
			{
				transform.rotation = Quaternion.LookRotation(-direction);
			}

			if (animator != null) animator.SetTrigger(AttackHash);
		}

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;
			if (_currentMovementInput.sqrMagnitude <= 0.01f) return;

			Vector3 movement = new Vector3(-_currentMovementInput.x, 0f, -_currentMovementInput.y);
			Quaternion targetRotation = Quaternion.LookRotation(-movement);
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