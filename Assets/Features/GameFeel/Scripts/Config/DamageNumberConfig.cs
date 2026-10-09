using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// Floating damage number over a body that takes a hit, with its own look for a regular hit, the killing blow and a critical strike.
	// Plugged into the content that shows numbers (EnemyConfig). The motion, fades and scale by damage are tuned on the third-party package prefab itself.
	[CreateAssetMenu(fileName = "DamageNumbers", menuName = "Feedback/Damage Numbers")]
	public class DamageNumberConfig : ScriptableObject
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable floating damage number, a third-party package prefab (address VFX/DamageNumber, group VFX)")]
		private AssetReferenceGameObject prefab;
		[SerializeField]
		[Tooltip("How far above the hit point (body center) the number appears, in world units")]
		private float height = 0.5f;

		[Header("Regular hit")]
		[SerializeField]
		[Tooltip("Size of a regular hit number (heavier weapons print bigger on top of this, see Scale By Number on the prefab)")]
		private float hitScale = 1.8f;
		[SerializeField]
		[Tooltip("Color at the top of a regular hit number")]
		private Color hitTopColor = new Color(1f, 0.9f, 0.25f);
		[SerializeField]
		[Tooltip("Color at the bottom of a regular hit number")]
		private Color hitBottomColor = new Color(1f, 0.45f, 0.05f);

		[Header("Killing blow")]
		[SerializeField]
		[Tooltip("Size of the number of the hit that kills, relative to a regular one (1.5 = 50% bigger)")]
		private float killScale = 1.5f;
		[SerializeField]
		[Tooltip("Color at the top of the number of the hit that kills")]
		private Color killTopColor = new Color(1f, 0.25f, 0f);
		[SerializeField]
		[Tooltip("Color at the bottom of the number of the hit that kills")]
		private Color killBottomColor = new Color(0.75f, 0f, 0.05f);

		[Header("Critical strike")]
		[SerializeField]
		[Tooltip("Size of a critical number, relative to a regular one (and to the kill size when the crit kills)")]
		private float critScale = 1.4f;
		[SerializeField]
		[Tooltip("Color at the top of a critical number")]
		private Color critTopColor = new Color(1f, 0.55f, 0.95f);
		[SerializeField]
		[Tooltip("Color at the bottom of a critical number")]
		private Color critBottomColor = new Color(0.55f, 0.1f, 0.85f);

		public float Height => height;
		public float HitScale => hitScale;
		public Color HitTopColor => hitTopColor;
		public Color HitBottomColor => hitBottomColor;
		public float KillScale => killScale;
		public Color KillTopColor => killTopColor;
		public Color KillBottomColor => killBottomColor;
		public float CritScale => critScale;
		public Color CritTopColor => critTopColor;
		public Color CritBottomColor => critBottomColor;

		// The prefab is only the template: third-party package instantiates and pools the numbers from it.
		// Prewarming here keeps the first hit of the game from paying for the number creation (it is a no-op once the pool is full).
		public async UniTask PreloadAssets(FeedbackAssetsService assets)
		{
			await assets.Preload(this, prefab);

			DamageNumber number = GetPrefab(assets);
			if (number != null) number.PrewarmPool();
			else if (assets.Get(prefab) != null) Debug.LogWarning($"{name}: the damage number prefab has no DamageNumber component. Damage numbers are disabled.", this);
		}

		// The third-party package template, or null when it is missing: the hit just shows no number.
		public DamageNumber GetPrefab(FeedbackAssetsService assets)
		{
			GameObject loaded = assets.Get(prefab);
			return loaded != null && loaded.TryGetComponent(out DamageNumber number) ? number : null;
		}
	}
}
