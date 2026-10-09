using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GameFeel
{
	// Look of the cone a weapon covers, drawn on the ground in front of its wielder: a faint fill fading toward him and a thin arc at the exact reach.
	// During the swing windup a brighter fill grows to the reach and touches it when the damage lands; the slash landing then makes the cone pulse once. Plugged into the weapon (WeaponConfig): the cone's shape is the weapon's range and arc, its look is this.
	[CreateAssetMenu(fileName = "AttackRange", menuName = "Feedback/Attack Range")]
	public class AttackRangeConfig : ScriptableObject
	{
		[Header("Content")]
		[SerializeField]
		[Tooltip("Addressable unlit material of the cone (address VFX/AttackRange, group VFX). Its Base Color is the tint, the strengths below are vertex alphas")]
		private AssetReferenceT<Material> material;

		[Header("Shape")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Strength of the cone's fill at its far end (0 = no fill). It fades to nothing toward the wielder, so the area stays discreet")]
		private float fillAlpha = 0.14f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Strength of the outer arc at the exact reach of the weapon (0 = no arc)")]
		private float edgeAlpha = 0.45f;
		[SerializeField, Min(0.01f)]
		[Tooltip("Thickness of the outer arc, in world units")]
		private float edgeWidth = 0.1f;

		[Header("Pulse when the slash lands")]
		[SerializeField]
		[Tooltip("Seconds of the pulse played when the slash lands, hit or miss (0 disables)")]
		private float pulseDuration = 0.25f;
		[SerializeField, Min(1f)]
		[Tooltip("How many times stronger the cone is at the start of the pulse (1 = no change)")]
		private float pulseBoost = 3f;
		[SerializeField, Min(0f)]
		[Tooltip("How much bigger the cone is at the start of the pulse (0.06 = 6% bigger), it settles back with the pulse")]
		private float pulsePop = 0.06f;

		[Header("Charge during the windup")]
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Strength of the fill that grows from the wielder to the reach during the swing windup: it touches the arc exactly when the damage lands, then fades with the pulse (0 = no fill)")]
		private float chargeAlpha = 0.22f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Strength of the bright front at the leading edge of the growing fill")]
		private float chargeEdgeAlpha = 0.75f;
		[SerializeField]
		[Tooltip("How the fill grows over the windup (time 0..1 to size 0..1). Ease-in makes it rush into the hit")]
		private AnimationCurve chargeCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2f, 2f));

		public AssetReferenceT<Material> Material => material;
		public float FillAlpha => fillAlpha;
		public float EdgeAlpha => edgeAlpha;
		public float EdgeWidth => edgeWidth;
		public float PulseDuration => pulseDuration;
		public float PulseBoost => pulseBoost;
		public float PulsePop => pulsePop;
		public float ChargeAlpha => chargeAlpha;
		public float ChargeEdgeAlpha => chargeEdgeAlpha;
		public AnimationCurve ChargeCurve => chargeCurve;

		public UniTask PreloadAssets(FeedbackAssetsService assets) => assets.Preload(this, material);
	}
}
