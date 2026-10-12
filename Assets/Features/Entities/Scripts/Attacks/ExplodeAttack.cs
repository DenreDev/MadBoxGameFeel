using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// Kamikaze: hits the instant the enemy reaches its attack range, with no windup, and dies doing so (no reward for the hero).
	[CreateAssetMenu(fileName = "ExplodeAttack", menuName = "Content/Enemy Attack/Explode")]
	public class ExplodeAttack : EnemyAttack
	{
		public override UniTask Perform(IEnemyAttackContext context, CancellationToken cancellationToken)
		{
			context.HitHero();
			context.Explode();
			return UniTask.CompletedTask;
		}
	}
}
