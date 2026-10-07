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
		private float _hitStopEndTime = float.NegativeInfinity; // unscaled time: the freeze must not depend on the time it freezes
		private float _lastKillTime = float.NegativeInfinity;
		private int _killStreak;
		private AudioClip _whooshClip;
		private AudioClip _hitClip;
		private AudioClip _killClip;
		private GameObject _audioHost;
		private AudioSource _whooshSource; // one source per sound: a pitch change must not bend the others
		private AudioSource _hitSource;
		private AudioSource _killSource;
		private int _hitSoundFrame = -1;
		private int _killSoundFrame = -1;

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
			_whooshClip = await Load(config.SwordWhooshClip);
			_hitClip = await Load(config.HitImpactClip);
			_killClip = await Load(config.KillImpactClip);
			CreateAudioSources();
			await LoadDamageNumber(config);

			return true;
		}

		public UniTask Reset()
		{
			_hitStopEndTime = float.NegativeInfinity;
			_killStreak = 0;
			Time.timeScale = 1f;

			// The numbers pooled by third-party package are copies of the prefab about to be released.
			if (_damageNumberPrefab != null) DamageNumber.ClearPooled();

			foreach (AsyncOperationHandle handle in _handles)
			{
				if (handle.IsValid()) Addressables.Release(handle);
			}

			_handles.Clear();
			if (_audioHost != null) Object.Destroy(_audioHost);
			_audioHost = null;
			_whooshSource = _hitSource = _killSource = null;
			_whooshClip = _hitClip = _killClip = null;
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

		// Freeze frame, kept for big impacts only: a regular hit or a lone kill never freezes, otherwise every swing would stutter.
		// Kills chained within the streak window count as a streak (a swing killing several enemies, or a crowd going down).
		// From HitStopMinKillStreak kills on, each kill freezes the game for a few frames, longer and longer with the streak.
		// Overlapping freezes do not stack: the longest one wins.
		public void PlayHitStop(bool isKillingBlow)
		{
			if (!isKillingBlow) return;

			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			float now = Time.unscaledTime;
			_killStreak = now - _lastKillTime <= config.KillStreakWindow ? _killStreak + 1 : 1;
			_lastKillTime = now;
			if (_killStreak < config.HitStopMinKillStreak) return;

			int extraKills = _killStreak - Mathf.Max(1, config.HitStopMinKillStreak);
			float duration = Mathf.Min(config.KillStopDuration + extraKills * config.KillStreakStopBonus, config.MaxHitStopDuration);
			if (duration <= 0f || now + duration <= _hitStopEndTime) return;

			bool alreadyFrozen = _hitStopEndTime > now;
			_hitStopEndTime = now + duration;
			Time.timeScale = config.HitStopTimeScale;
			if (!alreadyFrozen) EndHitStopWhenOver().Forget();
		}

		// Frame based wait, so it keeps running while the time scale is near zero.
		private async UniTaskVoid EndHitStopWhenOver()
		{
			while (Time.unscaledTime < _hitStopEndTime) await UniTask.Yield(PlayerLoopTiming.Update);

			Time.timeScale = 1f;
		}

		// Swing whoosh, played when the swing starts: the sound peaks just before the slash lands.
		public void PlaySwordWhoosh()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			PlaySound(_whooshSource, _whooshClip, config.SwordWhooshVolume, UnityEngine.Random.Range(0.95f, 1.05f));
		}

		// Thump when the sword hits, heavier crunch when it kills. One of each per frame: a swing hitting several enemies is one blow, not three.
		// The kill sound climbs in pitch with the kill streak, so call this after PlayHitStop (which counts the streak).
		public void PlayHitSound(bool isKillingBlow)
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			if (isKillingBlow)
			{
				if (_killSoundFrame == Time.frameCount) return;

				_killSoundFrame = Time.frameCount;
				float pitch = Mathf.Min(config.MaxKillPitch, 1f + Mathf.Max(0, _killStreak - 1) * config.KillStreakPitchStep);
				PlaySound(_killSource, _killClip, config.KillImpactVolume, pitch);
			}
			else
			{
				if (_hitSoundFrame == Time.frameCount) return;

				_hitSoundFrame = Time.frameCount;
				PlaySound(_hitSource, _hitClip, config.HitImpactVolume, UnityEngine.Random.Range(0.95f, 1.05f));
			}
		}

		private static void PlaySound(AudioSource source, AudioClip clip, float volume, float pitch)
		{
			if (source == null || clip == null) return;

			source.pitch = pitch;
			source.PlayOneShot(clip, volume);
		}

		// Plain 2D sources on one hidden object: the sounds are interface-like feedback, not positional.
		private void CreateAudioSources()
		{
			if (_whooshClip == null && _hitClip == null && _killClip == null) return;

			_audioHost = new GameObject("GameFeelAudio") { hideFlags = HideFlags.DontSave };

			// The base scene has no AudioListener, so nothing would be heard. The sounds are 2D, so the listener's position does not matter.
			if (Object.FindObjectOfType<AudioListener>() == null) _audioHost.AddComponent<AudioListener>();

			_whooshSource = CreateSource();
			_hitSource = CreateSource();
			_killSource = CreateSource();
		}

		private AudioSource CreateSource()
		{
			AudioSource source = _audioHost.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = 0f;
			return source;
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
