using Core.ServicesManager;
using Game.GameFeel;
using Game.GameFlow;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	// Faint ring on the ground under the hero, so his position reads at a glance among the enemies.
	// Hidden on the start screen (the camera looks at the hero from the front, the ring would be an edge-on line) and while he is dead.
	// The ring prefab is HeroConfig's GroundRingPrefab. Size and alpha live on the prefab and its material, nothing here is gameplay.
	public class HeroRingView : MonoBehaviour
	{
		private GameObject _ring;
		private HeroController _heroController;
		private GameFlowService _gameFlowService;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			FeedbackAssetsService assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			if (assets == null) return;

			_ring = assets.Spawn(HeroConfig.Instance.GroundRingPrefab, transform);
			if (_ring == null) return;

			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_heroController.OnStateChanged += OnHeroStateChanged;
			_gameFlowService.OnStateChanged += OnGameStateChanged;

			Refresh();
		}

		private void OnDestroy()
		{
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_heroController != null) _heroController.OnStateChanged -= OnHeroStateChanged;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
		}

		private void OnHeroStateChanged(HeroState state) => Refresh();
		private void OnGameStateChanged(GameState state) => Refresh();

		private void Refresh()
		{
			bool visible = _gameFlowService.State != GameState.Start && !_heroController.CurrentState.IsDead;
			if (_ring.activeSelf != visible) _ring.SetActive(visible);
		}
	}
}
