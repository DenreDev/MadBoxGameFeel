using Core.ScriptableObjectSingleton;
using Game.GameFeel;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GamePlay.Heroes
{
	[CreateAssetMenu(fileName = "HeroConfig", menuName = "Game/HeroConfig")]
	public class HeroConfig : ScriptableObjectSingleton<HeroConfig>
	{
		[SerializeField]
		[Tooltip("The hero prefab to instantiate")]
		private HeroView heroPrefab;

		[SerializeField]
		[Tooltip("Movement speed in units per second")]
		private float moveSpeed = 5f;

		[SerializeField]
		[Tooltip("Initial health of the hero")]
		private int initialHealth = 100;

		[Header("Feedback (each one is optional: leave it empty and the hero simply has no such feedback)")]
		[SerializeField]
		[Tooltip("Flash, squash and spark when the hero is hit")]
		private HitFeedbackConfig hitFeedback;
		[SerializeField]
		[Tooltip("Dust puffs at the hero's feet while he walks")]
		private FootstepFeedbackConfig footsteps;
		[SerializeField]
		[Tooltip("Bell, tier sparkle and break sound of the hero's kill combo")]
		private ComboSoundsConfig comboSounds;
		[SerializeField]
		[Tooltip("Addressable ground ring under the hero, so his position reads at a glance (address VFX/HeroRing, group VFX). Its size is the prefab's scale, its color and alpha are the material's Base Color")]
		private AssetReferenceGameObject groundRingPrefab;

		public HeroView HeroPrefab => heroPrefab;
		public float MoveSpeed => moveSpeed;
		public int InitialHealth => initialHealth;
		public HitFeedbackConfig HitFeedback => hitFeedback;
		public FootstepFeedbackConfig Footsteps => footsteps;
		public ComboSoundsConfig ComboSounds => comboSounds;
		public AssetReferenceGameObject GroundRingPrefab => groundRingPrefab;
	}
}
