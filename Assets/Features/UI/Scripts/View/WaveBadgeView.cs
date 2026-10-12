using Game.GameFeel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	// Animates the current wave number on unscaled time; WaveCounterView drives it.
	public class WaveBadgeView : MonoBehaviour
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

		private Color _numberColor;
		private float _badgeTargetAlpha;
		private float _punchTime;

		private void Awake()
		{
			_numberColor = badgeNumberText.color;
			ResetVisuals();
		}

		public void ShowWave(int waveNumber)
		{
			badgeNumberText.text = waveNumber.ToString();
			_badgeTargetAlpha = 1f;
			_punchTime = 0f;
		}

		public void ResetVisuals()
		{
			_badgeTargetAlpha = 0f;
			_punchTime = punchDuration;
			ApplyPunch();
		}

		private void Update()
		{
			float deltaTime = Time.unscaledDeltaTime;

			badge.alpha = Mathf.MoveTowards(badge.alpha, _badgeTargetAlpha, deltaTime / badgeFadeDuration);

			_punchTime += deltaTime;
			ApplyPunch();
		}

		// The number pops big and white, then springs back past its size and settles. A ring grows off the badge and fades.
		private void ApplyPunch()
		{
			float t = Mathf.Clamp01(_punchTime / punchDuration);

			badgeNumber.localScale = Vector3.one * Mathf.LerpUnclamped(punchScale, 1f, Easing.OutBack(t));
			badgeNumberText.color = Color.Lerp(punchColor, _numberColor, Easing.OutCubic(t));

			badgePulse.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, Easing.OutCubic(t));
			Color ringColor = badgePulseImage.color;
			ringColor.a = pulseAlpha * (1f - t);
			badgePulseImage.color = ringColor;
		}
	}
}
