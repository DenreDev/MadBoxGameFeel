using System;
using System.Collections.Generic;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Game.GameFeel
{
	// Owns the Addressable handles of the hit feedback content: loaded once at startup, reused for every hit, released on Reset.
	public class GameFeelService : IService
	{
		private readonly List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
		private GameObject _sparkPrefab;
		private GameObject _spawnPuffPrefab;
		private GameObject _stepPuffPrefab;
		private DamageNumber _damageNumberPrefab;
		private GameObject _heroRingPrefab;
		private GameObject _attackWarningPrefab;

		public Material FlashMaterial { get; private set; }
		public Material AttackWarnMaterial { get; private set; }

		public Type[] GetDependencies() => null;

		public async UniTask<bool> Initialize()
		{
			// Failure must never break gameplay: a missing config or asset only disables its own feedback.
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return true;

			_sparkPrefab = await Load(config.HitSparkPrefab);
			FlashMaterial = await Load(config.HitFlashMaterial);
			_spawnPuffPrefab = await Load(config.SpawnPuffPrefab);
			_stepPuffPrefab = await Load(config.StepPuffPrefab);
			_heroRingPrefab = await Load(config.HeroRingPrefab);
			_attackWarningPrefab = await Load(config.AttackWarningPrefab);
			AttackWarnMaterial = await Load(config.AttackWarnMaterial);
			await LoadDamageNumber(config);

			return true;
		}

		public UniTask Reset()
		{
			// The numbers pooled by third-party package are copies of the prefab about to be released.
			if (_damageNumberPrefab != null) DamageNumber.ClearPooled();

			foreach (AsyncOperationHandle handle in _handles)
			{
				if (handle.IsValid()) Addressables.Release(handle);
			}

			_handles.Clear();
			_sparkPrefab = null;
			_spawnPuffPrefab = null;
			_stepPuffPrefab = null;
			_heroRingPrefab = null;
			_attackWarningPrefab = null;
			_damageNumberPrefab = null;
			FlashMaterial = null;
			AttackWarnMaterial = null;
			return UniTask.CompletedTask;
		}

		public void PlayHitSpark(Vector3 position)
		{
			if (_sparkPrefab == null) return;

			// The prefab destroys itself when its particles are done (Stop Action = Destroy).
			Object.Instantiate(_sparkPrefab, position, Quaternion.identity);
		}

		public void PlaySpawnPuff(Vector3 position)
		{
			if (_spawnPuffPrefab == null) return;

			// Same as the spark: the prefab destroys itself when its particles are done.
			Object.Instantiate(_spawnPuffPrefab, position, Quaternion.identity);
		}

		public void PlayStepPuff(Vector3 position)
		{
			if (_stepPuffPrefab == null) return;

			// Same as the spark: the prefab destroys itself when its particles are done.
			Object.Instantiate(_stepPuffPrefab, position, Quaternion.identity);
		}

		// Ground ring that stays under its parent for as long as the parent lives. Null when the asset is missing, the caller just has no ring.
		public GameObject CreateHeroRing(Transform parent)
		{
			if (_heroRingPrefab == null) return null;

			return Object.Instantiate(_heroRingPrefab, parent);
		}

		// Red ground disc under an enemy that is about to attack, sized and animated by AttackTelegraphView. Null when the asset is missing, the caller just has no disc.
		public GameObject CreateAttackWarning(Transform parent)
		{
			if (_attackWarningPrefab == null) return null;

			return Object.Instantiate(_attackWarningPrefab, parent);
		}

		// Floating damage number (third-party package). Position, colors and size come from the config, the motion and fades from the prefab.
		public void PlayDamageNumber(Vector3 hitPoint, int damage, bool isKillingBlow)
		{
			if (_damageNumberPrefab == null) return;

			GameFeelConfig config = GameFeelConfig.Instance;
			Vector3 position = hitPoint + Vector3.up * config.DamageNumberHeight;
			DamageNumber number = _damageNumberPrefab.Spawn(position, damage);

			// Set on every spawn: pooled numbers keep the look of their previous use.
			Color top = isKillingBlow ? config.KillNumberTopColor : config.HitNumberTopColor;
			Color bottom = isKillingBlow ? config.KillNumberBottomColor : config.HitNumberBottomColor;
			number.SetGradientColor(top, top, bottom, bottom);
			number.SetScale(isKillingBlow ? config.HitNumberScale * config.KillNumberScale : config.HitNumberScale);
		}

		// The prefab is only the template: third-party package instantiates and pools the numbers from it.
		// Prewarming here keeps the first hit of the game from paying for the number creation.
		private async UniTask LoadDamageNumber(GameFeelConfig config)
		{
			GameObject prefab = await Load(config.DamageNumberPrefab);
			if (prefab == null) return;

			if (!prefab.TryGetComponent(out _damageNumberPrefab))
			{
				Debug.LogWarning("Damage number prefab has no DamageNumber component. Damage numbers are disabled.");
				return;
			}

			_damageNumberPrefab.PrewarmPool();
		}

		private async UniTask<T> Load<T>(AssetReferenceT<T> reference) where T : Object
		{
			if (!reference.RuntimeKeyIsValid())
			{
				Debug.LogWarning($"Game feel asset {typeof(T).Name} is not assigned in GameFeelConfig. Its feedback is disabled.");
				return null;
			}

			try
			{
				AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(reference);
				_handles.Add(handle);
				return await handle.ToUniTask();
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Game feel asset {typeof(T).Name} could not be loaded. Its feedback is disabled. {e.Message}");
				return null;
			}
		}
	}
}
