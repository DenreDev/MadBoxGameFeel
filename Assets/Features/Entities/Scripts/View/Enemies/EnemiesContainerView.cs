using System.Collections.Generic;
using Game.GamePlay.Entities;
using Game.HealthBar;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesContainerView : MonoBehaviour
	{
		private EnemiesController _enemiesController;
		private HealthBarService _healthBarService;
		private Dictionary<int, EnemyView> _enemyViews;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;
			_healthBarService = ServicesLocator.Instance.GetService<HealthBarService>();
			_enemyViews = new Dictionary<int, EnemyView>();

			_enemiesController.OnEnemySpawned += OnEnemySpawned;
			_enemiesController.OnEnemyRemoved += OnEnemyRemoved;
			_enemiesController.OnEnemyPositionChanged += OnEnemyPositionChanged;
			_enemiesController.OnEnemyAttacked += OnEnemyAttacked;
			_enemiesController.OnEnemyDamaged += OnEnemyDamaged;
			_enemiesController.OnEnemyKilled += OnEnemyKilled;
		}

		private void OnEnemyAttacked(int enemyId)
		{
			if (_enemyViews.TryGetValue(enemyId, out EnemyView enemyView)) enemyView.PlayAttack();
		}

		private void OnEnemyDamaged(int enemyId)
		{
			if (!_enemyViews.TryGetValue(enemyId, out EnemyView enemyView)) return;
			if (!_enemiesController.Enemies.TryGetValue(enemyId, out EnemyState enemyState)) return;

			enemyView.PlayHit(enemyState.Health, enemyState.Config.InitialHealth);
		}

		// Killed enemies leave the dictionary here and destroy themselves after the death animation,
		// so the OnEnemyRemoved that follows finds nothing. Restart (ClearAllEnemies) skips this and destroys instantly.
		private void OnEnemyKilled(int enemyId)
		{
			if (_enemyViews.Remove(enemyId, out EnemyView enemyView)) enemyView.PlayDeath();
		}

		private void OnEnemySpawned(EnemyState enemyState)
		{
			EnemyView enemyView = Instantiate(enemyState.Config.Prefab, transform);
			enemyView.transform.position = enemyState.Position;
			enemyView.CreateHealthBar(_healthBarService, enemyState.Health, enemyState.Config.InitialHealth);
			_enemyViews[enemyState.Id] = enemyView;
		}

		private void OnEnemyRemoved(int enemyId)
		{
			if (_enemyViews.Remove(enemyId, out EnemyView enemyView))
			{
				Destroy(enemyView.gameObject);
			}
		}

		private void OnEnemyPositionChanged(EnemyState enemyState)
		{
			if (_enemyViews.TryGetValue(enemyState.Id, out EnemyView enemyView))
			{
				enemyView.transform.position = enemyState.Position;
			}
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemySpawned -= OnEnemySpawned;
				_enemiesController.OnEnemyRemoved -= OnEnemyRemoved;
				_enemiesController.OnEnemyPositionChanged -= OnEnemyPositionChanged;
				_enemiesController.OnEnemyAttacked -= OnEnemyAttacked;
				_enemiesController.OnEnemyDamaged -= OnEnemyDamaged;
				_enemiesController.OnEnemyKilled -= OnEnemyKilled;
			}
		}
	}
}