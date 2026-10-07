using Core.ScriptableObjectSingleton;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	[CreateAssetMenu(fileName = "GameFeelConfig", menuName = "Game/GameFeelConfig")]
	public class GameFeelConfig : ScriptableObjectSingleton<GameFeelConfig>
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable spark burst played on a hit (address VFX/HitSpark, group VFX)")]
		private AssetReferenceGameObject hitSparkPrefab;
		[SerializeField]
		[Tooltip("Addressable unlit material the body flashes to on a hit (address VFX/HitFlash, group VFX)")]
		private AssetReferenceT<Material> hitFlashMaterial;
		[SerializeField]
		[Tooltip("Addressable dust burst played where an enemy appears (address VFX/SpawnPuff, group VFX)")]
		private AssetReferenceGameObject spawnPuffPrefab;
		[SerializeField]
		[Tooltip("Addressable floating damage number, a third-party package prefab (address VFX/DamageNumber, group VFX). Its motion, fades, lifetime and scale by damage are tuned on the prefab itself")]
		private AssetReferenceGameObject damageNumberPrefab;
		[SerializeField]
		[Tooltip("Addressable ground ring under the hero (address VFX/HeroRing, group VFX). Its size is the prefab's scale, its color and alpha are the material's Base Color")]
		private AssetReferenceGameObject heroRingPrefab;

		[SerializeField]
		[Tooltip("Addressable small dust puff played under the hero's feet while he moves (address VFX/StepPuff, group VFX)")]
		private AssetReferenceGameObject stepPuffPrefab;
		[SerializeField]
		[Tooltip("Addressable red ground disc that fills in under an enemy winding up an attack (address VFX/AttackWarning, group VFX). Its size is set from the enemy's attack range, its color and alpha are the material's Base Color")]
		private AssetReferenceGameObject attackWarningPrefab;
		[SerializeField]
		[Tooltip("Addressable unlit material the enemy body blinks to while winding up an attack (address VFX/AttackWarnTint, group VFX)")]
		private AssetReferenceT<Material> attackWarnMaterial;

		[Header("Attack telegraph")]
		[SerializeField]
		[Tooltip("Red blinks per second of the enemy body during the windup")]
		private float attackWarnBlinkRate = 6f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Size of the ground disc when the windup starts, relative to the full attack range (it grows to 1 over the windup)")]
		private float attackWarnStartScale = 0.3f;

		[Header("Footsteps")]
		[SerializeField]
		[Tooltip("Ground covered between two step puffs, in world units (0 disables). Hero speed is 5 units/s, so 1.2 is about 4 puffs per second")]
		private float stepDistance = 1.2f;

		[Header("Spawn feedback")]
		[SerializeField]
		[Tooltip("Seconds the enemy takes to pop in from nothing to full size")]
		private float spawnDuration = 0.4f;
		[SerializeField]
		[Tooltip("How far the pop goes past full size before settling (0 = none, 1.7 = +10%, 2.5 = +19%, 3 = +25%)")]
		private float spawnOvershoot = 2.5f;

		[Header("Hit feedback")]
		[SerializeField]
		[Tooltip("Seconds the body stays flashed")]
		private float flashDuration = 0.08f;
		[SerializeField]
		[Tooltip("How much the body is squashed at the instant of a hit (0.15 = 15% flatter and wider)")]
		private float punchSquash = 0.15f;
		[SerializeField]
		[Tooltip("Seconds for the body to settle back to its normal shape")]
		private float punchDuration = 0.18f;

		[Header("Damage numbers")]
		[SerializeField]
		[Tooltip("How far above the hit point (body center) the number appears, in world units")]
		private float damageNumberHeight = 0.5f;
		[SerializeField]
		[Tooltip("Size of a regular hit number (heavier weapons print bigger on top of this, see Scale By Number on the prefab)")]
		private float hitNumberScale = 1.8f;
		[SerializeField]
		[Tooltip("Color at the top of a regular hit number")]
		private Color hitNumberTopColor = new Color(1f, 0.9f, 0.25f);
		[SerializeField]
		[Tooltip("Color at the bottom of a regular hit number")]
		private Color hitNumberBottomColor = new Color(1f, 0.45f, 0.05f);
		[SerializeField]
		[Tooltip("Color at the top of the number of the hit that kills")]
		private Color killNumberTopColor = new Color(1f, 0.25f, 0f);
		[SerializeField]
		[Tooltip("Color at the bottom of the number of the hit that kills")]
		private Color killNumberBottomColor = new Color(0.75f, 0f, 0.05f);
		[SerializeField]
		[Tooltip("Size of the number of the hit that kills, relative to a regular one (1.5 = 50% bigger)")]
		private float killNumberScale = 1.5f;

		[Header("Camera shake (impulse strength in world units, 0 disables)")]
		[SerializeField]
		[Tooltip("Hero swings his weapon")]
		private float attackShake = 0.04f;
		[SerializeField]
		[Tooltip("Hero takes a hit")]
		private float hitShake = 0.15f;
		[SerializeField]
		[Tooltip("Hero dies")]
		private float deathShake = 0.3f;
		[SerializeField]
		[Tooltip("Minimum seconds between two hit shakes, so a crowd hitting the hero does not shake the screen non-stop")]
		private float hitShakeCooldown = 0.5f;

		[Header("Start screen")]
		[SerializeField]
		[Tooltip("Camera distance while waiting for the first tap. The gameplay distance is the Framing Transposer's on the World prefab, and its Z Damping sets how fast the camera pulls back")]
		private float startCameraDistance = 5f;
		[SerializeField]
		[Tooltip("Camera pitch in degrees on the start screen. Low = the camera sits in front of the hero and sees his face (0 = eye level, 90 = straight down). The gameplay pitch is the World prefab's")]
		private float startCameraPitch = 12f;
		[SerializeField]
		[Tooltip("Seconds the camera takes to tilt between the start pitch and the gameplay pitch")]
		private float cameraPitchSmoothTime = 0.6f;

		public AssetReferenceGameObject HitSparkPrefab => hitSparkPrefab;
		public AssetReferenceT<Material> HitFlashMaterial => hitFlashMaterial;
		public AssetReferenceGameObject SpawnPuffPrefab => spawnPuffPrefab;
		public AssetReferenceGameObject StepPuffPrefab => stepPuffPrefab;
		public float StepDistance => stepDistance;
		public AssetReferenceGameObject AttackWarningPrefab => attackWarningPrefab;
		public AssetReferenceT<Material> AttackWarnMaterial => attackWarnMaterial;
		public float AttackWarnBlinkRate => attackWarnBlinkRate;
		public float AttackWarnStartScale => attackWarnStartScale;
		public AssetReferenceGameObject DamageNumberPrefab => damageNumberPrefab;
		public AssetReferenceGameObject HeroRingPrefab => heroRingPrefab;
		public float DamageNumberHeight => damageNumberHeight;
		public float HitNumberScale => hitNumberScale;
		public Color HitNumberTopColor => hitNumberTopColor;
		public Color HitNumberBottomColor => hitNumberBottomColor;
		public Color KillNumberTopColor => killNumberTopColor;
		public Color KillNumberBottomColor => killNumberBottomColor;
		public float KillNumberScale => killNumberScale;
		public float SpawnDuration => spawnDuration;
		public float SpawnOvershoot => spawnOvershoot;
		public float FlashDuration => flashDuration;
		public float PunchSquash => punchSquash;
		public float PunchDuration => punchDuration;
		public float AttackShake => attackShake;
		public float HitShake => hitShake;
		public float DeathShake => deathShake;
		public float HitShakeCooldown => hitShakeCooldown;
		public float StartCameraDistance => startCameraDistance;
		public float StartCameraPitch => startCameraPitch;
		public float CameraPitchSmoothTime => cameraPitchSmoothTime;
	}
}
