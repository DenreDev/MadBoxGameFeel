using Core.ServicesManager;
using Game.GameFlow;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
	// Full-screen "tap to start" layer, shown whenever the game is in the Start state (launch and after a restart).
	// The press starts the game on pointer down, not on release, to keep the response instant.
	// Showing it drops the title in and pops the prompt after it; leaving it punches the prompt, flies the title up and fades everything.
	public class StartScreenView : MonoBehaviour, IPointerDownHandler
	{
		[SerializeField] private CanvasGroup canvasGroup;
		[SerializeField] private RectTransform prompt;
		[SerializeField] private RectTransform title;
		[SerializeField] private CanvasGroup hint;
		[SerializeField, Tooltip("Pulses per second of the prompt")]
		private float pulseSpeed = 0.8f;
		[SerializeField, Tooltip("How much the prompt grows at the top of a pulse (0.08 = 8%)")]
		private float pulseScale = 0.08f;
		[SerializeField, Tooltip("Seconds for the screen to fade out when the game starts")]
		private float fadeDuration = 0.3f;

		[Header("Intro")]
		[SerializeField, Min(0f), Tooltip("Seconds before the title starts dropping in, leaves room for the boot cover to fade")]
		private float titleDelay = 0.2f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the title to drop in and settle")]
		private float titleDropDuration = 0.55f;
		[SerializeField, Tooltip("Pixels above its place the title starts from")]
		private float titleDropHeight = 420f;
		[SerializeField, Min(0f), Tooltip("Seconds after the title starts before the prompt pops in")]
		private float promptDelay = 0.45f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the prompt to pop in")]
		private float promptPopDuration = 0.4f;
		[SerializeField, Tooltip("Degrees the title sways while waiting, keeps the screen alive")]
		private float titleSwayAngle = 2.5f;

		[Header("Exit")]
		[SerializeField, Min(1f), Tooltip("Scale of the prompt the moment the player taps")]
		private float tapPunchScale = 1.3f;
		[SerializeField, Tooltip("Pixels the title flies up while the screen fades out")]
		private float titleExitRise = 260f;

		private GameFlowService _gameFlowService;
		private Vector2 _titleRestPosition;
		private bool _isShown = true;
		private float _stateTime; // unscaled time when the screen was last shown or hidden
		private float _hiddenAlpha; // alpha when the exit started, so a fast hide never pops

		private void Awake()
		{
			_titleRestPosition = title.anchoredPosition;
			// Hidden until the services answer: the boot cover is on top meanwhile, and the intro must play from its start.
			_stateTime = float.MaxValue;
			ApplyIntro(0f);
		}

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
			if (isStart == _isShown && _stateTime != float.MaxValue) return;

			_isShown = isStart;
			_stateTime = Time.unscaledTime;
			_hiddenAlpha = canvasGroup.alpha;
			canvasGroup.blocksRaycasts = isStart;
		}

		private void Update()
		{
			if (_stateTime == float.MaxValue) return;

			float elapsed = Time.unscaledTime - _stateTime;
			if (_isShown) ApplyIntro(elapsed);
			else ApplyExit(elapsed);
		}

		private void ApplyIntro(float elapsed)
		{
			canvasGroup.alpha = 1f;

			float titleProgress = Mathf.Clamp01((elapsed - titleDelay) / titleDropDuration);
			title.anchoredPosition = _titleRestPosition + Vector2.up * (1f - EaseOutBack(titleProgress)) * titleDropHeight;
			float sway = Mathf.Sin(Time.unscaledTime * 1.3f) * titleSwayAngle * titleProgress;
			title.localRotation = Quaternion.Euler(0f, 0f, sway);
			title.localScale = Vector3.one;

			float promptProgress = Mathf.Clamp01((elapsed - titleDelay - promptDelay) / promptPopDuration);
			float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
			prompt.localScale = Vector3.one * (EaseOutBack(promptProgress) * (1f + pulse * pulseScale));

			if (hint != null) hint.alpha = Mathf.Clamp01((elapsed - titleDelay - promptDelay - promptPopDuration) / 0.3f);
		}

		private void ApplyExit(float elapsed)
		{
			float progress = Mathf.Clamp01(elapsed / fadeDuration);
			canvasGroup.alpha = _hiddenAlpha * (1f - progress);

			// The prompt answers the tap with a punch that shrinks away, the title flies off the top.
			prompt.localScale = Vector3.one * Mathf.Lerp(tapPunchScale, 0.6f, progress);
			title.anchoredPosition = _titleRestPosition + Vector2.up * progress * progress * titleExitRise;
		}

		// Overshoots past 1 then settles: the classic cartoon "pop".
		private static float EaseOutBack(float t)
		{
			const float overshoot = 1.9f;
			float u = t - 1f;
			return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
		}
	}
}
