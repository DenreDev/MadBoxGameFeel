using Core.ServicesManager;
using Game.GameFeel;
using Game.GameFlow;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	// Wave HUD: a small "WAVE n" badge at the top of the screen, plus a big banner that slams in when a wave starts.
	// It only listens to EnemyWaveSpawner.OnWaveStarted: the spawner knows nothing about it.
	// Everything runs on unscaled time, so the hit-stop freeze frames do not stall the animation.
	public class WaveCounterView : MonoBehaviour
	{
		[Header("Badge")]
		[SerializeField] private CanvasGroup badge;
		[SerializeField] private RectTransform badgeNumber;
		[SerializeField] private TMP_Text badgeNumberText;
		[SerializeField] private RectTransform badgePulse;
		[SerializeField] private Image badgePulseImage;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the badge to fade in when the first wave starts, or out when the round resets")]
		private float badgeFadeDuration = 0.3f;
		[SerializeField, Min(1f), Tooltip("Scale of the number the moment it changes (1.6 = 60% bigger), then it springs back")]
		private float punchScale = 1.7f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the number to settle back after a change")]
		private float punchDuration = 0.5f;
		[SerializeField, Tooltip("Color of the number the moment it changes, it fades back to its normal color")]
		private Color punchColor = Color.white;
		[SerializeField, Min(1f), Tooltip("How much the ring around the badge grows while it fades out (2 = twice the badge size)")]
		private float pulseScale = 1.6f;
		[SerializeField, Range(0f, 1f), Tooltip("Opacity of the ring at the start of the pulse")]
		private float pulseAlpha = 0.7f;

		[Header("Banner")]
		[SerializeField] private CanvasGroup banner;
		[SerializeField] private RectTransform bannerRibbon;
		[SerializeField] private RectTransform bannerText;
		[SerializeField] private TMP_Text bannerLabel;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the banner text to slam in")]
		private float bannerSlamDuration = 0.28f;
		[SerializeField, Min(1f), Tooltip("Scale the banner text starts at before it slams down to its size")]
		private float bannerStartScale = 2.6f;
		[SerializeField, Min(0f), Tooltip("Seconds the banner stays readable once it landed")]
		private float bannerHoldDuration = 0.9f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the banner to fade out")]
		private float bannerFadeDuration = 0.4f;
		[SerializeField, Tooltip("Pixels the banner text drifts up while it fades out")]
		private float bannerRiseDistance = 70f;

		private GameFlowService _gameFlowService;
		private GameFeelService _gameFeelService;
		private EnemyWaveSpawner _waveSpawner;
		private Color _numberColor;
		private float _badgeTargetAlpha;
		private float _punchTime;
		private float _bannerTime;

		private float BannerDuration => bannerSlamDuration + bannerHoldDuration + bannerFadeDuration;

		private void Awake()
		{
			_numberColor = badgeNumberText.color;
			ResetVisuals();
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_gameFeelService = ServicesLocator.Instance.GetService<GameFeelService>();

			_waveSpawner = ServicesLocator.Instance.GetService<EntitiesService>().WaveSpawner;
			_waveSpawner.OnWaveStarted += OnWaveStarted;

			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_gameFlowService.OnStateChanged += OnGameStateChanged;
		}

		private void OnDestroy()
		{
			// The locator can already be gone when the scene is torn down.
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_waveSpawner != null) _waveSpawner.OnWaveStarted -= OnWaveStarted;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
		}

		private void OnWaveStarted(int waveNumber)
		{
			string number = waveNumber.ToString();
			badgeNumberText.text = number;
			bannerLabel.text = $"WAVE {number}";

			_badgeTargetAlpha = 1f;
			_punchTime = 0f;
			_bannerTime = 0f;

			if (_gameFeelService != null) _gameFeelService.PlayWaveStart();
		}

		// Back on the start screen: nothing to count yet, the next round begins at wave 1.
		private void OnGameStateChanged(GameState state)
		{
			if (state == GameState.Start) ResetVisuals();
		}

		private void ResetVisuals()
		{
			_badgeTargetAlpha = 0f;
			_punchTime = punchDuration;
			_bannerTime = BannerDuration;
			ApplyBanner();
			ApplyPunch();
		}

		private void Update()
		{
			float deltaTime = Time.unscaledDeltaTime;

			badge.alpha = Mathf.MoveTowards(badge.alpha, _badgeTargetAlpha, deltaTime / badgeFadeDuration);

			_punchTime += deltaTime;
			ApplyPunch();

			_bannerTime += deltaTime;
			ApplyBanner();
		}

		// The number pops big and white, then springs back past its size and settles. A ring grows off the badge and fades.
		private void ApplyPunch()
		{
			float t = Mathf.Clamp01(_punchTime / punchDuration);

			badgeNumber.localScale = Vector3.one * Mathf.LerpUnclamped(punchScale, 1f, EaseOutBack(t));
			badgeNumberText.color = Color.Lerp(punchColor, _numberColor, EaseOutCubic(t));

			badgePulse.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, EaseOutCubic(t));
			Color ringColor = badgePulseImage.color;
			ringColor.a = pulseAlpha * (1f - t);
			badgePulseImage.color = ringColor;
		}

		// Slam (the text drops in from big while the ribbon opens), hold, then fade (the text drifts up while the ribbon closes).
		private void ApplyBanner()
		{
			float slam = Mathf.Clamp01(_bannerTime / bannerSlamDuration);
			float fade = Mathf.Clamp01((_bannerTime - bannerSlamDuration - bannerHoldDuration) / bannerFadeDuration);

			banner.alpha = Mathf.Clamp01(slam * 2f) * (1f - fade);
			bannerText.localScale = Vector3.one * Mathf.LerpUnclamped(bannerStartScale, 1f, EaseOutBack(slam));
			bannerText.anchoredPosition = new Vector2(0f, bannerRiseDistance * EaseOutCubic(fade));
			bannerRibbon.localScale = new Vector3(1f, EaseOutCubic(slam) * (1f - fade * fade), 1f);
		}

		private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

		// Overshoots 1 before settling, which gives the slam its bounce.
		private static float EaseOutBack(float t)
		{
			const float overshoot = 1.70158f;
			float shifted = t - 1f;
			return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
		}
	}
}
