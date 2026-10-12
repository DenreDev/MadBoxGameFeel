using Game.GameFeel;
using TMPro;
using UnityEngine;

namespace Game.UI
{
	// Animates the wave announcement on unscaled time; WaveCounterView drives it.
	public class WaveBannerView : MonoBehaviour
	{
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

		private float _bannerTime;

		private float BannerDuration => bannerSlamDuration + bannerHoldDuration + bannerFadeDuration;

		private void Awake() => ResetVisuals();

		public void ShowWave(int waveNumber)
		{
			bannerLabel.text = $"WAVE {waveNumber}";
			_bannerTime = 0f;
		}

		public void ResetVisuals()
		{
			_bannerTime = BannerDuration;
			ApplyBanner();
		}

		private void Update()
		{
			_bannerTime += Time.unscaledDeltaTime;
			ApplyBanner();
		}

		// Slam (the text drops in from big while the ribbon opens), hold, then fade (the text drifts up while the ribbon closes).
		private void ApplyBanner()
		{
			float slam = Mathf.Clamp01(_bannerTime / bannerSlamDuration);
			float fade = Mathf.Clamp01((_bannerTime - bannerSlamDuration - bannerHoldDuration) / bannerFadeDuration);

			banner.alpha = Mathf.Clamp01(slam * 2f) * (1f - fade);
			bannerText.localScale = Vector3.one * Mathf.LerpUnclamped(bannerStartScale, 1f, Easing.OutBack(slam));
			bannerText.anchoredPosition = new Vector2(0f, bannerRiseDistance * Easing.OutCubic(fade));
			bannerRibbon.localScale = new Vector3(1f, Easing.OutCubic(slam) * (1f - fade * fade), 1f);
		}

	}
}
