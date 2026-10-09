using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Game.GameFeel;
using Game.HealthBar;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// Pooled by EnemiesContainerView: Appear() is called on every spawn, also on an instance that already lived and died.
	public class EnemyView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int HitHash = Animator.StringToHash("Hit");
		private static readonly int DieHash = Animator.StringToHash("Die");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField, Tooltip("Seconds the death animation plays before the enemy goes back to the pool")]
		private float deathDuration = 1.5f;
		[SerializeField, Tooltip("Screen-space height above the enemy pivot where the health bar floats")]
		private float healthBarHeight = 1f;
		[SerializeField] private SpawnFeedbackView spawnFeedback;
		[SerializeField] private AttackTelegraphView attackTelegraph;
		[SerializeField] private HitFeedbackView hitFeedback;
		[SerializeField] private DeathPhysicsView deathPhysics;

		private HealthBarView _healthBar;
		private HeroController _heroController;
		private Vector3 _lastPosition;
		private bool _isDying;

		public EnemyConfig Config { get; private set; } // which enemy this instance shows, so it goes back to the right pool
		public float DeathDuration => deathDuration;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
		}

		// Spawn, fresh or from the pool: undo whatever the previous life left (physics corpse, flash, telegraph), then pop in.
		// The Animator needs no reset: deactivating the object in the pool already sent it back to its default state.
		public void Appear(EnemyConfig config, HealthBarService healthBarService, int health, int maxHealth)
		{
			Config = config;
			_isDying = false;
			_lastPosition = transform.position;

			if (hitFeedback != null) hitFeedback.Stop();
			if (deathPhysics != null) deathPhysics.Restore();
			if (attackTelegraph != null) attackTelegraph.End();
			FaceHero(instantly: true);

			if (_healthBar == null) _healthBar = healthBarService.Create(transform, healthBarHeight, HealthBarTeam.Enemy);
			if (_healthBar != null) _healthBar.SetHealth(health, maxHealth);

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

		// isAttacking: the enemy is committed to an attack, so the flinch animation is skipped (it would cut the bite short).
		// The flash, squash, number and health bar still show the hit landed.
		public void PlayHit(int damage, int health, int maxHealth, bool isCritical, bool isAttacking)
		{
			if (animator != null && !isAttacking) animator.SetTrigger(HitHash);
			if (hitFeedback != null) hitFeedback.Play(damage, awayDirection: AwayFromHero(), isCritical: isCritical);
			if (_healthBar != null) _healthBar.SetHealth(health, maxHealth);
		}

		// Plays the death animation while the body is launched away from the hero as a physics corpse. The container releases it after DeathDuration.
		public void PlayDeath(int damage, bool isCritical)
		{
			StopForDeath();
			if (animator != null) animator.SetTrigger(DieHash);
			if (hitFeedback != null) hitFeedback.Play(damage, isKillingBlow: true, isCritical: isCritical);
			if (deathPhysics != null) deathPhysics.Play(AwayFromHero());

			// The orbs belong to the world, not to this enemy: they outlive it.
			GameFeelService gameFeelService = ServicesLocator.Instance.GetService<GameFeelService>();
			if (gameFeelService != null) gameFeelService.PlayCurrencyDrop(transform.position);
		}

		// Kamikaze hit: the body bursts in a puff and a spark against the hero. No number, no orbs: the player did not kill it.
		// The container releases it right away.
		public void PlayExplode()
		{
			StopForDeath();

			GameFeelService gameFeelService = ServicesLocator.Instance.GetService<GameFeelService>();
			if (gameFeelService == null) return;

			gameFeelService.PlaySpawnPuff(transform.position);
			gameFeelService.PlayHitSpark(transform.position + Vector3.up * 0.5f);
		}

		private void StopForDeath()
		{
			_isDying = true;
			if (spawnFeedback != null) spawnFeedback.Finish();
			if (attackTelegraph != null) attackTelegraph.End();
			if (_healthBar != null) _healthBar.Hide();
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

			FaceHero(instantly: false);
		}

		private void FaceHero(bool instantly)
		{
			if (_heroController == null) return;

			Vector3 direction = _heroController.CurrentState.Position - transform.position;
			direction.y = 0f;
			if (direction.sqrMagnitude <= 0.01f) return;

			Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
			transform.rotation = instantly ? targetRotation : Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		}

		// Movement is driven by the controller, so the view derives its speed from the actual displacement.
		private void UpdateAnimatorSpeed()
		{
			if (animator == null || Time.deltaTime <= 0f) return;

			float speed = _isDying ? 0f : (transform.position - _lastPosition).magnitude / Time.deltaTime;
			_lastPosition = transform.position;
			animator.SetFloat(SpeedHash, speed);
		}

		// Back to the pool by any path (kill, explosion, restart): the bar is not our child, so it must be hidden explicitly.
		private void OnDisable()
		{
			if (_healthBar != null) _healthBar.Hide();
		}

		private void OnDestroy()
		{
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
	}
}
