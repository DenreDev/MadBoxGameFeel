using Core.ServicesManager;
using Game.GameFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	// Shown in the GameOver state. The button only asks the game flow to restart, resetting the round is not its job.
	public class GameOverOverlayView : MonoBehaviour
	{
		[SerializeField] private Button restartButton;

		private GameFlowService _gameFlowService;

		private void Start()
		{
			gameObject.SetActive(false);
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_gameFlowService.OnStateChanged += OnGameStateChanged;

			if (restartButton != null)
			{
				restartButton.onClick.AddListener(OnRestartButtonClicked);
			}

			OnGameStateChanged(_gameFlowService.State);
		}

		private void OnGameStateChanged(GameState state)
		{
			gameObject.SetActive(state == GameState.GameOver);
		}

		private void OnRestartButtonClicked()
		{
			_gameFlowService.Restart();
		}

		private void OnDestroy()
		{
			// The locator can already be gone when the scene is torn down.
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_gameFlowService != null)
			{
				_gameFlowService.OnStateChanged -= OnGameStateChanged;
			}
			if (restartButton != null)
			{
				restartButton.onClick.RemoveListener(OnRestartButtonClicked);
			}
		}
	}
}
