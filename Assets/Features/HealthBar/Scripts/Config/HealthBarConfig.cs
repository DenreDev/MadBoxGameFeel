using Core.ScriptableObjectSingleton;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.HealthBar
{
	[CreateAssetMenu(fileName = "HealthBarConfig", menuName = "Game/HealthBarConfig")]
	public class HealthBarConfig : ScriptableObjectSingleton<HealthBarConfig>
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable health bar prefab (address UI/HealthBar, group UI)")]
		private AssetReferenceGameObject prefab;

		[Header("Size and visibility")]
		[SerializeField]
		[Tooltip("Scale of the hero bar, bigger than the enemy bars so the player's health reads first")]
		private float heroScale = 1.5f;
		[SerializeField] private float enemyScale = 1f;
		[SerializeField]
		[Tooltip("Enemy bars only appear once the enemy has taken damage. The hero bar is always visible")]
		private bool hideEnemyBarWhenFull = true;

		[Header("Colors")]
		[SerializeField] private Color heroColor = new Color(0.35f, 0.85f, 0.25f);
		[SerializeField] private Color enemyColor = new Color(0.95f, 0.25f, 0.25f);
		[SerializeField]
		[Tooltip("Lagging bar that shows the health that was just lost")]
		private Color trailColor = new Color(1f, 0.85f, 0.3f);
		[SerializeField]
		[Tooltip("Color the bar flashes to when a hit lands")]
		private Color flashColor = Color.white;

		[Header("Hit feedback")]
		[SerializeField]
		[Tooltip("Seconds for the fill to slide down to the new health value")]
		private float fillSmoothTime = 0.12f;
		[SerializeField]
		[Tooltip("Seconds the lost-health trail waits before it starts draining")]
		private float trailDelay = 0.4f;
		[SerializeField]
		[Tooltip("How fast the trail drains, in fraction of a full bar per second")]
		private float trailDrainSpeed = 1.5f;
		[SerializeField]
		[Tooltip("Seconds of the white flash on the fill")]
		private float flashDuration = 0.12f;
		[SerializeField]
		[Tooltip("Extra scale of the bar at the instant of a hit (0.3 = 30% bigger)")]
		private float punchScale = 0.3f;
		[SerializeField]
		[Tooltip("Seconds for the bar to settle back to its normal size")]
		private float punchDuration = 0.25f;

		public AssetReferenceGameObject Prefab => prefab;
		public float HeroScale => heroScale;
		public float EnemyScale => enemyScale;
		public bool HideEnemyBarWhenFull => hideEnemyBarWhenFull;
		public Color HeroColor => heroColor;
		public Color EnemyColor => enemyColor;
		public Color TrailColor => trailColor;
		public Color FlashColor => flashColor;
		public float FillSmoothTime => fillSmoothTime;
		public float TrailDelay => trailDelay;
		public float TrailDrainSpeed => trailDrainSpeed;
		public float FlashDuration => flashDuration;
		public float PunchScale => punchScale;
		public float PunchDuration => punchDuration;
	}
}
