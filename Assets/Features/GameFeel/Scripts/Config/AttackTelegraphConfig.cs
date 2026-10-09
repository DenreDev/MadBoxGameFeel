using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// How an attacker warns the player during its windup: the body blinks red and a red disc on the ground grows to the attack range.
	// Plugged into the content that attacks the hero (EnemyConfig). The windup duration and the range are the enemy's gameplay values.
	[CreateAssetMenu(fileName = "AttackTelegraph", menuName = "Feedback/Attack Telegraph")]
	public class AttackTelegraphConfig : ScriptableObject
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable red ground disc that fills in under the attacker (address VFX/AttackWarning, group VFX). Its size is set from the attack range, its color and alpha are the material's Base Color")]
		private AssetReferenceGameObject warningPrefab;
		[SerializeField]
		[Tooltip("Addressable unlit material the body blinks to during the windup (address VFX/AttackWarnTint, group VFX)")]
		private AssetReferenceT<Material> tintMaterial;

		[Header("Animation")]
		[SerializeField]
		[Tooltip("Red blinks per second of the body during the windup")]
		private float blinkRate = 6f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Size of the ground disc when the windup starts, relative to the full attack range (it grows to 1 over the windup)")]
		private float startScale = 0.3f;

		public AssetReferenceGameObject WarningPrefab => warningPrefab;
		public AssetReferenceT<Material> TintMaterial => tintMaterial;
		public float BlinkRate => blinkRate;
		public float StartScale => startScale;

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, warningPrefab, tintMaterial);
	}
}
