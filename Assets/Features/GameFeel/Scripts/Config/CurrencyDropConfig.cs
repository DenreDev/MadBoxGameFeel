using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// Currency orbs that burst out of a dead body, bounce, rest, then fly to the hero and are collected with a sparkle and a rising blip.
	// Plugged into the content that drops them (EnemyConfig), so each enemy type can drop more, fewer or none. Pure presentation: no amount, no rule.
	[CreateAssetMenu(fileName = "CurrencyDrop", menuName = "Feedback/Currency Drop")]
	public class CurrencyDropConfig : ScriptableObject
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable currency orb, a prefab with a CurrencyOrbView (address VFX/CurrencyOrb, group VFX). Its look is the prefab's: billboard, glow, shadow and trail")]
		private AssetReferenceGameObject orbPrefab;
		[SerializeField]
		[Tooltip("Addressable spark burst played on the hero when an orb reaches him (address VFX/CurrencyCollect, group VFX)")]
		private AssetReferenceGameObject collectPrefab;
		[SerializeField]
		[Tooltip("Addressable pop played when the orbs burst out (address Audio/CurrencyDrop, group Audio)")]
		private AssetReferenceT<AudioClip> dropClip;
		[SerializeField]
		[Tooltip("Addressable blip played when an orb reaches the hero, higher with each orb collected in a row (address Audio/CurrencyCollect, group Audio)")]
		private AssetReferenceT<AudioClip> collectClip;

		[Header("Drop")]
		[SerializeField]
		[Tooltip("Orbs dropped by one kill (x = fewest, y = most)")]
		private Vector2Int orbsPerKill = new Vector2Int(3, 5);
		[SerializeField]
		[Tooltip("Height above the body's feet where the orbs pop out, in world units")]
		private float dropHeight = 0.6f;
		[SerializeField]
		[Tooltip("Sideways speed of the burst (x = slowest, y = fastest), in units per second. Orbs are spread evenly around the body")]
		private Vector2 burstSpeed = new Vector2(2.5f, 4.5f);
		[SerializeField]
		[Tooltip("Upward speed of the burst, in units per second. With the gravity it sets how high the orbs hop")]
		private float burstUpSpeed = 6f;
		[SerializeField]
		[Tooltip("Gravity pulling the orbs down, in units per second squared")]
		private float gravity = 25f;
		[SerializeField, Range(0f, 0.9f)]
		[Tooltip("Share of its speed an orb keeps when it bounces on the floor (0 = lands dead, 0.5 = a couple of small hops)")]
		private float bounciness = 0.45f;
		[SerializeField]
		[Tooltip("Height of the orb's center above the floor when it rests, in world units")]
		private float restHeight = 0.2f;

		[Header("Attract")]
		[SerializeField]
		[Tooltip("Seconds an orb rests on the floor before it is called to the hero")]
		private float attractDelay = 0.8f;
		[SerializeField]
		[Tooltip("Random extra rest, in seconds, so the orbs of one kill leave one after the other instead of as a block")]
		private float attractDelayJitter = 0.35f;
		[SerializeField]
		[Tooltip("Seconds of the wind-up: the orb squashes and recoils away from the hero before it leaves")]
		private float windupDuration = 0.18f;
		[SerializeField]
		[Tooltip("How far the orb recoils during the wind-up, in world units")]
		private float windupDistance = 0.4f;
		[SerializeField]
		[Tooltip("How fast the orb gains speed toward the hero, in units per second squared")]
		private float attractAcceleration = 45f;
		[SerializeField]
		[Tooltip("Top speed of an orb flying to the hero, in units per second. Keep it above the hero's move speed")]
		private float maxSpeed = 20f;
		[SerializeField]
		[Tooltip("Sideways kick the orb leaves with, in units per second: 0 = a straight line, more = a wider curve into the hero")]
		private float attractCurve = 3f;
		[SerializeField]
		[Tooltip("Height above the hero's feet where the orbs fly to and are collected, in world units")]
		private float targetHeight = 1f;
		[SerializeField]
		[Tooltip("Distance to the hero at which an orb counts as collected, in world units")]
		private float collectRadius = 0.5f;

		[Header("Sound")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the pop when the orbs burst out")]
		private float dropVolume = 0.5f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Volume of the collect blip")]
		private float collectVolume = 0.6f;
		[SerializeField]
		[Tooltip("Seconds after a collect during which the next one counts as part of the chain")]
		private float chainWindow = 0.4f;
		[SerializeField]
		[Tooltip("Pitch gained by the collect blip for each orb collected in the chain")]
		private float chainPitchStep = 0.05f;
		[SerializeField]
		[Tooltip("Highest pitch of the collect blip, whatever the chain")]
		private float maxPitch = 1.6f;

		public AssetReferenceGameObject OrbPrefab => orbPrefab;
		public AssetReferenceGameObject CollectPrefab => collectPrefab;
		public AssetReferenceT<AudioClip> DropClip => dropClip;
		public AssetReferenceT<AudioClip> CollectClip => collectClip;
		public Vector2Int OrbsPerKill => orbsPerKill;
		public float DropHeight => dropHeight;
		public Vector2 BurstSpeed => burstSpeed;
		public float BurstUpSpeed => burstUpSpeed;
		public float Gravity => gravity;
		public float Bounciness => bounciness;
		public float RestHeight => restHeight;
		public float AttractDelay => attractDelay;
		public float AttractDelayJitter => attractDelayJitter;
		public float WindupDuration => windupDuration;
		public float WindupDistance => windupDistance;
		public float AttractAcceleration => attractAcceleration;
		public float MaxSpeed => maxSpeed;
		public float AttractCurve => attractCurve;
		public float TargetHeight => targetHeight;
		public float CollectRadius => collectRadius;
		public float DropVolume => dropVolume;
		public float CollectVolume => collectVolume;
		public float ChainWindow => chainWindow;
		public float ChainPitchStep => chainPitchStep;
		public float MaxPitch => maxPitch;

		public async UniTask PreloadAssets(FeedbackAssetsService assets)
		{
			await assets.Preload(this, orbPrefab, collectPrefab, dropClip, collectClip);

			if (assets.Get(orbPrefab) != null && GetOrbPrefab(assets) == null)
			{
				Debug.LogWarning($"{name}: the orb prefab has no CurrencyOrbView component. Currency orbs are disabled.", this);
			}
		}

		// The orb template, or null when it is missing: the kill just drops no orbs.
		public CurrencyOrbView GetOrbPrefab(FeedbackAssetsService assets)
		{
			GameObject loaded = assets.Get(orbPrefab);
			return loaded != null && loaded.TryGetComponent(out CurrencyOrbView orb) ? orb : null;
		}
	}
}
