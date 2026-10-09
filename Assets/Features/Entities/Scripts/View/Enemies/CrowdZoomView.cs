using Cinemachine;
using Core.ServicesManager;
using Game.GameFeel;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// Widens the camera's field of view as the crowd grows, so the player still sees the enemies coming when the late waves pile up.
	// Sits on the World prefab next to the virtual camera. It only touches the lens: StartCameraView owns the distance and the pitch.
	// The spawner reads the real camera, so a wider view also pushes the spawn points out.
	public class CrowdZoomView : MonoBehaviour
	{
		[SerializeField] private CinemachineVirtualCamera virtualCamera;

		private EnemiesController _enemiesController;
		private float _baseFov;
		private float _fovVelocity;

		private void Awake()
		{
			if (virtualCamera != null) _baseFov = virtualCamera.m_Lens.FieldOfView;
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;
		}

		private void OnDestroy()
		{
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}

		// Before Cinemachine reads the lens in LateUpdate.
		private void Update()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (virtualCamera == null || _enemiesController == null || config == null) return;

			float crowd = Mathf.InverseLerp(config.CrowdZoomStartCount, config.CrowdZoomFullCount, _enemiesController.Enemies.Count);
			float targetFov = _baseFov + config.CrowdZoomFovIncrease * crowd;
			virtualCamera.m_Lens.FieldOfView = Mathf.SmoothDamp(virtualCamera.m_Lens.FieldOfView, targetFov, ref _fovVelocity, config.CrowdZoomSmoothTime);
		}
	}
}
