using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// Sounds of the kill combo: a bell on every kill that climbs a pentatonic scale, a sparkle on each new tier and a falling sound when it breaks.
	// Plugged into the hero (HeroConfig): the combo is his streak of kills. The combo rules and visuals live on the ComboCounterView.
	[CreateAssetMenu(fileName = "ComboSounds", menuName = "Feedback/Combo Sounds")]
	public class ComboSoundsConfig : ScriptableObject
	{
		private static readonly int[] PentatonicSemitones = { 0, 2, 4, 7, 9 }; // major pentatonic: any two notes of it sound good together

		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable bell played on every kill of a combo, it climbs the scale with the combo (address Audio/ComboHit, group Audio)")]
		private AssetReferenceT<AudioClip> hitClip;
		[SerializeField]
		[Tooltip("Addressable sparkle arpeggio played when the combo reaches a new tier (address Audio/ComboMilestone, group Audio)")]
		private AssetReferenceT<AudioClip> milestoneClip;
		[SerializeField]
		[Tooltip("Addressable falling sound played when the combo breaks (address Audio/ComboBreak, group Audio)")]
		private AssetReferenceT<AudioClip> breakClip;

		[Header("Mix")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the combo bell")]
		private float hitVolume = 0.5f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the tier sparkle")]
		private float milestoneVolume = 0.8f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the break sound")]
		private float breakVolume = 0.7f;
		[SerializeField, Min(0)]
		[Tooltip("Highest the bell climbs above its normal pitch, in semitones (12 = one octave). It climbs a pentatonic scale, so it always sounds in tune")]
		private int maxSemitones = 14;

		public AssetReferenceT<AudioClip> HitClip => hitClip;
		public AssetReferenceT<AudioClip> MilestoneClip => milestoneClip;
		public AssetReferenceT<AudioClip> BreakClip => breakClip;
		public float HitVolume => hitVolume;
		public float MilestoneVolume => milestoneVolume;
		public float BreakVolume => breakVolume;

		// Pitch of the bell for a combo step: step 0 is the first kill that shows the combo, each next kill climbs one note of the scale (up to maxSemitones).
		public float GetHitPitch(int step)
		{
			step = Mathf.Max(0, step);
			int semitones = Mathf.Min(maxSemitones, 12 * (step / PentatonicSemitones.Length) + PentatonicSemitones[step % PentatonicSemitones.Length]);
			return Mathf.Pow(2f, semitones / 12f);
		}

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, hitClip, milestoneClip, breakClip);
	}
}
