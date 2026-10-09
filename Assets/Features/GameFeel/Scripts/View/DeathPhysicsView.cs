using UnityEngine;

namespace Game.GameFeel
{
	// Physics death for crowd enemies: the body becomes a real rigid body launched away from the killer. It tumbles, bounces on the floor
	// and bumps into the other corpses. Alive, the body is kinematic with its collider off: it just follows the entity root, which the gameplay
	// moves, and walking bees never bulldoze corpses. Living positions belong to EnemiesController, which shoves the survivors itself.
	// The rigid body sits on a wrapper above the animated model, so the hit squash and recoil on the model never fight the simulation.
	public class DeathPhysicsView : MonoBehaviour
	{
		[SerializeField, Tooltip("Rigid body wrapping the model. Kinematic in the prefab, released on death")]
		private Rigidbody body;
		[SerializeField, Tooltip("Collider of the corpse, off while alive")]
		private Collider shape;
		[SerializeField, Tooltip("Horizontal launch speed away from the killer, in m/s")]
		private float launchSpeed = 5f;
		[SerializeField, Tooltip("Upward launch speed in m/s: higher = taller hop")]
		private float launchUpSpeed = 4f;
		[SerializeField, Tooltip("Backward tumble speed in turns per second")]
		private float tumble = 1.5f;
		[SerializeField, Range(0f, 0.5f), Tooltip("Random variation of speed and tumble so deaths do not look identical")]
		private float variation = 0.2f;

		private Vector3 _startLocalPosition;
		private Quaternion _startLocalRotation;

		private void Awake()
		{
			_startLocalPosition = body.transform.localPosition;
			_startLocalRotation = body.transform.localRotation;
			body.isKinematic = true;
			shape.enabled = false;
		}

		// awayDirection: from the killer to this body. Falls back to straight behind the model when it is zero.
		public void Play(Vector3 awayDirection)
		{
			awayDirection.y = 0f;
			if (awayDirection.sqrMagnitude < 0.0001f) awayDirection = -body.transform.forward;
			Vector3 direction = awayDirection.normalized;
			float scale = 1f + Random.Range(-variation, variation);

			// Interpolation only while simulated: on a kinematic body moved through its parent it would make the model lag behind the root.
			shape.enabled = true;
			body.isKinematic = false;
			body.interpolation = RigidbodyInterpolation.Interpolate;
			body.velocity = (direction * launchSpeed + Vector3.up * launchUpSpeed) * scale;
			body.angularVelocity = Vector3.Cross(Vector3.up, direction) * (tumble * scale * 2f * Mathf.PI); // head goes back, away from the killer
		}

		// Back to a kinematic body where the prefab had it (pooled enemy coming back to life).
		public void Restore()
		{
			if (!body.isKinematic)
			{
				body.velocity = Vector3.zero;
				body.angularVelocity = Vector3.zero;
			}

			body.interpolation = RigidbodyInterpolation.None;
			body.isKinematic = true;
			shape.enabled = false;
			body.transform.localPosition = _startLocalPosition;
			body.transform.localRotation = _startLocalRotation;
		}
	}
}
