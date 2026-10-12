using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public struct EnemyState
	{
		public int Id { get; }
		public Vector3 Position { get; }
		public int Health { get; }
		public EnemyConfig Config { get; }
		public float LastAttackTime { get; }
		public bool IsAttacking { get; } // committed to an attack (EnemyAttack.Perform is running): not chasing, not pushed by chasers
		public Vector3 Knockback { get; } // m/s of slide from a shove (a neighbour's death), slowed down by friction

		public EnemyState(int id, Vector3 position, int health, EnemyConfig config, float lastAttackTime = 0f, bool isAttacking = false, Vector3 knockback = default)
		{
			Id = id;
			Position = position;
			Health = health;
			Config = config;
			LastAttackTime = lastAttackTime;
			IsAttacking = isAttacking;
			Knockback = knockback;
		}

		public EnemyState WithPosition(Vector3 position) => new EnemyState(Id, position, Health, Config, LastAttackTime, IsAttacking, Knockback);
		public EnemyState WithHealth(int health) => new EnemyState(Id, Position, health, Config, LastAttackTime, IsAttacking, Knockback);
		public EnemyState WithAttackStarted() => new EnemyState(Id, Position, Health, Config, LastAttackTime, true, Knockback);
		public EnemyState WithAttackFinished(float time) => new EnemyState(Id, Position, Health, Config, time, false, Knockback);
		public EnemyState WithKnockback(Vector3 knockback) => new EnemyState(Id, Position, Health, Config, LastAttackTime, IsAttacking, knockback);
	}
}
