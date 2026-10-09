using Core.ServicesManager;
using UnityEngine;

namespace Game.GameFeel
{
	// Spawn reaction of the entity it sits on: a dust puff at its feet while the body pops in from nothing with a bit of overshoot.
	// The entity's own view binds its SpawnFeedbackConfig and calls Play() when it appears, so no gameplay rule lives here.
	// It scales the entity root, while the hit squash scales the model underneath, so the two never fight over the same transform.
	public class SpawnFeedbackView : MonoBehaviour
	{
		private SpawnFeedbackConfig _config;
		private Vector3 _baseScale;
		private float _spawnTime;

		private void Awake()
		{
			_baseScale = transform.localScale;
			enabled = false; // only updates while the pop plays
		}

		// A null config disables the reaction: the entity just appears.
		public void Bind(SpawnFeedbackConfig config) => _config = config;

		public void Play()
		{
			if (_config == null || _config.Duration <= 0f) return;

			_spawnTime = Time.time;
			transform.localScale = Vector3.zero; // the first rendered frame must not be full size
			enabled = true;

			FeedbackAssetsService assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			if (assets != null) assets.Spawn(_config.PuffPrefab, transform.position);
		}

		// Snaps to full size, for an enemy that dies in the middle of its pop.
		public void Finish()
		{
			transform.localScale = _baseScale;
			enabled = false;
		}

		private void Update()
		{
			float progress = Mathf.Clamp01((Time.time - _spawnTime) / _config.Duration);
			transform.localScale = _baseScale * EaseOutBack(progress, _config.Overshoot);

			if (progress >= 1f) enabled = false;
		}

		// 0 -> 1 with a swing past 1 that settles back. overshoot = 0 is a plain ease-out.
		private static float EaseOutBack(float t, float overshoot)
		{
			float u = t - 1f;
			return 1f + u * u * ((overshoot + 1f) * u + overshoot);
		}
	}
}
