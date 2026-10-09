using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFlow;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	// Decides when, what and where enemies appear. EnemiesController owns them once they are spawned.
	// When:  waves follow each other on a timer counted in gameplay time (start screen, game over and freeze frames pause it).
	//        Each wave opens with a burst so the pressure rises at once, then spawns a group every interval while below its max alive count.
	//        The last wave repeats until the hero dies.
	// What:  a weighted pick among the wave's enemies whose prefab is loaded.
	// Where: just outside the screen, read from the camera so it follows the zoom. Sides are rarer than top and bottom (see PickScreenEdgePoint).
	public class EnemyWaveSpawner
	{
		public event System.Action<int> OnWaveStarted; // wave number, counted from 1: raised as the wave opens, before its first burst

		private EnemiesController _enemiesController;
		private HeroController _heroController;
		private GameFlowService _gameFlowService;
		private EnemyAssetsService _enemyAssets;
		private CancellationTokenSource _cancellationTokenSource;
		private Camera _camera;

		private int _waveIndex; // -1 before the first wave of the round
		private float _waveTime; // gameplay seconds since the current wave (or the round) started
		private float _nextSpawnTime; // _waveTime of the next group

		public void Initialize(EnemiesController enemiesController, HeroController heroController, GameFlowService gameFlowService, EnemyAssetsService enemyAssets)
		{
			_enemiesController = enemiesController;
			_heroController = heroController;
			_gameFlowService = gameFlowService;
			_enemyAssets = enemyAssets;
			_cancellationTokenSource = new CancellationTokenSource();

			Restart();
			UpdateLoop(_cancellationTokenSource.Token).Forget();
		}

		public void Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = null;
		}

		// Back before the first wave, for the next round.
		public void Restart()
		{
			_waveIndex = -1;
			_waveTime = 0f;
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_gameFlowService.IsPlaying && !_heroController.CurrentState.IsDead) Tick(Time.deltaTime);
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void Tick(float deltaTime)
		{
			EnemiesConfig config = EnemiesConfig.Instance;
			if (config.Waves == null || config.Waves.Count == 0) return;

			_waveTime += deltaTime;

			if (_waveIndex < 0)
			{
				if (_waveTime >= config.RoundStartDelay) StartWave(0);
				return;
			}

			EnemyWave wave = config.Waves[_waveIndex];
			if (_waveIndex < config.Waves.Count - 1 && _waveTime >= wave.Duration)
			{
				StartWave(_waveIndex + 1);
				return;
			}

			if (_waveTime < _nextSpawnTime) return;

			_nextSpawnTime = _waveTime + wave.SpawnInterval;
			Spawn(wave, wave.GroupSize);
		}

		private void StartWave(int waveIndex)
		{
			_waveIndex = waveIndex;
			_waveTime = 0f;

			EnemyWave wave = EnemiesConfig.Instance.Waves[waveIndex];
			_nextSpawnTime = wave.SpawnInterval;
			OnWaveStarted?.Invoke(waveIndex + 1);
			Spawn(wave, wave.StartBurst);
		}

		// Spawns up to count enemies, in groups of the wave's group size, never above its max alive count.
		private void Spawn(EnemyWave wave, int count)
		{
			count = Mathf.Min(count, wave.MaxAlive - _enemiesController.Enemies.Count);
			while (count > 0)
			{
				int groupSize = Mathf.Min(count, wave.GroupSize);
				SpawnGroup(wave, groupSize);
				count -= groupSize;
			}
		}

		// The whole group comes from the same point outside the screen, slightly spread so the enemies do not overlap.
		private void SpawnGroup(EnemyWave wave, int groupSize)
		{
			Vector3 groupCenter = GetOffscreenPosition();
			for (int i = 0; i < groupSize; i++)
			{
				EnemyConfig enemy = PickEnemy(wave);
				if (enemy == null) return;

				Vector2 offset = groupSize > 1 ? Random.insideUnitCircle * EnemiesConfig.Instance.GroupSpread : Vector2.zero;
				_enemiesController.SpawnEnemy(enemy, groupCenter + new Vector3(offset.x, 0f, offset.y));
			}
		}

		// Weighted random among the wave's enemies whose prefab is loaded. Null when none is available.
		private EnemyConfig PickEnemy(EnemyWave wave)
		{
			float totalWeight = 0f;
			foreach (WaveEnemy entry in wave.Enemies)
			{
				if (entry.Weight > 0f && _enemyAssets.IsAvailable(entry.Enemy)) totalWeight += entry.Weight;
			}

			float roll = Random.value * totalWeight;
			EnemyConfig picked = null;
			foreach (WaveEnemy entry in wave.Enemies)
			{
				if (entry.Weight <= 0f || !_enemyAssets.IsAvailable(entry.Enemy)) continue;

				picked = entry.Enemy;
				roll -= entry.Weight;
				if (roll <= 0f) break;
			}

			return picked;
		}

		// A point on the ground (at the hero's height) just beyond a screen edge, pushed out by the off-screen margin.
		// Read from the real camera: the tilted camera shows more ground above the hero than below, and the zoom widens the view, both for free.
		private Vector3 GetOffscreenPosition()
		{
			EnemiesConfig config = EnemiesConfig.Instance;
			Vector3 heroPosition = _heroController.CurrentState.Position;

			if (_camera == null) _camera = Camera.main;
			if (_camera != null && TryProjectOnGround(PickScreenEdgePoint(config), heroPosition.y, out Vector3 edgePoint))
			{
				Vector3 outward = edgePoint - heroPosition;
				outward.y = 0f;
				return edgePoint + outward.normalized * config.OffscreenMargin;
			}

			// No camera, or an edge that does not look at the ground: a circle around the hero.
			float angle = Random.Range(0f, 2f * Mathf.PI);
			return heroPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * config.SpawnRadius;
		}

		// Random point on a screen edge, in viewport coordinates (0..1). In portrait the left and right edges are close to the hero,
		// so enemies coming from there reach him fast and feel unfair: they are the rarest.
		private static Vector2 PickScreenEdgePoint(EnemiesConfig config)
		{
			float along = Random.value;
			float roll = Random.value * (config.TopEdgeWeight + config.BottomEdgeWeight + config.SideEdgeWeight);

			if (roll < config.TopEdgeWeight) return new Vector2(along, 1f);
			roll -= config.TopEdgeWeight;
			if (roll < config.BottomEdgeWeight) return new Vector2(along, 0f);
			return new Vector2(Random.value < 0.5f ? 0f : 1f, along);
		}

		private bool TryProjectOnGround(Vector2 viewportPoint, float groundHeight, out Vector3 point)
		{
			Ray ray = _camera.ViewportPointToRay(viewportPoint);
			Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));

			if (ground.Raycast(ray, out float distance))
			{
				point = ray.GetPoint(distance);
				return true;
			}

			point = default;
			return false;
		}
	}
}
