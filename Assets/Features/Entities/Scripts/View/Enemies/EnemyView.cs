using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
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

		public void PlayAttack()
		{
			if (animator != null) animator.SetTrigger(AttackHash);
		}

		public void PlayHit()
		{
			if (animator != null) animator.SetTrigger(HitHash);
		}

		// Plays the death animation then destroys the enemy.
		public void PlayDeath()
		{
			_isDying = true;
			if (animator != null) animator.SetTrigger(DieHash);
			Destroy(gameObject, deathDuration);
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
