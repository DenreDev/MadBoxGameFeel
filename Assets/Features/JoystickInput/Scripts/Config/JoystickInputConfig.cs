using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.JoystickInput
{
	[CreateAssetMenu(fileName = "JoystickInputConfig", menuName = "Game/JoystickInputConfig")]
	public class JoystickInputConfig : ScriptableObjectSingleton<JoystickInputConfig>
	{
		[SerializeField]
		[Range(0.04f, 0.2f)]
		[Tooltip("Joystick radius as a fraction of the screen width. Drives both the finger travel and the drawn ring, so they always match on any resolution.")]
		private float _maxRadiusScreenRatio = 0.09f;

		[SerializeField]
		[Range(0.1f, 0.6f)]
		[Tooltip("Height of the bottom screen band (fraction of the screen height) where the joystick can appear. A touch above it still steers, but the joystick stays at the top of the band so the gameplay stays visible.")]
		private float _thumbZoneHeightRatio = 0.3f;

		/// <summary>Joystick radius in screen pixels.</summary>
		public float MaxRadius => Screen.width * _maxRadiusScreenRatio;

		public float ThumbZoneHeightRatio => _thumbZoneHeightRatio;
	}
}
