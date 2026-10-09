using Core.ServicesManager;
using UnityEngine;

namespace Game.UI
{
	// Covers the screen from the very first frame until every service is initialized (Addressables preloads included),
	// so the player never sees an empty sky or a start prompt that does not answer yet.
	// Then the title zooms out and the cover fades, revealing the start screen. It runs once per launch: a restart does not reload services.
	// Its own sprites are direct references on purpose: it is what the player looks at while Addressables load.
	public class BootCoverView : MonoBehaviour
	{
		[SerializeField] private CanvasGroup canvasGroup;
		[SerializeField] private RectTransform title;
		[SerializeField] private RectTransform[] loadingDots;
		[SerializeField, Min(0f), Tooltip("Seconds the cover stays at least, so a fast load does not flash it")]
		private float minDisplayDuration = 0.5f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the cover to fade out once everything is ready")]
		private float fadeDuration = 0.45f;
		[SerializeField, Min(1f), Tooltip("Scale the title grows to while the cover fades out")]
		private float titleExitScale = 1.25f;
		[SerializeField, Tooltip("Pixels the loading dots jump")]
		private float dotBounceHeight = 18f;
		[SerializeField, Min(0.01f), Tooltip("Jumps per second of each loading dot")]
		private float dotBounceSpeed = 1.6f;

		private float _readyTime = -1f;
		private float[] _dotRestY;

		private void Awake()
		{
			canvasGroup.alpha = 1f;
			canvasGroup.blocksRaycasts = true;

			_dotRestY = new float[loadingDots.Length];
			for (int i = 0; i < loadingDots.Length; i++) _dotRestY[i] = loadingDots[i].anchoredPosition.y;
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnDestroy()
		{
			// The locator can already be gone when the scene is torn down.
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}

		// The fade waits for the minimum display time, counted from launch.
		private void OnServicesInitialized() => _readyTime = Mathf.Max(Time.unscaledTime, minDisplayDuration);

		private void Update()
		{
			float now = Time.unscaledTime;

			// Dots jump one after the other, like a wave.
			for (int i = 0; i < loadingDots.Length; i++)
			{
				float phase = now * dotBounceSpeed - i * 0.15f;
				float jump = Mathf.Max(0f, Mathf.Sin(phase * Mathf.PI * 2f));
				Vector2 position = loadingDots[i].anchoredPosition;
				position.y = _dotRestY[i] + jump * dotBounceHeight;
				loadingDots[i].anchoredPosition = position;
			}

			if (_readyTime < 0f || now < _readyTime) return;

			float progress = Mathf.Clamp01((now - _readyTime) / fadeDuration);
			canvasGroup.alpha = 1f - progress * progress;
			canvasGroup.blocksRaycasts = false;
			title.localScale = Vector3.one * Mathf.Lerp(1f, titleExitScale, 1f - (1f - progress) * (1f - progress));

			if (progress >= 1f) gameObject.SetActive(false);
		}
	}
}
