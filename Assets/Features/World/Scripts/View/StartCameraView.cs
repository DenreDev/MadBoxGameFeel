using Cinemachine;
using Core.ServicesManager;
using Game.GameFeel;
using Game.GameFlow;
using UnityEngine;

namespace Game.World
{
	// "Main menu" framing in the Start state (launch and after a restart): the camera sits low in front of the hero, close up, so the player sees his face.
	// When the game starts it eases back to the gameplay framing, which is whatever the World prefab is set to (distance on the Framing Transposer, pitch on the virtual camera).
	// The distance eases through the transposer's Z Damping, the pitch through the StartCameraConfig's PitchSmoothTime.
	public class StartCameraView : MonoBehaviour
	{
		[SerializeField] private CinemachineVirtualCamera virtualCamera;
		[SerializeField] private StartCameraConfig config;

		private CinemachineFramingTransposer _framing;
		private GameFlowService _gameFlowService;
		private float _gameplayPitch;
		private float _yaw;
		private float _gameplayDistance;
		private float _currentPitch;
		private float _targetPitch;
		private float _pitchVelocity;

		private void Awake()
		{
			_framing = virtualCamera != null ? virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>() : null;
			if (_framing == null || config == null)
			{
				Debug.LogWarning("Start camera needs a virtual camera with a Framing Transposer and a StartCameraConfig. The start screen keeps the gameplay framing.");
				_framing = null;
				return;
			}

			// Pitch and yaw come from the forward vector, which is unambiguous (eulerAngles can return an equivalent but different triple).
			Vector3 forward = virtualCamera.transform.forward;
			_gameplayPitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
			_yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
			_gameplayDistance = _framing.m_CameraDistance;

			// Starts in the Start state, snapped: no tilt-in at launch.
			_framing.m_CameraDistance = config.Distance;
			_currentPitch = _targetPitch = config.Pitch;
			ApplyPitch();
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_gameFlowService.OnStateChanged += OnGameStateChanged;
		}

		private void OnDestroy()
		{
			// The locator can already be gone when the scene is torn down.
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
		}

		// Game over keeps the gameplay framing: the camera only closes in on the start screen.
		private void OnGameStateChanged(GameState state)
		{
			if (_framing == null) return;

			if (state == GameState.Start)
			{
				_framing.m_CameraDistance = config.Distance;
				_targetPitch = config.Pitch;
			}
			else if (state == GameState.Playing)
			{
				_framing.m_CameraDistance = _gameplayDistance;
				_targetPitch = _gameplayPitch;
			}
		}

		// Runs before Cinemachine updates the camera in LateUpdate, which reads the virtual camera's rotation as its aim.
		private void Update()
		{
			if (_framing == null) return;

			_currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, config.PitchSmoothTime);
			ApplyPitch();
		}

		private void ApplyPitch()
		{
			virtualCamera.transform.rotation = Quaternion.Euler(_currentPitch, _yaw, 0f);
		}
	}
}
