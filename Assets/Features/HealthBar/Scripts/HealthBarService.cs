using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Game.HealthBar
{
	public enum HealthBarTeam
	{
		Hero,
		Enemy
	}

	// Owns the Addressable handle of the health bar prefab: loaded once at startup, reused for every bar, released on Reset.
	public class HealthBarService : IService
	{
		private AsyncOperationHandle<GameObject> _prefabHandle;
		private HealthBarView _prefab;

		public Type[] GetDependencies() => null;

		public async UniTask<bool> Initialize()
		{
			// Failure must never break gameplay: without the prefab the game simply runs without health bars.
			try
			{
				_prefabHandle = Addressables.LoadAssetAsync<GameObject>(HealthBarConfig.Instance.Prefab);
				GameObject prefab = await _prefabHandle.ToUniTask();

				if (prefab == null || !prefab.TryGetComponent(out _prefab))
				{
					Debug.LogWarning("Health bar prefab has no HealthBarView. Health bars are disabled.");
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Health bar prefab could not be loaded. Health bars are disabled. {e.Message}");
				ReleasePrefab();
			}

			return true;
		}

		public UniTask Reset()
		{
			ReleasePrefab();
			return UniTask.CompletedTask;
		}

		// Returns null when the prefab is not available, callers just skip the bar.
		public HealthBarView Create(Transform target, float height, HealthBarTeam team)
		{
			if (_prefab == null) return null;

			HealthBarConfig config = HealthBarConfig.Instance;
			bool isHero = team == HealthBarTeam.Hero;

			HealthBarView healthBar = Object.Instantiate(_prefab);
			healthBar.Bind(config, target, height,
				isHero ? config.HeroColor : config.EnemyColor,
				isHero ? config.HeroScale : config.EnemyScale,
				!isHero && config.HideEnemyBarWhenFull,
				isHero && config.ShowHeroHealthValue);
			return healthBar;
		}

		private void ReleasePrefab()
		{
			_prefab = null;
			if (_prefabHandle.IsValid()) Addressables.Release(_prefabHandle);
		}
	}
}
