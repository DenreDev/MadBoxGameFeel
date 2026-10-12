using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// What an enemy attack can see and do while it runs. EnemiesController implements it, so attacks stay free of the enemy registry and the hero controller.
	public interface IEnemyAttackContext
	{
		// The enemy is alive, the hero is alive and the round is still playing: an attack that outlives any of them must stop and hit nothing.
		bool IsActive { get; }

		Vector3 Position { get; } // the enemy, flat on the ground
		Vector3 HeroPosition { get; }
		bool IsHeroInRange { get; } // the hero is within the enemy's attack range right now

		void StartWindup(float duration); // the enemy telegraphs its attack for this long (feedback only)
		void StartStrike(); // the attack animation starts (feedback only)
		void MoveTo(Vector3 position); // lunge, dash...
		void HitHero(); // the hero takes the enemy's attack damage
		void Explode(); // the enemy dies without being killed: no kill feedback, no reward for the hero
	}
}
