using UnityEngine;

namespace Game.HealthBar
{
	// World-space bar floating above a target. Follows it, faces the camera and animates health changes.
	// Not parented to the target so it ignores the target's scale and rotation.
	public class HealthBarView : MonoBehaviour
	{
		[SerializeField] private GameObject visual;
		[SerializeField, Tooltip("Left-anchored transform, scale.x = width * ratio")] private Transform fill;
		[SerializeField, Tooltip("Left-anchored transform, scale.x = width * ratio")] private Transform trail;
		[SerializeField] private SpriteRenderer fillRenderer;
		[SerializeField] private SpriteRenderer trailRenderer;
		[SerializeField] private float width = 1f;
		[SerializeField] private float height = 0.14f;

		private HealthBarConfig _config;
		private Transform _target;
		private float _targetHeight;
		private Color _fillColor;
		private float _scale;
		private bool _hideWhenFull;
		private bool _hasHealth; // false until the first value and after death, so the next value is not animated
		private Camera _camera;

		private float _healthRatio; // real health
		private float _fillRatio; // displayed, slides down to the real health
		private float _fillVelocity;
		private float _trailRatio; // displayed, waits then drains to the fill
		private float _trailHoldEnd;
		private float _hitTime;

		public void Bind(HealthBarConfig config, Transform target, float targetHeight, Color fillColor, float scale, bool hideWhenFull)
		{
			_config = config;
			_target = target;
			_targetHeight = targetHeight;
			_fillColor = fillColor;
			_scale = scale;
			_hideWhenFull = hideWhenFull;

			trailRenderer.color = config.TrailColor;
			Hide();
			FollowTarget();
		}

		public void Hide() => visual.SetActive(false);

		public void SetHealth(int health, int maxHealth)
		{
			// Dead entities lose their bar.
			if (health <= 0)
			{
				_hasHealth = false;
				Hide();
				return;
			}

			float ratio = Mathf.Clamp01(health / (float)Mathf.Max(1, maxHealth));

			// First value, revive or heal: jump straight to it, only damage is animated.
			if (!_hasHealth || ratio > _healthRatio)
			{
				_hasHealth = true;
				_healthRatio = _fillRatio = _trailRatio = ratio;
				_fillVelocity = 0f;
				_hitTime = float.NegativeInfinity;
			}
			else if (!Mathf.Approximately(ratio, _healthRatio))
			{
				_healthRatio = ratio;
				_hitTime = Time.time;
				_trailHoldEnd = Time.time + _config.TrailDelay;
			}

			bool hidden = _hideWhenFull && ratio >= 1f;
			visual.SetActive(!hidden);
			if (!hidden) Animate();
		}

		private void LateUpdate()
		{
			// The target is gone (enemy destroyed or restart), so the bar goes with it.
			if (_target == null)
			{
				Destroy(gameObject);
				return;
			}

			FollowTarget();
			if (visual.activeSelf) Animate();
		}

		private void FollowTarget()
		{
			if (_camera == null) _camera = Camera.main;
			if (_camera == null)
			{
				transform.position = _target.position + Vector3.up * _targetHeight;
				return;
			}

			// Offset along the camera's up axis so the bar clears the head on screen, whatever the camera tilt.
			transform.rotation = _camera.transform.rotation;
			transform.position = _target.position + _camera.transform.up * _targetHeight;
		}

		private void Animate()
		{
			_fillRatio = Mathf.SmoothDamp(_fillRatio, _healthRatio, ref _fillVelocity, _config.FillSmoothTime);

			if (Time.time >= _trailHoldEnd)
			{
				_trailRatio = Mathf.MoveTowards(_trailRatio, _fillRatio, _config.TrailDrainSpeed * Time.deltaTime);
			}
			_trailRatio = Mathf.Max(_trailRatio, _fillRatio);

			SetBarRatio(fill, _fillRatio);
			SetBarRatio(trail, _trailRatio);

			float sinceHit = Time.time - _hitTime;
			float flash = 1f - Mathf.Clamp01(sinceHit / Mathf.Max(0.0001f, _config.FlashDuration));
			fillRenderer.color = Color.Lerp(_fillColor, _config.FlashColor, flash);

			// Pops instantly on hit then eases back to normal size.
			float punch = 1f - Mathf.Clamp01(sinceHit / Mathf.Max(0.0001f, _config.PunchDuration));
			transform.localScale = Vector3.one * (_scale * (1f + _config.PunchScale * punch * punch));
		}

		private void SetBarRatio(Transform bar, float ratio)
		{
			bar.localScale = new Vector3(width * ratio, height, 1f);
		}
	}
}
