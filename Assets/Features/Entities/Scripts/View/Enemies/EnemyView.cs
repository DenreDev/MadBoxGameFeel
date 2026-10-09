using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Game.GameFeel;
using Game.HealthBar;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemyView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int HitHash = Animator.StringToHash("Hit");
		private static readonly int DieHash = Animator.StringToHash("Die");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField, Tooltip("Seconds the death animation plays before the enemy is destroyed")]
		private float deathDuration = 1.5f;
		[SerializeField, Tooltip("Screen-space height above the enemy pivot where the health bar floats")]
		private float healthBarHeight = 1f;
		[SerializeField] private SpawnFeedbackView spawnFeedback;
		[SerializeField] private AttackTelegraphView attackTelegraph;
		[SerializeField] private HitFeedbackView hitFeedback;
		[SerializeField] private DeathLaunchView deathLaunch;

		private HealthBarView _healthBar;
		private HeroController _heroController;
		private Vector3 _lastPosition;
		private bool _isDying;

		private void Start()
		{
			_lastPosition = transform.position;
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
		}

		public void PlaySpawn()
		{
			if (spawnFeedback != null) spawnFeedback.Play();
		}

		// The enemy stands in range and warns the player: red blinks and a growing ground disc covering the attack range.
		public void PlayWindup(float duration, float attackRange)
		{
			if (attackTelegraph != null) attackTelegraph.Begin(duration, attackRange);
		}

		// The windup is over: the warning stops and the attack animation plays.
		public void PlayStrike()
		{
			if (attackTelegraph != null) attackTelegraph.End();
			if (animator != null) animator.SetTrigger(AttackHash);
		}

		public void CreateHealthBar(HealthBarService healthBarService, int health, int maxHealth)
		{
			_healthBar = healthBarService.Create(transform, healthBarHeight, HealthBarTeam.Enemy);
			if (_healthBar != null) _healthBar.SetHealth(health, maxHealth);
		}

		public void PlayHit(int damage, int health, int maxHealth, bool isCritical)
		{
			if (animator != null) animator.SetTrigger(HitHash);
			if (hitFeedback != null) hitFeedback.Play(damage, awayDirection: AwayFromHero(), isCritical: isCritical);
			if (_healthBar != null) _healthBar.SetHealth(health, maxHealth);
		}

		// Plays the death animation while the model is knocked away from the hero, then destroys the enemy.
		public void PlayDeath(int damage, bool isCritical)
		{
			_isDying = true;
			if (spawnFeedback != null) spawnFeedback.Finish();
			if (attackTelegraph != null) attackTelegraph.End();
			if (_healthBar != null) _healthBar.Hide();
			if (animator != null) animator.SetTrigger(DieHash);
			if (hitFeedback != null) hitFeedback.Play(damage, isKillingBlow: true, isCritical: isCritical);
			if (deathLaunch != null)
			{
				deathLaunch.Play(AwayFromHero());
			}

			// The orbs belong to the world, not to this enemy: they outlive it.
			GameFeelService gameFeelService = ServicesLocator.Instance.GetService<GameFeelService>();
			if (gameFeelService != null) gameFeelService.PlayCurrencyDrop(transform.position);
			Destroy(gameObject, deathDuration);
		}

		private Vector3 AwayFromHero()
		{
			return _heroController != null ? transform.position - _heroController.CurrentState.Position : Vector3.zero;
		}

		private void Update()
		{
			UpdateAnimatorSpeed();

			if (_isDying) return;
			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 direction = (heroPosition - transform.position).normalized;

			if (direction.sqrMagnitude > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		// Movement is driven by the controller, so the view derives its speed from the actual displacement.
		private void UpdateAnimatorSpeed()
		{
			if (animator == null || Time.deltaTime <= 0f) return;

			float speed = _isDying ? 0f : (transform.position - _lastPosition).magnitude / Time.deltaTime;
			_lastPosition = transform.position;
			animator.SetFloat(SpeedHash, speed);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
	}
}
