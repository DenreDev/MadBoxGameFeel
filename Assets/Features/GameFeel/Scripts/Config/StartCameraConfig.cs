using UnityEngine;

namespace Game.GameFeel
{
	// "Main menu" framing of the start screen: the camera sits low in front of the hero, close up, so the player sees his face.
	// The gameplay framing is the World prefab's own (Framing Transposer distance, virtual camera pitch).
	// Plugged into the StartCameraView that sits on the World prefab next to the virtual camera.
	[CreateAssetMenu(fileName = "StartCamera", menuName = "Feedback/Start Camera")]
	public class StartCameraConfig : ScriptableObject
	{
		[SerializeField]
		[Tooltip("Camera distance while waiting for the first tap. The Framing Transposer's Z Damping on the World prefab sets how fast the camera pulls back")]
		private float distance = 5f;
		[SerializeField]
		[Tooltip("Camera pitch in degrees on the start screen. Low = the camera sits in front of the hero and sees his face (0 = eye level, 90 = straight down)")]
		private float pitch = 12f;
		[SerializeField]
		[Tooltip("Seconds the camera takes to tilt between the start pitch and the gameplay pitch")]
		private float pitchSmoothTime = 0.6f;

		public float Distance => distance;
		public float Pitch => pitch;
		public float PitchSmoothTime => pitchSmoothTime;
	}
}
