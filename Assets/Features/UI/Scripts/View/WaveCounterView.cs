using Core.ServicesManager;
using Game.GameFeel;
using Game.GameFlow;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.UI
{
	// Entry point for the wave HUD. Child views own their visuals, this view owns event subscriptions.
	public class WaveCounterView : MonoBehaviour
	{
		[SerializeField] private WaveBadgeView badgeView;
		[SerializeField] private WaveBannerView bannerView;

		public WaveBadgeView Badge => badgeView;
		public WaveBannerView Banner => bannerView;

		private GameFlowService _gameFlowService;
		private FeedbackAssetsService _assets;
		private FeedbackAudioService _audio;
		private EnemyWaveSpawner _waveSpawner;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			_audio = ServicesLocator.Instance.GetService<FeedbackAudioService>();
			_waveSpawner = ServicesLocator.Instance.GetService<EntitiesService>().WaveSpawner;
			_waveSpawner.OnWaveStarted += OnWaveStarted;

			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_gameFlowService.OnStateChanged += OnGameStateChanged;
			OnGameStateChanged(_gameFlowService.State);
		}

		private void OnWaveStarted(int waveNumber)
		{
			if (badgeView != null) badgeView.ShowWave(waveNumber);
			if (bannerView != null)
			{
				bannerView.ShowWave(waveNumber);
				PlayWaveStinger();
			}
		}

		// The stinger is the waves' own feedback (EnemiesConfig.WaveSounds).
		private void PlayWaveStinger()
		{
			WaveSoundsConfig sounds = EnemiesConfig.Instance.WaveSounds;
			if (sounds != null && _audio != null) _audio.Play(_assets.Get(sounds.StartClip), sounds.StartVolume);
		}

		private void OnGameStateChanged(GameState state)
		{
			if (state != GameState.Start) return;
			if (badgeView != null) badgeView.ResetVisuals();
			if (bannerView != null) bannerView.ResetVisuals();
		}

		private void OnDestroy()
		{
			// The locator can already be gone when the scene is torn down.
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_waveSpawner != null) _waveSpawner.OnWaveStarted -= OnWaveStarted;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
		}
	}
}
