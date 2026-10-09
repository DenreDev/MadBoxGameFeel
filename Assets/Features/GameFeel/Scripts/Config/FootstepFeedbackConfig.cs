using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// Dust puffs at the feet of a walking body, one every stepDistance of ground covered.
	// Plugged into the content that walks (HeroConfig).
	[CreateAssetMenu(fileName = "Footsteps", menuName = "Feedback/Footsteps")]
	public class FootstepFeedbackConfig : ScriptableObject
	{
		[SerializeField]
		[Tooltip("Addressable small dust puff played under the feet while moving (address VFX/StepPuff, group VFX)")]
		private AssetReferenceGameObject puffPrefab;
		[SerializeField]
		[Tooltip("Ground covered between two step puffs, in world units (0 disables). At 5 units/s, 1.2 is about 4 puffs per second")]
		private float stepDistance = 1.2f;

		public AssetReferenceGameObject PuffPrefab => puffPrefab;
		public float StepDistance => stepDistance;

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, puffPrefab);
	}
}
