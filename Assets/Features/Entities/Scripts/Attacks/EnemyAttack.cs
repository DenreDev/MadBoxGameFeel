using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// How an enemy attacks: the one thing that differs between a biting bee, a kamikaze or a shooter. EnemiesController decides WHEN to attack
	// (the hero is in range and the cooldown is over) and then asks the attack to Perform. Plugged into EnemyConfig, so each enemy owns its attack and its tuning.
	public abstract class EnemyAttack : ScriptableObject
	{
		// Meters beyond the enemy's attack range that this attack can still reach (a lunge): the telegraph covers them too.
		public virtual float ExtraReach => 0f;

		// Runs the whole attack, from its first frame until the enemy is free to chase again, and lands the hit through the context.
		// Cancelled when the game shuts down.
		public abstract UniTask Perform(IEnemyAttackContext context, CancellationToken cancellationToken);
	}
}
