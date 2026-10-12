using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// A telegraphed bite: the enemy stands still (Windup), then plays the attack animation with an optional lunge toward the hero (Strike).
	// The hit lands at the end of the animation, only if the hero is still in range, so walking away from the telegraph dodges it.
	[CreateAssetMenu(fileName = "MeleeAttack", menuName = "Content/Enemy Attack/Melee")]
	public class MeleeAttack : EnemyAttack
	{
		[SerializeField, Tooltip("Seconds the enemy stands still and telegraphs the attack once the hero is in range")]
		private float windupDuration = 0.6f;
		[SerializeField, Tooltip("Seconds of the attack animation. The hit lands at its end if the hero is still in range. Match the animation clip length")]
		private float strikeDuration = 0.45f;
		[SerializeField, Min(0f), Tooltip("Meters the enemy dashes toward the hero at the start of its attack animation, so a hero who ignores the telegraph gets hit. 0 = attacks in place")]
		private float lungeDistance;
		[SerializeField, Min(0f), Tooltip("Meters from the hero's position at which the lunge stops, so the bodies do not overlap")]
		private float lungeStopDistance = 0.8f;

		public override float ExtraReach => lungeDistance;

		public override async UniTask Perform(IEnemyAttackContext context, CancellationToken cancellationToken)
		{
			context.StartWindup(windupDuration);
			await UniTask.Delay(TimeSpan.FromSeconds(windupDuration), cancellationToken: cancellationToken);
			if (!context.IsActive) return;

			// The lunge aims at where the hero stands when the strike starts, not where he goes next: stepping aside still dodges it.
			Vector3 toHero = context.HeroPosition - context.Position;
			toHero.y = 0f;
			float lunge = Mathf.Clamp(toHero.magnitude - lungeStopDistance, 0f, lungeDistance);
			Vector3 lungeTarget = context.Position + toHero.normalized * lunge;

			// The dash takes the first half of the animation: a quick jump forward, then the bite.
			float lungeSpeed = lungeDistance / Mathf.Max(0.01f, strikeDuration * 0.5f);
			float strikeEnd = Time.time + strikeDuration;
			context.StartStrike();

			while (Time.time < strikeEnd)
			{
				if (!context.IsActive) return;

				if (context.Position != lungeTarget) context.MoveTo(Vector3.MoveTowards(context.Position, lungeTarget, lungeSpeed * Time.deltaTime));
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}

			if (context.IsActive && context.IsHeroInRange) context.HitHero();
		}
	}
}
