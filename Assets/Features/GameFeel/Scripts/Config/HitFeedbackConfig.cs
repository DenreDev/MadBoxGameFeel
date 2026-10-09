using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// How a body reacts when it takes a hit: white flash, squash, shove and a spark burst. A critical strike makes the spark and the impact bigger.
	// Plugged into the content that gets hit (HeroConfig, EnemyConfig), so each entity can react its own way.
	[CreateAssetMenu(fileName = "HitFeedback", menuName = "Feedback/Hit")]
	public class HitFeedbackConfig : ScriptableObject
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable spark burst played on a hit (address VFX/HitSpark, group VFX)")]
		private AssetReferenceGameObject sparkPrefab;
		[SerializeField]
		[Tooltip("Addressable unlit material the body flashes to on a hit (address VFX/HitFlash, group VFX)")]
		private AssetReferenceT<Material> flashMaterial;

		[Header("Body")]
		[SerializeField]
		[Tooltip("Seconds the body stays flashed")]
		private float flashDuration = 0.08f;
		[SerializeField]
		[Tooltip("How much the body is squashed at the instant of a hit (0.15 = 15% flatter and wider)")]
		private float punchSquash = 0.15f;
		[SerializeField]
		[Tooltip("Seconds for the body to settle back to its normal shape")]
		private float punchDuration = 0.18f;

		[Header("Recoil (the model is shoved away from the attacker)")]
		[SerializeField]
		[Tooltip("How far the model is shoved at the instant of a hit, in world units (0 disables)")]
		private float recoilDistance = 0.45f;
		[SerializeField]
		[Tooltip("Seconds for the model to slide back to its place")]
		private float recoilDuration = 0.22f;

		[Header("Critical strike (chance and damage multiplier are on the WeaponConfig)")]
		[SerializeField]
		[Tooltip("Size of the hit spark on a critical strike, relative to a regular one")]
		private float critSparkScale = 1.8f;
		[SerializeField]
		[Tooltip("How much stronger the body squash and the shove are on a critical strike (1.6 = 60% more)")]
		private float critImpactScale = 1.6f;

		public AssetReferenceGameObject SparkPrefab => sparkPrefab;
		public AssetReferenceT<Material> FlashMaterial => flashMaterial;
		public float FlashDuration => flashDuration;
		public float PunchSquash => punchSquash;
		public float PunchDuration => punchDuration;
		public float RecoilDistance => recoilDistance;
		public float RecoilDuration => recoilDuration;
		public float CritSparkScale => critSparkScale;
		public float CritImpactScale => critImpactScale;

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, sparkPrefab, flashMaterial);
	}
}
