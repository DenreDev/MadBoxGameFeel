using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public enum EnemyAttackPhase
	{
		None, // chasing the hero or waiting for the cooldown
		Windup, // in range, standing still and telegraphing the attack
		Strike // the attack animation plays (with an optional lunge), the hit resolves when it ends
	}

	public struct EnemyState
	{
		public int Id { get; }
		public Vector3 Position { get; }
		public int Health { get; }
		public EnemyConfig Config { get; }
		public float LastAttackTime { get; }
		public EnemyAttackPhase AttackPhase { get; }
		public float PhaseEndTime { get; } // Time.time at which the current Windup or Strike ends
		public Vector3 LungeTarget { get; } // during a Strike: where the lunge ends
		public Vector3 Knockback { get; } // m/s of slide from a shove (a neighbour's death), slowed down by friction

		public EnemyState(int id, Vector3 position, int health, EnemyConfig config, float lastAttackTime = 0f, EnemyAttackPhase attackPhase = EnemyAttackPhase.None, float phaseEndTime = 0f, Vector3 lungeTarget = default, Vector3 knockback = default)
		{
			Id = id;
			Position = position;
			Health = health;
			Config = config;
			LastAttackTime = lastAttackTime;
			AttackPhase = attackPhase;
			PhaseEndTime = phaseEndTime;
			LungeTarget = lungeTarget;
			Knockback = knockback;
		}

		public EnemyState WithPosition(Vector3 position) => new EnemyState(Id, position, Health, Config, LastAttackTime, AttackPhase, PhaseEndTime, LungeTarget, Knockback);
		public EnemyState WithHealth(int health) => new EnemyState(Id, Position, health, Config, LastAttackTime, AttackPhase, PhaseEndTime, LungeTarget, Knockback);
		public EnemyState WithPhase(EnemyAttackPhase attackPhase, float phaseEndTime) => new EnemyState(Id, Position, Health, Config, LastAttackTime, attackPhase, phaseEndTime, Position, Knockback);
		public EnemyState WithStrike(float phaseEndTime, Vector3 lungeTarget) => new EnemyState(Id, Position, Health, Config, LastAttackTime, EnemyAttackPhase.Strike, phaseEndTime, lungeTarget, Knockback);
		public EnemyState WithAttackFinished(float time) => new EnemyState(Id, Position, Health, Config, time, knockback: Knockback);
		public EnemyState WithKnockback(Vector3 knockback) => new EnemyState(Id, Position, Health, Config, LastAttackTime, AttackPhase, PhaseEndTime, LungeTarget, knockback);
	}
}
