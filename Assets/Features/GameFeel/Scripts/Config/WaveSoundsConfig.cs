using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// Sound of the wave announcement: a stinger played as the wave banner slams in.
	// Plugged into the content that defines the waves (EnemiesConfig).
	[CreateAssetMenu(fileName = "WaveSounds", menuName = "Feedback/Wave Sounds")]
	public class WaveSoundsConfig : ScriptableObject
	{
		[SerializeField]
		[Tooltip("Addressable stinger played when a new wave is announced (address Audio/WaveStart, group Audio)")]
		private AssetReferenceT<AudioClip> startClip;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the wave stinger")]
		private float startVolume = 0.7f;

		public AssetReferenceT<AudioClip> StartClip => startClip;
		public float StartVolume => startVolume;

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, startClip);
	}
}
