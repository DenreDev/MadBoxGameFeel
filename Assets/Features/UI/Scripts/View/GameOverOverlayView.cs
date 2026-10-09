using Core.ServicesManager;
using Game.GameFlow;
using Game.GamePlay.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	// Shown in the GameOver state. The button only asks the game flow to restart, resetting the round is not its job.
	// It plays a short sequence instead of popping: the screen dims, the title slams in, the round stats rise, then the restart button pops.
	// The button only accepts taps once it is visible, so a player still swiping the joystick when he dies cannot skip the screen by accident.
	public class GameOverOverlayView : MonoBehaviour
	{
		[SerializeField] private Button restartButton;
		[SerializeField] private CanvasGroup dimBackground;
		[SerializeField] private RectTransform title;
		[SerializeField] private CanvasGroup stats;
		[SerializeField] private TMP_Text statsText;
		[SerializeField] private RectTransform restartGroup;

		[SerializeField, Min(0f), Tooltip("Seconds the death plays before the screen starts showing")]
		private float startDelay = 0.6f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the screen to dim")]
		private float dimDuration = 0.5f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the title to slam down to its size")]
		private float titleSlamDuration = 0.3f;
		[SerializeField, Min(1f), Tooltip("Scale the title starts at before it slams in")]
		private float titleStartScale = 2.8f;
		[SerializeField, Min(0f), Tooltip("Seconds after the title before the stats rise in")]
		private float statsDelay = 0.35f;
		[SerializeField, Tooltip("Pixels the stats rise while they fade in")]
		private float statsRiseDistance = 40f;
		[SerializeField, Min(0f), Tooltip("Seconds after the stats before the restart button pops in")]
		private float buttonDelay = 0.4f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the restart button to pop in")]
		private float buttonPopDuration = 0.35f;

		private GameFlowService _gameFlowService;
		private EntitiesService _entitiesService;
		private int _wave;
		private int _kills;
		private float _shownTime;
		private RectTransform _statsRect;
		private Vector2 _statsRestPosition;

		private void Awake()
		{
			_statsRect = (RectTransform)stats.transform;
			_statsRestPosition = _statsRect.anchoredPosition;
		}

		private void Start()
		{
			gameObject.SetActive(false);
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_gameFlowService.OnStateChanged += OnGameStateChanged;

			// The round stats are counted here, from gameplay events: no gameplay class has to track them for the UI.
			_entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_entitiesService.WaveSpawner.OnWaveStarted += OnWaveStarted;
			_entitiesService.EnemiesController.OnEnemyKilled += OnEnemyKilled;

			if (restartButton != null)
			{
				restartButton.onClick.AddListener(OnRestartButtonClicked);
			}

			OnGameStateChanged(_gameFlowService.State);
		}

		private void OnWaveStarted(int wave) => _wave = wave;
		private void OnEnemyKilled(int enemyId, int damage, bool isCritical) => _kills++;

		private void OnGameStateChanged(GameState state)
		{
			if (state == GameState.Start)
			{
				_wave = 0;
				_kills = 0;
			}

			bool isGameOver = state == GameState.GameOver;
			gameObject.SetActive(isGameOver);
			if (!isGameOver) return;

			_shownTime = Time.unscaledTime;
			if (statsText != null) statsText.text = $"REACHED WAVE <color=#FFD84A>{Mathf.Max(1, _wave)}</color>\n<color=#FFD84A>{_kills}</color> {(_kills == 1 ? "KILL" : "KILLS")}";
			Animate(0f);
		}

		private void Update() => Animate(Time.unscaledTime - _shownTime);

		private void Animate(float elapsed)
		{
			float t = elapsed - startDelay;
			dimBackground.alpha = Mathf.Clamp01(t / dimDuration);

			// The title slams from big and transparent down to its size.
			float slam = Mathf.Clamp01(t / titleSlamDuration);
			title.localScale = Vector3.one * Mathf.Lerp(titleStartScale, 1f, slam * slam);
			title.gameObject.SetActive(t > 0f);

			float statsProgress = Mathf.Clamp01((t - titleSlamDuration - statsDelay) / 0.3f);
			stats.alpha = statsProgress;
			_statsRect.anchoredPosition = _statsRestPosition + Vector2.down * (1f - statsProgress) * statsRiseDistance;

			float buttonProgress = Mathf.Clamp01((t - titleSlamDuration - statsDelay - buttonDelay) / buttonPopDuration);
			restartGroup.localScale = Vector3.one * EaseOutBack(buttonProgress);
			restartButton.interactable = buttonProgress >= 1f;
		}

		// Overshoots past 1 then settles: the classic cartoon "pop".
		private static float EaseOutBack(float t)
		{
			const float overshoot = 1.9f;
			float u = t - 1f;
			return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
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
			if (_entitiesService != null)
			{
				_entitiesService.WaveSpawner.OnWaveStarted -= OnWaveStarted;
				_entitiesService.EnemiesController.OnEnemyKilled -= OnEnemyKilled;
			}
			if (restartButton != null)
			{
				restartButton.onClick.RemoveListener(OnRestartButtonClicked);
			}
		}
	}
}
