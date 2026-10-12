using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public struct HeroState
	{
		public Vector3 Position { get; }
		public int Health { get; }

		public bool IsDead => Health <= 0;

		public HeroState(Vector3 position, int health)
		{
			Position = position;
			Health = health;
		}
	}
}
