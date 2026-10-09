using System;
using System.Collections.Generic;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.GamePlay.Enemies
{
	// Owns the Addressable handles of the enemy prefabs: all loaded at startup, before the first wave can need them,
	// kept for the whole session (enemies spawn all game long) and released on Reset.
	// An enemy whose prefab fails to load is never spawned: the waves pick among the available ones, the game goes on.
	public class EnemyAssetsService : IService
	{
		private readonly Dictionary<EnemyConfig, EnemyView> _prefabs = new Dictionary<EnemyConfig, EnemyView>();
		private readonly List<AsyncOperationHandle<GameObject>> _handles = new List<AsyncOperationHandle<GameObject>>();

		public Type[] GetDependencies() => null;

		public async UniTask<bool> Initialize()
		{
			EnemiesConfig config = EnemiesConfig.Instance;
			if (config == null) return true;

			// Started side by side: from a CDN the downloads overlap instead of queuing.
			List<UniTask> loads = new List<UniTask>();
			foreach (EnemyConfig enemy in config.Enemies) loads.Add(Load(enemy));
			await UniTask.WhenAll(loads);

			if (_prefabs.Count == 0) Debug.LogWarning("No enemy prefab could be loaded. No enemy will spawn.");
			return true;
		}

		public UniTask Reset()
		{
			foreach (AsyncOperationHandle<GameObject> handle in _handles)
			{
				if (handle.IsValid()) Addressables.Release(handle);
			}

			_handles.Clear();
			_prefabs.Clear();
			return UniTask.CompletedTask;
		}

		public bool IsAvailable(EnemyConfig enemy) => enemy != null && _prefabs.ContainsKey(enemy);

		public bool TryGetPrefab(EnemyConfig enemy, out EnemyView prefab)
		{
			prefab = null;
			return enemy != null && _prefabs.TryGetValue(enemy, out prefab);
		}

		private async UniTask Load(EnemyConfig enemy)
		{
			if (enemy == null || _prefabs.ContainsKey(enemy)) return;

			if (!enemy.Prefab.RuntimeKeyIsValid())
			{
				Debug.LogWarning($"Enemy {enemy.Id} has no prefab assigned. It will not spawn.");
				return;
			}

			try
			{
				AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(enemy.Prefab);
				_handles.Add(handle);
				GameObject prefab = await handle.ToUniTask();

				if (prefab.TryGetComponent(out EnemyView view)) _prefabs[enemy] = view;
				else Debug.LogWarning($"Enemy {enemy.Id} prefab has no EnemyView component. It will not spawn.");
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Enemy {enemy.Id} prefab could not be loaded. It will not spawn. {e.Message}");
			}
		}
	}
}
