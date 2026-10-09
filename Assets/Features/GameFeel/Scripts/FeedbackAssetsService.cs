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
	// The only owner of the game feel Addressable handles. The feedback configs preload their assets here before gameplay starts
	// (FeedbackPreloadService walks the content catalogs), views read them back synchronously, and everything is released on Reset.
	// Handles are cached by key, so an asset shared by several configs (the hero and the bees use the same spark) is loaded once.
	public class FeedbackAssetsService : IService
	{
		private readonly Dictionary<object, AsyncOperationHandle<Object>> _handles = new Dictionary<object, AsyncOperationHandle<Object>>();

		public Type[] GetDependencies() => null;

		public UniTask<bool> Initialize() => UniTask.FromResult(true);

		public UniTask Reset()
		{
			// The numbers pooled by third-party package are copies of a prefab about to be released.
			DamageNumber.ClearPooled();

			foreach (AsyncOperationHandle<Object> handle in _handles.Values)
			{
				if (handle.IsValid()) Addressables.Release(handle);
			}

			_handles.Clear();
			return UniTask.CompletedTask;
		}

		// Loads every reference not loaded yet. owner is the config asking, named in the warnings.
		// Failure must never break gameplay: a missing or broken asset only disables its own feedback.
		public async UniTask Preload(Object owner, params AssetReference[] references)
		{
			foreach (AssetReference reference in references)
			{
				if (reference == null || !reference.RuntimeKeyIsValid())
				{
					Debug.LogWarning($"{owner.name}: an asset is not assigned. Its feedback is disabled.", owner);
					continue;
				}

				object key = reference.RuntimeKey;
				if (_handles.ContainsKey(key)) continue;

				AsyncOperationHandle<Object> handle = Addressables.LoadAssetAsync<Object>(key);
				_handles[key] = handle;
				try
				{
					await handle.ToUniTask();
				}
				catch (Exception e)
				{
					Debug.LogWarning($"{owner.name}: an asset could not be loaded. Its feedback is disabled. {e.Message}", owner);
					_handles.Remove(key);
					if (handle.IsValid()) Addressables.Release(handle);
				}
			}
		}

		// The preloaded asset, or null when it is unassigned, was not preloaded or failed to load: the caller skips its feedback.
		public T Get<T>(AssetReferenceT<T> reference) where T : Object
		{
			if (reference == null || !reference.RuntimeKeyIsValid()) return null;

			return _handles.TryGetValue(reference.RuntimeKey, out AsyncOperationHandle<Object> handle) && handle.IsDone ? handle.Result as T : null;
		}

		// One-shot effect at a position. Null when the prefab is missing. The effect prefabs destroy themselves when their particles are done (Stop Action = Destroy).
		public GameObject Spawn(AssetReferenceGameObject reference, Vector3 position)
		{
			GameObject prefab = Get(reference);
			return prefab != null ? Object.Instantiate(prefab, position, Quaternion.identity) : null;
		}

		// Effect that lives under its parent. Null when the prefab is missing, the caller just has no effect.
		public GameObject Spawn(AssetReferenceGameObject reference, Transform parent)
		{
			GameObject prefab = Get(reference);
			return prefab != null ? Object.Instantiate(prefab, parent) : null;
		}
	}
}
