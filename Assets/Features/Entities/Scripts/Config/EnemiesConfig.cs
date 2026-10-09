using System.Collections.Generic;
using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	[CreateAssetMenu(fileName = "EnemiesConfig", menuName = "Game/EnemiesConfig")]
	public class EnemiesConfig : ScriptableObjectSingleton<EnemiesConfig>
	{
		[SerializeField]
		[Tooltip("Waves played in order from the start of a round. The last one repeats until the hero dies")]
		private List<EnemyWave> waves;

		[SerializeField]
		[Tooltip("Seconds between the tap and the first wave: the camera pulls back to the gameplay framing first, so the first enemies spawn outside of the real view")]
		private float roundStartDelay = 1f;

		[Header("Spawn placement")]
		[SerializeField]
		[Tooltip("Meters beyond the screen edge where enemies appear, so they walk in instead of popping up in view")]
		private float offscreenMargin = 1.5f;

		[SerializeField]
		[Tooltip("Relative chance to come from the top of the screen. The top shows the most ground, so the player sees them coming longest")]
		private float topEdgeWeight = 5f;

		[SerializeField]
		[Tooltip("Relative chance to come from the bottom of the screen")]
		private float bottomEdgeWeight = 3f;

		[SerializeField]
		[Tooltip("Relative chance to come from the left or right (shared). Kept low: in portrait the sides are close to the hero, so these enemies surprise the most")]
		private float sideEdgeWeight = 2f;

		[SerializeField]
		[Tooltip("Meters between the enemies of one spawn group")]
		private float groupSpread = 1.2f;

		[SerializeField]
		[Tooltip("Fallback when there is no camera to read the screen from: enemies spawn on a circle of this radius around the hero")]
		private float spawnRadius = 10f;

		[Header("Crowd movement")]
		[SerializeField, Min(0f)]
		[Tooltip("Meters of free space enemies try to keep between their bodies. They start steering away from a neighbour this far before touching it")]
		private float avoidanceMargin = 0.6f;

		[SerializeField, Min(0f)]
		[Tooltip("How hard enemies steer away from close neighbours compared to chasing the hero. Low = they bump and get shoved, high = they spread wide and flow around each other")]
		private float avoidanceStrength = 1.5f;

		[Header("Death shove")]
		[SerializeField, Min(0f)]
		[Tooltip("Meters around a killed enemy within which living enemies get shoved. 0 = no shove")]
		private float deathShoveRadius = 2.5f;

		[SerializeField, Min(0f)]
		[Tooltip("Starting slide speed (m/s) of a neighbour right next to the corpse when both weigh the same. Fades with distance; lighter bees fly further, heavy corpses shove harder")]
		private float deathShoveSpeed = 6f;

		[SerializeField, Min(0.01f)]
		[Tooltip("Ground friction (m/s²) that slows a shoved enemy down. Slide distance = speed² / (2 × friction)")]
		private float knockbackFriction = 15f;

		[SerializeField]
		[Tooltip("List of all available enemies in the game. Their prefabs are loaded at startup")]
		private List<EnemyConfig> enemies;

		public IReadOnlyList<EnemyWave> Waves => waves;
		public float RoundStartDelay => roundStartDelay;
		public float OffscreenMargin => offscreenMargin;
		public float TopEdgeWeight => topEdgeWeight;
		public float BottomEdgeWeight => bottomEdgeWeight;
		public float SideEdgeWeight => sideEdgeWeight;
		public float GroupSpread => groupSpread;
		public float SpawnRadius => spawnRadius;
		public float AvoidanceMargin => avoidanceMargin;
		public float AvoidanceStrength => avoidanceStrength;
		public float DeathShoveRadius => deathShoveRadius;
		public float DeathShoveSpeed => deathShoveSpeed;
		public float KnockbackFriction => knockbackFriction;
		public IReadOnlyList<EnemyConfig> Enemies => enemies;

		private Dictionary<string, EnemyConfig> _enemiesMap;

		public EnemyConfig GetEnemyById(string enemyId)
		{
			if (_enemiesMap == null)
			{
				_enemiesMap = new Dictionary<string, EnemyConfig>();
				foreach (EnemyConfig enemy in enemies) _enemiesMap[enemy.Id] = enemy;
			}

			_enemiesMap.TryGetValue(enemyId, out EnemyConfig config);
			return config;
		}
	}
}
