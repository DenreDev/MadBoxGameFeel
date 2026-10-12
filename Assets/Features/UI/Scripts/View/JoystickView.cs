using Core.ServicesManager;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.JoystickInput
{
	public class JoystickView : MonoBehaviour
	{
		[SerializeField] private RectTransform joystickOuterStick;
		[SerializeField] private RectTransform joystickInnerStick;

		private Canvas _canvas;
		private HeroController _heroController;
		private bool _isHeroDead;

		private void Awake()
		{
			_canvas = GetComponentInParent<Canvas>();
			joystickOuterStick.gameObject.SetActive(false);
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			joystickInputService.OnStateChanged += OnJoystickStateChanged;

			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_heroController.OnStateChanged += OnHeroStateChanged;

			OnJoystickStateChanged(joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
		}

		private void OnDestroy()
		{
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			if(joystickInputService != null) joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			if (_heroController != null) _heroController.OnStateChanged -= OnHeroStateChanged;
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			_isHeroDead = heroState.IsDead;
			if (_isHeroDead)
			{
				joystickOuterStick.gameObject.SetActive(false);
			}
		}

		private void OnJoystickStateChanged(JoystickState state)
		{
			if (_isHeroDead)
			{
				joystickOuterStick.gameObject.SetActive(false);
				return;
			}

			bool wasVisible = joystickOuterStick.gameObject.activeSelf;
			joystickOuterStick.gameObject.SetActive(state.IsActive);
			if (!state.IsActive) return;

			if (!wasVisible) ApplyRingSize();
			UpdateJoystickVisuals(state);
		}

		// The ring is sized from the same radius the input uses, so the knob always stays under the finger.
		private void ApplyRingSize()
		{
			float diameterInCanvasUnits = JoystickInputConfig.Instance.MaxRadius * 2f / _canvas.scaleFactor;
			joystickOuterStick.sizeDelta = new Vector2(diameterInCanvasUnits, diameterInCanvasUnits);
		}

		// Overlay canvas: RectTransform.position is in screen pixels, like the input state.
		private void UpdateJoystickVisuals(JoystickState state)
		{
			joystickOuterStick.position = state.JoystickCenter;
			joystickInnerStick.position = state.JoystickCenter + state.MovementVector * JoystickInputConfig.Instance.MaxRadius;
		}
	}
}