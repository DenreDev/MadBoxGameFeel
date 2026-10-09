using Cinemachine;
using Core.ServicesManager;
using Game.GameFeel;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	// Small camera kicks when the hero swings, takes a hit and dies. Sits on the World prefab next to the virtual camera,
	// which carries the CinemachineImpulseListener. Strengths live in GameFeelConfig.
	public class CameraShakeView : MonoBehaviour
	{
		[SerializeField] private CinemachineImpulseSource impulseSource;

		private HeroController _heroController;
		private EnemiesController _enemiesController;
		private float _lastHitShakeTime = float.NegativeInfinity;
		private int _lastEnemyShakeFrame = -1;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_heroController = entitiesService.HeroController;
			_enemiesController = entitiesService.EnemiesController;
			_enemiesController.OnEnemyDamaged += OnEnemyDamaged;
			_enemiesController.OnEnemyKilled += OnEnemyKilled;
			_heroController.OnAttacked += OnHeroAttacked;
			_heroController.OnDamaged += OnHeroDamaged;
			_heroController.OnDied += OnHeroDied;
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemyDamaged -= OnEnemyDamaged;
				_enemiesController.OnEnemyKilled -= OnEnemyKilled;
			}
			if (_heroController != null)
			{
				_heroController.OnAttacked -= OnHeroAttacked;
				_heroController.OnDamaged -= OnHeroDamaged;
				_heroController.OnDied -= OnHeroDied;
			}
		}

		private void OnHeroAttacked(Vector3 targetPosition) => Shake(GameFeelConfig.Instance.AttackShake);
		private void OnHeroDied() => Shake(GameFeelConfig.Instance.DeathShake);
		private void OnEnemyDamaged(int enemyId, int damage, bool isCritical) => ShakeAlongSlash(enemyId, GameFeelConfig.Instance.EnemyHitShake, isCritical);
		private void OnEnemyKilled(int enemyId, int damage, bool isCritical) => ShakeAlongSlash(enemyId, GameFeelConfig.Instance.EnemyKillShake, isCritical);

		private void OnHeroDamaged()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (Time.time - _lastHitShakeTime < config.HitShakeCooldown) return;

			_lastHitShakeTime = Time.time;
			Shake(config.HitShake);
		}

		// The kick goes the way the sword went (hero toward the enemy it hit), so the screen takes the blow too.
		// The enemy is still in the controller when these events are raised.
		// One swing can hit several enemies in the same frame: only the first one kicks the screen, so a multi-hit is not a bigger shake.
		// A critical strike kicks harder.
		private void ShakeAlongSlash(int enemyId, float strength, bool isCritical)
		{
			if (Time.frameCount == _lastEnemyShakeFrame) return;
			_lastEnemyShakeFrame = Time.frameCount;
			if (isCritical) strength *= GameFeelConfig.Instance.CritShakeMultiplier;

			if (_enemiesController.Enemies.TryGetValue(enemyId, out EnemyState enemy))
			{
				Vector3 direction = enemy.Position - _heroController.CurrentState.Position;
				direction.y = 0f;
				if (direction.sqrMagnitude > 0.0001f)
				{
					Shake(strength, direction.normalized);
					return;
				}
			}

			Shake(strength);
		}

		// Random kick along the ground plane, so every shake goes a different way.
		private void Shake(float strength)
		{
			Vector2 direction = Random.insideUnitCircle.normalized;
			Shake(strength, new Vector3(direction.x, 0f, direction.y));
		}

		private void Shake(float strength, Vector3 direction)
		{
			if (impulseSource == null || strength <= 0f) return;

			impulseSource.GenerateImpulseWithVelocity(direction * strength);
		}
	}
}
