using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFeel;
using UnityEngine;

namespace Game.Weapons
{
	// A melee swing: the animation starts, the slash reaches its high point after HitDelay and hits the cone in front of the hero, then the hero recovers.
	// The reach of the cone is the weapon's Range.
	[CreateAssetMenu(fileName = "SlashAttack", menuName = "Content/Weapon Attack/Slash")]
	public class SlashAttack : WeaponAttack
	{
		[SerializeField, Tooltip("Seconds between the start of the swing and the slash's high point, when the damage lands. Match the hero's Attack animation (clip time of the impact / Attack state speed).")]
		private float hitDelay = 0.33f;
		[SerializeField, Tooltip("Seconds the whole swing animation lasts: clip length / Attack state speed (0.81), plus the blend back to idle. The hero keeps facing his target and cannot start another attack until it is over.")]
		private float swingDuration = 0.86f;
		[SerializeField, Min(1), Tooltip("How many enemies one swing can hit at the same time. The one the hero aims at is always hit first, then the closest ones in range. 1 = single target.")]
		private int maxTargets = 1;
		[SerializeField, Range(10f, 360f), Tooltip("Width in degrees of the cone in front of the hero that a swing covers, centered on the enemy he aims at. Enemies outside of it are not hit, even when they are closer than the range. 360 = all around him.")]
		private float arcAngle = 120f;
		[SerializeField, Tooltip("Look of the cone drawn on the ground in front of the hero. Its shape comes from the weapon's Range and the Arc Angle above")]
		private AttackRangeConfig rangeDisplay;

		public float HitDelay => hitDelay;
		public float SwingDuration => swingDuration;
		public int MaxTargets => Mathf.Max(1, maxTargets);
		public float ArcAngle => Mathf.Clamp(arcAngle, 10f, 360f);
		public AttackRangeConfig RangeDisplay => rangeDisplay;

		public override async UniTask Perform(IAttackContext context, WeaponConfig weapon, CancellationToken cancellationToken)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(hitDelay), cancellationToken: cancellationToken);

			if (context.CanHit)
			{
				context.TrackTarget();
				context.HitCone(weapon.Range, ArcAngle, MaxTargets);
			}

			await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, swingDuration - hitDelay)), cancellationToken: cancellationToken);
		}

		public override UniTask PreloadAssets(FeedbackAssetsService assets) => rangeDisplay != null ? rangeDisplay.PreloadAssets(assets) : UniTask.CompletedTask;
	}
}
