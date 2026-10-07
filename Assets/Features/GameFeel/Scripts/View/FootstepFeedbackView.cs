using Core.ServicesManager;
using UnityEngine;

namespace Game.GameFeel
{
	// Footstep dust of the entity it sits on: a small puff at its feet every stepDistance of ground covered.
	// It only watches where the entity went, so it needs no call from the entity's own view and no gameplay rule lives here.
	public class FootstepFeedbackView : MonoBehaviour
	{
		// A bigger move in one frame is a teleport (restart, initial placement), not a step.
		private const float TeleportDistance = 1f;

		private Vector3 _lastPosition;
		private float _distanceSinceStep;

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

			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null || config.StepDistance <= 0f) return;

			_distanceSinceStep += distance;
			if (_distanceSinceStep < config.StepDistance) return;

			// Keep the remainder so the rhythm does not drift with the frame rate.
			_distanceSinceStep -= config.StepDistance;

			GameFeelService service = ServicesLocator.Instance.GetService<GameFeelService>();
			if (service != null) service.PlayStepPuff(position);
		}
	}
}
