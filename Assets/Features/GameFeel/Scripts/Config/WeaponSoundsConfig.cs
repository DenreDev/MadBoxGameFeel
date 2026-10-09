using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// What a weapon sounds like: a whoosh when the swing starts, a thump when it hits, a heavier crunch when it kills or crits.
	// Plugged into the weapon (WeaponConfig), so a heavy weapon can sound heavier than a light one.
	[CreateAssetMenu(fileName = "WeaponSounds", menuName = "Feedback/Weapon Sounds")]
	public class WeaponSoundsConfig : ScriptableObject
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable whoosh played when the swing starts, its loudest moment lands just before the slash (address Audio/SwordWhoosh, group Audio)")]
		private AssetReferenceT<AudioClip> swingClip;
		[SerializeField]
		[Tooltip("Addressable thump played when the weapon hits an enemy (address Audio/HitImpact, group Audio)")]
		private AssetReferenceT<AudioClip> hitClip;
		[SerializeField]
		[Tooltip("Addressable heavier crunch played when the weapon kills an enemy or lands a critical strike (address Audio/KillImpact, group Audio)")]
		private AssetReferenceT<AudioClip> killClip;

		[Header("Mix")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the swing whoosh")]
		private float swingVolume = 0.5f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the hit thump")]
		private float hitVolume = 0.8f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the kill crunch")]
		private float killVolume = 1f;
		[SerializeField]
		[Tooltip("Pitch gained by the kill sound for each kill chained in the streak (streak window on the weapon's HitStopConfig), so a crowd going down sounds higher and higher")]
		private float killStreakPitchStep = 0.06f;
		[SerializeField]
		[Tooltip("Highest pitch of the kill sound, whatever the streak")]
		private float maxKillPitch = 1.5f;

		public AssetReferenceT<AudioClip> SwingClip => swingClip;
		public AssetReferenceT<AudioClip> HitClip => hitClip;
		public AssetReferenceT<AudioClip> KillClip => killClip;
		public float SwingVolume => swingVolume;
		public float HitVolume => hitVolume;
		public float KillVolume => killVolume;
		public float KillStreakPitchStep => killStreakPitchStep;
		public float MaxKillPitch => maxKillPitch;

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, swingClip, hitClip, killClip);
	}
}
