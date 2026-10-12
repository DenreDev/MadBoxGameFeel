using UnityEngine;

namespace Game.Weapons
{
	// What a weapon attack can see and do while it runs. The hero implements it (HeroAttack), so weapons stay free of enemy and gameplay types.
	public interface IAttackContext
	{
		// The hero is alive and the round is still playing: an attack that outlives either must not hit anything.
		bool CanHit { get; }

		// Turns the aim toward the enemy this attack was started on (it keeps its last aim when that enemy died).
		void TrackTarget();

		// Hits the enemies within range and inside the cone in front of the hero, up to maxTargets: the aimed one first, then the closest.
		// Damage and criticals come from the weapon. It announces the impact (HeroAttack.OnAttacked) even when nobody is left to hit.
		void HitCone(float range, float arcAngle, int maxTargets);
	}
}
