using Core.ServicesManager;
using UnityEngine;

namespace Game.GameFeel
{
	// Footstep dust of the entity it sits on: a small puff at its feet every StepDistance of ground covered.
	// It only watches where the entity went, so no gameplay rule lives here. The entity's own view binds its FootstepFeedbackConfig.
	public class FootstepFeedbackView : MonoBehaviour
	{
		// A bigger move in one frame is a teleport (restart, initial placement), not a step.
		private const float TeleportDistance = 1f;

		private FootstepFeedbackConfig _config;
		private FeedbackAssetsService _assets;
		private Vector3 _lastPosition;
		private float _distanceSinceStep;

		// A null config disables the puffs.
		public void Bind(FootstepFeedbackConfig config)
		{
			_config = config;
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
		}

		private void OnEnable()
		{
			_lastPosition = transform.position;
			_distanceSinceStep = 0f;
		}

		private void Update()
		{
			Vector3 position = transform.position;
			Vector3 moved = position - _lastPosition;
			moved.y = 0f;
			_lastPosition = position;

			float distance = moved.magnitude;
			if (distance > TeleportDistance)
			{
				_distanceSinceStep = 0f;
				return;
			}

			if (_config == null || _config.StepDistance <= 0f) return;

			_distanceSinceStep += distance;
			if (_distanceSinceStep < _config.StepDistance) return;

			// Keep the remainder so the rhythm does not drift with the frame rate.
			_distanceSinceStep -= _config.StepDistance;

			if (_assets != null) _assets.Spawn(_config.PuffPrefab, position);
		}
	}
}
