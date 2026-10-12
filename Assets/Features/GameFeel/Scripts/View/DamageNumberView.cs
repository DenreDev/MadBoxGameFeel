using TMPro;
using UnityEngine;

namespace Game.GameFeel
{
	// One floating damage number: it slams in big and stretched, drifts up and to a random side, then shrinks and fades out.
	// Spawned by HitFeedbackView, which picks the colors and size of the hit, and destroys itself once faded.
	// The motion is tuned here, on the prefab. It always faces the camera.
	public class DamageNumberView : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text text;

		[Header("Timing")]
		[SerializeField, Tooltip("Seconds the number stays before it starts fading out, pop-in included")]
		private float lifetime = 0.8f;
		[SerializeField, Tooltip("Seconds of the pop-in, from big and stretched to its normal size")]
		private float popDuration = 0.2f;
		[SerializeField, Tooltip("Seconds of the fade out at the end")]
		private float fadeOutDuration = 0.25f;

		[Header("Pop in / fade out")]
		[SerializeField, Tooltip("Size at the very start of the pop-in, relative to the normal size")]
		private float popScale = 1.8f;
		[SerializeField, Tooltip("Extra vertical stretch at the very start of the pop-in (1 = none)")]
		private float popStretch = 1.35f;
		[SerializeField, Tooltip("Size at the very end of the fade out, relative to the normal size")]
		private float fadeOutScale = 0.55f;

		[Header("Drift")]
		[SerializeField, Tooltip("Sideways drift, random between x and y, to the left or the right, in world units")]
		private Vector2 driftSide = new Vector2(0.2f, 0.55f);
		[SerializeField, Tooltip("Upward drift, random between x and y, in world units")]
		private Vector2 driftUp = new Vector2(0.9f, 1.25f);
		[SerializeField, Tooltip("How fast the number eases toward its drift target (higher = snappier)")]
		private float driftSpeed = 6f;
		[SerializeField, Tooltip("Random tilt, between x and y degrees, to the left or the right")]
		private Vector2 tilt = new Vector2(5f, 12f);

		[Header("Scale by damage")]
		[SerializeField, Tooltip("Damage at or below which the number keeps its base size")]
		private int smallDamage = 15;
		[SerializeField, Tooltip("Damage at or above which the number gets Big Damage Scale")]
		private int bigDamage = 50;
		[SerializeField, Tooltip("Size multiplier of a big damage number")]
		private float bigDamageScale = 1.35f;

		private Camera _camera;
		private Vector3 _origin;
		private Vector3 _drift;
		private float _tilt;
		private float _scale;
		private float _age;

		// scale is the size picked by the hit (regular, kill, crit); the damage value makes it bigger on top of that.
		public void Play(int damage, string suffix, Color top, Color bottom, float scale)
		{
			if (_camera == null) _camera = Camera.main;

			text.SetText($"{damage}{suffix}");
			text.colorGradient = new VertexGradient(top, top, bottom, bottom);

			float damageProgress = Mathf.InverseLerp(smallDamage, bigDamage, damage);
			_scale = scale * Mathf.Lerp(1f, bigDamageScale, damageProgress);

			// Drift and tilt are picked in the camera's screen plane, so they read the same wherever the camera looks.
			Transform view = _camera != null ? _camera.transform : transform;
			float side = Random.Range(driftSide.x, driftSide.y) * RandomSign();
			_drift = view.right * side + view.up * Random.Range(driftUp.x, driftUp.y);
			_tilt = Random.Range(tilt.x, tilt.y) * RandomSign();
			_origin = transform.position;
			_age = 0f;

			Animate(); // the first rendered frame must already be in its pop-in pose
		}

		private void LateUpdate()
		{
			_age += Time.deltaTime;
			Animate();
		}

		private void Animate()
		{
			// fade goes 0 -> 1 during the pop-in, stays at 1, then 1 -> 0 during the fade out.
			float popIn = Mathf.Clamp01(_age / Mathf.Max(0.0001f, popDuration));
			float fadeOut = Mathf.Clamp01((_age - lifetime) / Mathf.Max(0.0001f, fadeOutDuration));
			Vector2 shape = _age < lifetime
				? Vector2.Lerp(new Vector2(popScale, popScale * popStretch), Vector2.one, popIn)
				: Vector2.one * Mathf.Lerp(1f, fadeOutScale, fadeOut);
			float fade = _age < lifetime ? popIn : 1f - fadeOut;

			transform.position = _origin + _drift * (1f - Mathf.Exp(-driftSpeed * _age));
			if (_camera != null) transform.rotation = _camera.transform.rotation * Quaternion.Euler(0f, 0f, _tilt);
			transform.localScale = new Vector3(shape.x, shape.y, 1f) * _scale;
			text.alpha = fade * fade;

			if (fadeOut >= 1f) Destroy(gameObject);
		}

		private static float RandomSign() => Random.value < 0.5f ? -1f : 1f;
	}
}
