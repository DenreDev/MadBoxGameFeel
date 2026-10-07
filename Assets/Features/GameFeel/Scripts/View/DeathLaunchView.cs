using UnityEngine;

namespace Game.GameFeel
{
	// Ragdoll-style death: the model is knocked away from the killer in a flipping hop, bounces once and lands,
	// while its Death animation plays. Moved as a simple rigid body (arc + flip around its center), no physics engine.
	// Only the model moves, never the entity root: the camera follows the hero root.
	public class DeathLaunchView : MonoBehaviour
	{
		[SerializeField, Tooltip("Animated model root that gets launched")]
		private Transform visual;
		[SerializeField, Tooltip("Meters travelled before the first landing")]
		private float distance = 3f;
		[SerializeField, Tooltip("Peak height of the hop in meters")]
		private float hopHeight = 1.2f;
		[SerializeField, Tooltip("Seconds in the air before the first landing")]
		private float airTime = 0.5f;
		[SerializeField, Tooltip("Full backflips while in the air (whole numbers, the body lands upright; 0 = none)")]
		private int flips = 1;
		[SerializeField, Range(0f, 0.9f), Tooltip("Speed kept on each landing. 0 = no bounce")]
		private float bounciness = 0.35f;
		[SerializeField, Range(0f, 0.5f), Tooltip("Random variation of distance and height so deaths do not look identical")]
		private float variation = 0.2f;

		private Vector3 _startLocalPosition;
		private Quaternion _startLocalRotation;

		private Vector3 _center; // body center, the point the model rotates around
		private Vector3 _velocity;
		private Vector3 _pivotOffset; // from the center to the model pivot (the feet)
		private Quaternion _startRotation;
		private Vector3 _flipAxis;
		private float _gravity;
		private float _floorY;
		private float _elapsed;

		private void Awake()
		{
			_startLocalPosition = visual.localPosition;
			_startLocalRotation = visual.localRotation;
			enabled = false; // only updates while launched
		}

		// awayDirection: from the killer to this body. Falls back to straight behind the model when it is zero.
		public void Play(Vector3 awayDirection)
		{
			awayDirection.y = 0f;
			if (awayDirection.sqrMagnitude < 0.0001f) awayDirection = -visual.forward;
			Vector3 direction = awayDirection.normalized;

			float scale = 1f + Random.Range(-variation, variation);
			_gravity = 8f * hopHeight * scale / (airTime * airTime); // peak height = gravity * airTime^2 / 8
			_velocity = direction * (distance * scale / airTime) + Vector3.up * (_gravity * airTime * 0.5f);

			_center = BodyCenter();
			_floorY = _center.y;
			_pivotOffset = visual.position - _center;
			_startRotation = visual.rotation;
			_flipAxis = Vector3.Cross(Vector3.up, direction); // head goes back, away from the killer
			_elapsed = 0f;
			enabled = true;
		}

		// Puts the model back where the prefab had it (hero restart).
		public void Restore()
		{
			enabled = false;
			visual.localPosition = _startLocalPosition;
			visual.localRotation = _startLocalRotation;
		}

		// LateUpdate so the Animator can never overwrite the launch.
		private void LateUpdate()
		{
			float deltaTime = Time.deltaTime;
			_elapsed += deltaTime;

			_velocity.y -= _gravity * deltaTime;
			_center += _velocity * deltaTime;

			if (_center.y <= _floorY && _velocity.y < 0f)
			{
				_center.y = _floorY;
				_velocity = new Vector3(_velocity.x * bounciness, -_velocity.y * bounciness, _velocity.z * bounciness);
				if (_velocity.y < 1f)
				{
					// Too small to hop again: finish the flip and stay where the body landed.
					enabled = false;
					_elapsed = airTime;
				}
			}

			// The flips are spread over the first flight, so the body is upright again at the first landing.
			float angle = 360f * flips * Mathf.Clamp01(_elapsed / airTime);
			Quaternion flip = Quaternion.AngleAxis(angle, _flipAxis);
			visual.SetPositionAndRotation(_center + flip * _pivotOffset, flip * _startRotation);
		}

		private Vector3 BodyCenter()
		{
			SkinnedMeshRenderer body = visual.GetComponentInChildren<SkinnedMeshRenderer>();
			return body != null ? body.bounds.center : visual.position + Vector3.up;
		}
	}
}
