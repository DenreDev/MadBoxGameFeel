using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Entities;
using Game.HealthBar;
using Core.ServicesManager;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.GamePlay.Enemies
{
	// Shows the enemies of EnemiesController. Views are pooled per enemy type (Unity's ObjectPool): with waves, enemies spawn and die
	// all game long, so a dead bee is deactivated and reused for the next spawn instead of being destroyed and instantiated again.
	public class EnemiesContainerView : MonoBehaviour
	{
		private EnemiesController _enemiesController;
		private HealthBarService _healthBarService;
		private EnemyAssetsService _enemyAssets;
		private Dictionary<int, EnemyView> _enemyViews;
		private readonly Dictionary<EnemyConfig, ObjectPool<EnemyView>> _pools = new Dictionary<EnemyConfig, ObjectPool<EnemyView>>();

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_enemiesController = entitiesService.EnemiesController;
			_healthBarService = ServicesLocator.Instance.GetService<HealthBarService>();
			_enemyAssets = ServicesLocator.Instance.GetService<EnemyAssetsService>();
			_enemyViews = new Dictionary<int, EnemyView>();

			_enemiesController.OnEnemySpawned += OnEnemySpawned;
			_enemiesController.OnEnemyRemoved += OnEnemyRemoved;
			_enemiesController.OnEnemyPositionChanged += OnEnemyPositionChanged;
			_enemiesController.OnEnemyWindupStarted += OnEnemyWindupStarted;
			_enemiesController.OnEnemyStrikeStarted += OnEnemyStrikeStarted;
			_enemiesController.OnEnemyDamaged += OnEnemyDamaged;
			_enemiesController.OnEnemyKilled += OnEnemyKilled;
			_enemiesController.OnEnemyExploded += OnEnemyExploded;
		}

		// The telegraph covers the whole reach: the attack range plus the lunge.
		private void OnEnemyWindupStarted(int enemyId, float duration)
		{
			if (!_enemyViews.TryGetValue(enemyId, out EnemyView enemyView)) return;
			if (!_enemiesController.Enemies.TryGetValue(enemyId, out EnemyState enemyState)) return;

			enemyView.PlayWindup(duration, enemyState.Config.AttackRange + enemyState.Config.LungeDistance);
		}

		private void OnEnemyStrikeStarted(int enemyId)
		{
			if (_enemyViews.TryGetValue(enemyId, out EnemyView enemyView)) enemyView.PlayStrike();
		}

		private void OnEnemyDamaged(int enemyId, int damage, bool isCritical)
		{
			if (!_enemyViews.TryGetValue(enemyId, out EnemyView enemyView)) return;
			if (!_enemiesController.Enemies.TryGetValue(enemyId, out EnemyState enemyState)) return;

			bool isAttacking = enemyState.AttackPhase != EnemyAttackPhase.None;
			enemyView.PlayHit(damage, enemyState.Health, enemyState.Config.InitialHealth, isCritical, isAttacking);
		}

		// Killed enemies leave the dictionary here and go back to the pool after the death animation,
		// so the OnEnemyRemoved that follows finds nothing. Restart (ClearAllEnemies) skips this and releases instantly.
		private void OnEnemyKilled(int enemyId, int damage, bool isCritical)
		{
			if (!_enemyViews.Remove(enemyId, out EnemyView enemyView)) return;

			enemyView.PlayDeath(damage, isCritical);
			ReleaseAfter(enemyView, enemyView.DeathDuration).Forget();
		}

		// Same as a kill, but the body bursts at once: nothing to watch.
		private void OnEnemyExploded(int enemyId)
		{
			if (!_enemyViews.Remove(enemyId, out EnemyView enemyView)) return;

			enemyView.PlayExplode();
			Release(enemyView);
		}

		private void OnEnemySpawned(EnemyState enemyState)
		{
			// The spawner only picks loaded enemies, so this only guards against a misconfiguration.
			if (!_enemyAssets.TryGetPrefab(enemyState.Config, out EnemyView prefab)) return;

			EnemyView enemyView = GetPool(enemyState.Config, prefab).Get();
			enemyView.transform.position = enemyState.Position;
			enemyView.Appear(enemyState.Config, _healthBarService, enemyState.Health, enemyState.Config.InitialHealth);
			_enemyViews[enemyState.Id] = enemyView;
		}

		private void OnEnemyRemoved(int enemyId)
		{
			if (_enemyViews.Remove(enemyId, out EnemyView enemyView)) Release(enemyView);
		}

		private void OnEnemyPositionChanged(EnemyState enemyState)
		{
			if (_enemyViews.TryGetValue(enemyState.Id, out EnemyView enemyView))
			{
				enemyView.transform.position = enemyState.Position;
			}
		}

		// One pool per enemy type, created on its first spawn. It grows to the largest crowd seen and never shrinks during the session.
		private ObjectPool<EnemyView> GetPool(EnemyConfig config, EnemyView prefab)
		{
			if (_pools.TryGetValue(config, out ObjectPool<EnemyView> pool)) return pool;

			pool = new ObjectPool<EnemyView>(
				createFunc: () => Instantiate(prefab, transform),
				actionOnGet: view => view.gameObject.SetActive(true),
				actionOnRelease: view => view.gameObject.SetActive(false),
				actionOnDestroy: view => Destroy(view.gameObject));
			_pools[config] = pool;
			return pool;
		}

		private void Release(EnemyView enemyView)
		{
			if (enemyView != null && _pools.TryGetValue(enemyView.Config, out ObjectPool<EnemyView> pool)) pool.Release(enemyView);
		}

		// Scaled time, like the death animation itself: a freeze frame holds the body on screen too.
		private async UniTaskVoid ReleaseAfter(EnemyView enemyView, float delay)
		{
			bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow();
			if (!cancelled) Release(enemyView);
		}

		private void OnDestroy()
		{
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemySpawned -= OnEnemySpawned;
				_enemiesController.OnEnemyRemoved -= OnEnemyRemoved;
				_enemiesController.OnEnemyPositionChanged -= OnEnemyPositionChanged;
				_enemiesController.OnEnemyWindupStarted -= OnEnemyWindupStarted;
				_enemiesController.OnEnemyStrikeStarted -= OnEnemyStrikeStarted;
				_enemiesController.OnEnemyDamaged -= OnEnemyDamaged;
				_enemiesController.OnEnemyKilled -= OnEnemyKilled;
				_enemiesController.OnEnemyExploded -= OnEnemyExploded;
			}
		}
	}
}
