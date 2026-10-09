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
