using UnityEngine;

namespace Game.GameFeel
{
	// The camera widens its field of view as the crowd grows, so the player still sees the enemies coming when the late waves pile up.
	// Plugged into the CrowdZoomView that sits on the World prefab next to the virtual camera.
	[CreateAssetMenu(fileName = "CrowdZoom", menuName = "Feedback/Crowd Zoom")]
	public class CrowdZoomConfig : ScriptableObject
	{
		[SerializeField]
		[Tooltip("Alive enemies at which the camera starts to zoom out")]
		private int startCount = 12;
		[SerializeField]
		[Tooltip("Alive enemies at which the zoom out is complete")]
		private int fullCount = 24;
		[SerializeField]
		[Tooltip("Degrees added to the camera's field of view at full zoom out. 0 disables the crowd zoom")]
		private float fovIncrease = 12f;
		[SerializeField]
		[Tooltip("Seconds the field of view takes to follow the crowd size: slow, so the camera breathes instead of pumping with each kill")]
		private float smoothTime = 1.5f;

		public int StartCount => startCount;
		public int FullCount => fullCount;
		public float FovIncrease => fovIncrease;
		public float SmoothTime => smoothTime;
	}
}
