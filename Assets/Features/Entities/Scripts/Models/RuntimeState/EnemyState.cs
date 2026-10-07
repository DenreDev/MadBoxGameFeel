using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public enum EnemyAttackPhase
	{
		None, // chasing the hero or waiting for the cooldown
		Windup, // in range, standing still and telegraphing the attack
		Strike // the attack animation plays, the hit resolves when it ends
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

		public EnemyState(int id, Vector3 position, int health, EnemyConfig config, float lastAttackTime = 0f, EnemyAttackPhase attackPhase = EnemyAttackPhase.None, float phaseEndTime = 0f)
		{
			Id = id;
			Position = position;
			Health = health;
			Config = config;
			LastAttackTime = lastAttackTime;
			AttackPhase = attackPhase;
			PhaseEndTime = phaseEndTime;
		}

		public EnemyState WithPosition(Vector3 position) => new EnemyState(Id, position, Health, Config, LastAttackTime, AttackPhase, PhaseEndTime);
		public EnemyState WithHealth(int health) => new EnemyState(Id, Position, health, Config, LastAttackTime, AttackPhase, PhaseEndTime);
		public EnemyState WithPhase(EnemyAttackPhase attackPhase, float phaseEndTime) => new EnemyState(Id, Position, Health, Config, LastAttackTime, attackPhase, phaseEndTime);
		public EnemyState WithAttackFinished(float time) => new EnemyState(Id, Position, Health, Config, time);
	}
}
