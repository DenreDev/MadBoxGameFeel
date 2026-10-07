using Core.ServicesManager;
using Game.GameFlow;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
	// Full-screen "tap to start" layer, shown whenever the game is in the Start state (launch and after a restart).
	// The press starts the game on pointer down, not on release, to keep the response instant.
	public class StartScreenView : MonoBehaviour, IPointerDownHandler
	{
		[SerializeField] private CanvasGroup canvasGroup;
		[SerializeField] private RectTransform prompt;
		[SerializeField, Tooltip("Pulses per second of the prompt")]
		private float pulseSpeed = 0.8f;
		[SerializeField, Tooltip("How much the prompt grows at the top of a pulse (0.08 = 8%)")]
		private float pulseScale = 0.08f;
		[SerializeField, Tooltip("Seconds for the screen to fade in or out when the game state changes")]
		private float fadeDuration = 0.3f;

		private GameFlowService _gameFlowService;
		private float _targetAlpha = 1f;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_gameFlowService.OnStateChanged += OnGameStateChanged;
			OnGameStateChanged(_gameFlowService.State);
		}

		private void OnDestroy()
		{
			// The locator can already be gone when the scene is torn down.
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (_gameFlowService != null) _gameFlowService.StartGame();
		}

		private void OnGameStateChanged(GameState state)
		{
			bool isStart = state == GameState.Start;
			_targetAlpha = isStart ? 1f : 0f;
			canvasGroup.blocksRaycasts = isStart;
		}

		private void Update()
		{
			float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
			prompt.localScale = Vector3.one * (1f + pulse * pulseScale);

			canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha, Time.unscaledDeltaTime / fadeDuration);
		}
	}
}
