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
		[SerializeField]
		[Tooltip("Addressable unlit material of the hero's attack range drawn on the ground (address VFX/AttackRange, group VFX). Its Base Color is the tint, the strengths below are vertex alphas")]
		private AssetReferenceT<Material> attackRangeMaterial;

		[SerializeField]
		[Tooltip("Addressable whoosh played when the hero starts a swing, its loudest moment lands just before the slash (address Audio/SwordWhoosh, group Audio)")]
		private AssetReferenceT<AudioClip> swordWhooshClip;
		[SerializeField]
		[Tooltip("Addressable thump played when the hero's sword hits an enemy (address Audio/HitImpact, group Audio)")]
		private AssetReferenceT<AudioClip> hitImpactClip;
		[SerializeField]
		[Tooltip("Addressable heavier crunch played when the hero's sword kills an enemy (address Audio/KillImpact, group Audio)")]
		private AssetReferenceT<AudioClip> killImpactClip;

		[Header("Currency orbs content")]
		[SerializeField]
		[Tooltip("Addressable currency orb dropped by a dead enemy, a prefab with a CurrencyOrbView (address VFX/CurrencyOrb, group VFX). Its look is the prefab's: billboard, glow, shadow and trail")]
		private AssetReferenceGameObject currencyOrbPrefab;
		[SerializeField]
		[Tooltip("Addressable spark burst played on the hero when an orb reaches him (address VFX/CurrencyCollect, group VFX)")]
		private AssetReferenceGameObject currencyCollectPrefab;
		[SerializeField]
		[Tooltip("Addressable pop played when the orbs burst out of a dead enemy (address Audio/CurrencyDrop, group Audio)")]
		private AssetReferenceT<AudioClip> currencyDropClip;
		[SerializeField]
		[Tooltip("Addressable blip played when an orb reaches the hero, higher with each orb collected in a row (address Audio/CurrencyCollect, group Audio)")]
		private AssetReferenceT<AudioClip> currencyCollectClip;

		[Header("Currency orbs: drop")]
		[SerializeField]
		[Tooltip("Orbs dropped by one kill (x = fewest, y = most)")]
		private Vector2Int currencyOrbsPerKill = new Vector2Int(3, 5);
		[SerializeField]
		[Tooltip("Height above the enemy's feet where the orbs pop out, in world units")]
		private float currencyDropHeight = 0.6f;
		[SerializeField]
		[Tooltip("Sideways speed of the burst (x = slowest, y = fastest), in units per second. Orbs are spread evenly around the enemy")]
		private Vector2 currencyBurstSpeed = new Vector2(2.5f, 4.5f);
		[SerializeField]
		[Tooltip("Upward speed of the burst, in units per second. With the gravity it sets how high the orbs hop")]
		private float currencyBurstUpSpeed = 6f;
		[SerializeField]
		[Tooltip("Gravity pulling the orbs down, in units per second squared")]
		private float currencyGravity = 25f;
		[SerializeField, Range(0f, 0.9f)]
		[Tooltip("Share of its speed an orb keeps when it bounces on the floor (0 = lands dead, 0.5 = a couple of small hops)")]
		private float currencyBounciness = 0.45f;
		[SerializeField]
		[Tooltip("Height of the orb's center above the floor when it rests, in world units")]
		private float currencyRestHeight = 0.2f;

		[Header("Currency orbs: attract")]
		[SerializeField]
		[Tooltip("Seconds an orb rests on the floor before it is called to the hero")]
		private float currencyAttractDelay = 0.8f;
		[SerializeField]
		[Tooltip("Random extra rest, in seconds, so the orbs of one kill leave one after the other instead of as a block")]
		private float currencyAttractDelayJitter = 0.35f;
		[SerializeField]
		[Tooltip("Seconds of the wind-up: the orb squashes and recoils away from the hero before it leaves")]
		private float currencyWindupDuration = 0.18f;
		[SerializeField]
		[Tooltip("How far the orb recoils during the wind-up, in world units")]
		private float currencyWindupDistance = 0.4f;
		[SerializeField]
		[Tooltip("How fast the orb gains speed toward the hero, in units per second squared")]
		private float currencyAttractAcceleration = 45f;
		[SerializeField]
		[Tooltip("Top speed of an orb flying to the hero, in units per second. Keep it above the hero's move speed")]
		private float currencyMaxSpeed = 20f;
		[SerializeField]
		[Tooltip("Sideways kick the orb leaves with, in units per second: 0 = a straight line, more = a wider curve into the hero")]
		private float currencyAttractCurve = 3f;
		[SerializeField]
		[Tooltip("Height above the hero's feet where the orbs fly to and are collected, in world units")]
		private float currencyTargetHeight = 1f;
		[SerializeField]
		[Tooltip("Distance to the hero at which an orb counts as collected, in world units")]
		private float currencyCollectRadius = 0.5f;

		[Header("Currency orbs: sound")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the pop when the orbs burst out")]
		private float currencyDropVolume = 0.5f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the collect blip")]
		private float currencyCollectVolume = 0.6f;
		[SerializeField]
		[Tooltip("Seconds after a collect during which the next one counts as part of the chain")]
		private float currencyChainWindow = 0.4f;
		[SerializeField]
		[Tooltip("Pitch gained by the collect blip for each orb collected in the chain")]
		private float currencyChainPitchStep = 0.05f;
		[SerializeField]
		[Tooltip("Highest pitch of the collect blip, whatever the chain")]
		private float currencyMaxPitch = 1.6f;

		[Header("Attack sounds")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the swing whoosh")]
		private float swordWhooshVolume = 0.5f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the hit thump")]
		private float hitImpactVolume = 0.8f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the kill crunch")]
		private float killImpactVolume = 1f;
		[SerializeField]
		[Tooltip("Pitch gained by the kill sound for each kill chained in the streak window, so a crowd going down sounds higher and higher")]
		private float killStreakPitchStep = 0.06f;
		[SerializeField]
		[Tooltip("Highest pitch of the kill sound, whatever the streak")]
		private float maxKillPitch = 1.5f;

		[Header("Attack telegraph")]
		[SerializeField]
		[Tooltip("Red blinks per second of the enemy body during the windup")]
		private float attackWarnBlinkRate = 6f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Size of the ground disc when the windup starts, relative to the full attack range (it grows to 1 over the windup)")]
		private float attackWarnStartScale = 0.3f;

		[Header("Attack range (the cone the hero's sword covers, drawn on the ground)")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Strength of the cone's fill at its far end (0 = no fill). It fades to nothing toward the hero, so the area stays discreet")]
		private float attackRangeFillAlpha = 0.14f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Strength of the outer arc at the exact reach of the sword (0 = no arc)")]
		private float attackRangeEdgeAlpha = 0.45f;
		[SerializeField, Min(0.01f)]
		[Tooltip("Thickness of the outer arc, in world units")]
		private float attackRangeEdgeWidth = 0.1f;
		[SerializeField]
		[Tooltip("Seconds of the pulse played when the slash lands, hit or miss (0 disables)")]
		private float attackRangePulseDuration = 0.25f;
		[SerializeField, Min(1f)]
		[Tooltip("How many times stronger the cone is at the start of the pulse (1 = no change)")]
		private float attackRangePulseBoost = 3f;
		[SerializeField, Min(0f)]
		[Tooltip("How much bigger the cone is at the start of the pulse (0.06 = 6% bigger), it settles back with the pulse")]
		private float attackRangePulsePop = 0.06f;

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

		[Header("Enemy recoil (the model is shoved away from the hero when his sword lands)")]
		[SerializeField]
		[Tooltip("How far the enemy's model is shoved at the instant of a hit, in world units (0 disables)")]
		private float recoilDistance = 0.45f;
		[SerializeField]
		[Tooltip("Seconds for the model to slide back to its place")]
		private float recoilDuration = 0.22f;

		[Header("Hit stop (the game freezes for a few frames, only on big impacts: a kill chained with others)")]
		[SerializeField]
		[Tooltip("Kills chained inside the streak window needed before the kill freezes the game (1 = every kill, 2 = the second kill of a crowd, 0 or less would act like 1)")]
		private int hitStopMinKillStreak = 2;
		[SerializeField]
		[Tooltip("Freeze, in real seconds, on the first kill that qualifies (0 disables the freeze)")]
		private float killStopDuration = 0.1f;
		[SerializeField]
		[Tooltip("Extra freeze for each further kill chained in the streak window, so killing a crowd feels heavier and heavier")]
		private float killStreakStopBonus = 0.03f;
		[SerializeField]
		[Tooltip("Seconds after a kill during which the next kill counts as part of the streak")]
		private float killStreakWindow = 1.5f;
		[SerializeField]
		[Tooltip("The longest a single freeze can last, in real seconds, whatever the streak")]
		private float maxHitStopDuration = 0.22f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Game speed during the freeze (0 = fully stopped, 0.1 = crawling, which keeps a little motion)")]
		private float hitStopTimeScale = 0.02f;

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

		[Header("Critical strike (chance and damage multiplier are on the WeaponConfig)")]
		[SerializeField]
		[Tooltip("Size of the hit spark on a critical strike, relative to a regular one")]
		private float critSparkScale = 1.8f;
		[SerializeField]
		[Tooltip("How much stronger the body squash and the shove are on a critical strike (1.6 = 60% more)")]
		private float critImpactScale = 1.6f;
		[SerializeField]
		[Tooltip("Freeze, in real seconds, on every critical strike, even if it does not kill (0 disables). A longer kill streak freeze wins")]
		private float critStopDuration = 0.08f;
		[SerializeField]
		[Tooltip("Camera kick multiplier on a critical strike (2 = twice the regular hit or kill shake)")]
		private float critShakeMultiplier = 2f;
		[SerializeField]
		[Tooltip("Size of a critical number, relative to a regular one (and to the kill size when the crit kills)")]
		private float critNumberScale = 1.4f;
		[SerializeField]
		[Tooltip("Color at the top of a critical number")]
		private Color critNumberTopColor = new Color(1f, 0.55f, 0.95f);
		[SerializeField]
		[Tooltip("Color at the bottom of a critical number")]
		private Color critNumberBottomColor = new Color(0.55f, 0.1f, 0.85f);

		[Header("Camera shake (impulse strength in world units, 0 disables)")]
		[SerializeField]
		[Tooltip("The hero's slash reaches its high point, hit or miss")]
		private float attackShake = 0.02f;
		[SerializeField]
		[Tooltip("The hero's sword hits an enemy that survives. The kick goes along the slash. A swing hitting several enemies kicks once")]
		private float enemyHitShake = 0.1f;
		[SerializeField]
		[Tooltip("The hero's sword kills an enemy. The kick goes along the slash. A swing hitting several enemies kicks once")]
		private float enemyKillShake = 0.2f;
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
		public AssetReferenceT<AudioClip> SwordWhooshClip => swordWhooshClip;
		public AssetReferenceT<AudioClip> HitImpactClip => hitImpactClip;
		public AssetReferenceT<AudioClip> KillImpactClip => killImpactClip;
		public float SwordWhooshVolume => swordWhooshVolume;
		public float HitImpactVolume => hitImpactVolume;
		public float KillImpactVolume => killImpactVolume;
		public float KillStreakPitchStep => killStreakPitchStep;
		public float MaxKillPitch => maxKillPitch;
		public AssetReferenceGameObject CurrencyOrbPrefab => currencyOrbPrefab;
		public AssetReferenceGameObject CurrencyCollectPrefab => currencyCollectPrefab;
		public AssetReferenceT<AudioClip> CurrencyDropClip => currencyDropClip;
		public AssetReferenceT<AudioClip> CurrencyCollectClip => currencyCollectClip;
		public Vector2Int CurrencyOrbsPerKill => currencyOrbsPerKill;
		public float CurrencyDropHeight => currencyDropHeight;
		public Vector2 CurrencyBurstSpeed => currencyBurstSpeed;
		public float CurrencyBurstUpSpeed => currencyBurstUpSpeed;
		public float CurrencyGravity => currencyGravity;
		public float CurrencyBounciness => currencyBounciness;
		public float CurrencyRestHeight => currencyRestHeight;
		public float CurrencyAttractDelay => currencyAttractDelay;
		public float CurrencyAttractDelayJitter => currencyAttractDelayJitter;
		public float CurrencyWindupDuration => currencyWindupDuration;
		public float CurrencyWindupDistance => currencyWindupDistance;
		public float CurrencyAttractAcceleration => currencyAttractAcceleration;
		public float CurrencyMaxSpeed => currencyMaxSpeed;
		public float CurrencyAttractCurve => currencyAttractCurve;
		public float CurrencyTargetHeight => currencyTargetHeight;
		public float CurrencyCollectRadius => currencyCollectRadius;
		public float CurrencyDropVolume => currencyDropVolume;
		public float CurrencyCollectVolume => currencyCollectVolume;
		public float CurrencyChainWindow => currencyChainWindow;
		public float CurrencyChainPitchStep => currencyChainPitchStep;
		public float CurrencyMaxPitch => currencyMaxPitch;
		public AssetReferenceGameObject AttackWarningPrefab => attackWarningPrefab;
		public AssetReferenceT<Material> AttackWarnMaterial => attackWarnMaterial;
		public AssetReferenceT<Material> AttackRangeMaterial => attackRangeMaterial;
		public float AttackRangeFillAlpha => attackRangeFillAlpha;
		public float AttackRangeEdgeAlpha => attackRangeEdgeAlpha;
		public float AttackRangeEdgeWidth => attackRangeEdgeWidth;
		public float AttackRangePulseDuration => attackRangePulseDuration;
		public float AttackRangePulseBoost => attackRangePulseBoost;
		public float AttackRangePulsePop => attackRangePulsePop;
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
		public float CritSparkScale => critSparkScale;
		public float CritImpactScale => critImpactScale;
		public float CritStopDuration => critStopDuration;
		public float CritShakeMultiplier => critShakeMultiplier;
		public float CritNumberScale => critNumberScale;
		public Color CritNumberTopColor => critNumberTopColor;
		public Color CritNumberBottomColor => critNumberBottomColor;
		public float SpawnDuration => spawnDuration;
		public float SpawnOvershoot => spawnOvershoot;
		public float FlashDuration => flashDuration;
		public float PunchSquash => punchSquash;
		public float PunchDuration => punchDuration;
		public float RecoilDistance => recoilDistance;
		public float RecoilDuration => recoilDuration;
		public int HitStopMinKillStreak => hitStopMinKillStreak;
		public float KillStopDuration => killStopDuration;
		public float KillStreakStopBonus => killStreakStopBonus;
		public float KillStreakWindow => killStreakWindow;
		public float MaxHitStopDuration => maxHitStopDuration;
		public float HitStopTimeScale => hitStopTimeScale;
		public float AttackShake => attackShake;
		public float EnemyHitShake => enemyHitShake;
		public float EnemyKillShake => enemyKillShake;
		public float HitShake => hitShake;
		public float DeathShake => deathShake;
		public float HitShakeCooldown => hitShakeCooldown;
		public float StartCameraDistance => startCameraDistance;
		public float StartCameraPitch => startCameraPitch;
		public float CameraPitchSmoothTime => cameraPitchSmoothTime;
	}
}
