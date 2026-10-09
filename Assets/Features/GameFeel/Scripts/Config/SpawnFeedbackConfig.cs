using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// How a body appears: it pops in from nothing with a bit of overshoot while a dust puff bursts at its feet.
	// Plugged into the content that spawns (EnemyConfig).
	[CreateAssetMenu(fileName = "SpawnFeedback", menuName = "Feedback/Spawn")]
	public class SpawnFeedbackConfig : ScriptableObject
	{
		[SerializeField]
		[Tooltip("Addressable dust burst played where the body appears (address VFX/SpawnPuff, group VFX)")]
		private AssetReferenceGameObject puffPrefab;
		[SerializeField]
		[Tooltip("Seconds the body takes to pop in from nothing to full size (0 disables the pop and the puff)")]
		private float duration = 0.4f;
		[SerializeField]
		[Tooltip("How far the pop goes past full size before settling (0 = none, 1.7 = +10%, 2.5 = +19%, 3 = +25%)")]
		private float overshoot = 2.5f;

		public AssetReferenceGameObject PuffPrefab => puffPrefab;
		public float Duration => duration;
		public float Overshoot => overshoot;

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, puffPrefab);
	}
}
