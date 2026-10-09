using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
	// Makes a button feel physical: it squashes under the finger and springs back with a small overshoot when released.
	// Pure presentation, it does not handle the click itself. Runs on unscaled time so a freeze frame never sticks it.
	public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
	{
		[SerializeField, Tooltip("What squashes. Leave empty to use this object")]
		private RectTransform target;
		[SerializeField, Range(0.5f, 1f), Tooltip("Scale while the finger is down (0.9 = 10% smaller)")]
		private float pressedScale = 0.9f;
		[SerializeField, Min(1f), Tooltip("Stiffness of the spring, higher snaps faster")]
		private float stiffness = 600f;
		[SerializeField, Range(0f, 1f), Tooltip("How fast the wobble dies out, lower wobbles longer")]
		private float damping = 0.35f;

		private float _scale = 1f;
		private float _velocity;
		private bool _isPressed;

		private RectTransform Target => target != null ? target : (RectTransform)transform;

		private void OnDisable()
		{
			_isPressed = false;
			_scale = 1f;
			_velocity = 0f;
			Target.localScale = Vector3.one;
		}

		public void OnPointerDown(PointerEventData eventData) => _isPressed = true;
		public void OnPointerUp(PointerEventData eventData) => _isPressed = false;
		public void OnPointerExit(PointerEventData eventData) => _isPressed = false;

		private void Update()
		{
			// Damped spring toward the target scale: the release overshoots a little, which reads as bouncy.
			float goal = _isPressed ? pressedScale : 1f;
			float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
			float criticalDamping = 2f * Mathf.Sqrt(stiffness);
			_velocity += ((goal - _scale) * stiffness - _velocity * criticalDamping * damping) * dt;
			_scale += _velocity * dt;
			Target.localScale = Vector3.one * _scale;
		}
	}
}
