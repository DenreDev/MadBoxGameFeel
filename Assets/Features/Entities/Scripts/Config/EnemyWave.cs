using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// One step of the enemy pressure curve. EnemyWaveSpawner plays the waves in order; the last one repeats until the hero dies.
	[Serializable]
	public class EnemyWave
	{
		[SerializeField, Min(1f), Tooltip("Seconds of gameplay before the next wave starts. Ignored on the last wave, which never ends")]
		private float duration = 20f;
		[SerializeField, Min(0), Tooltip("Enemies spawned at once when the wave starts, so the pressure rises right away")]
		private int startBurst = 3;
		[SerializeField, Min(0.1f), Tooltip("Seconds between two spawn groups during the wave")]
		private float spawnInterval = 1.5f;
		[SerializeField, Min(1), Tooltip("Enemies spawned together, from the same side of the screen")]
		private int groupSize = 1;
		[SerializeField, Min(1), Tooltip("No spawn while this many enemies are alive")]
		private int maxAlive = 8;
		[SerializeField, Tooltip("Which enemies this wave spawns and how often each one is picked")]
		private List<WaveEnemy> enemies = new List<WaveEnemy>();

		public float Duration => duration;
		public int StartBurst => startBurst;
		public float SpawnInterval => spawnInterval;
		public int GroupSize => groupSize;
		public int MaxAlive => maxAlive;
		public IReadOnlyList<WaveEnemy> Enemies => enemies;
	}

	[Serializable]
	public class WaveEnemy
	{
		[SerializeField] private EnemyConfig enemy;
		[SerializeField, Min(0f), Tooltip("Relative chance to be picked (2 = twice as often as 1)")]
		private float weight = 1f;

		public EnemyConfig Enemy => enemy;
		public float Weight => weight;
	}
}
