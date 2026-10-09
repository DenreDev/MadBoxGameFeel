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
		private const string CritNumberSuffix = "!";
		private static readonly int[] PentatonicSemitones = { 0, 2, 4, 7, 9 }; // major pentatonic: any two notes of it sound good together

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
		private CurrencyOrbView _currencyOrbPrefab;
		private GameObject _currencyCollectPrefab;
		private AudioClip _currencyDropClip;
		private AudioClip _currencyCollectClip;
		private AudioSource _currencyDropSource;
		private AudioSource _currencyCollectSource;
		private AudioClip _waveStartClip;
		private AudioSource _waveStartSource;
		private AudioClip _comboHitClip;
		private AudioClip _comboMilestoneClip;
		private AudioClip _comboBreakClip;
		private AudioSource _comboHitSource; // the bell's pitch changes with every kill, so it keeps its own source
		private AudioSource _comboStingSource; // milestone and break, always at the normal pitch
		private int _comboHitSoundFrame = -1;
		private Transform _orbRoot; // every live orb is a child, so a round end can reach them all
		private float _lastCollectTime = float.NegativeInfinity;
		private int _collectChain;
		private int _collectSoundFrame = -1;

		// Where the currency orbs fly to (the hero). Set by the hero's view, null while there is nobody to reach.
		public Transform CurrencyTarget { get; set; }
		public Material FlashMaterial { get; private set; }
		public Material AttackWarnMaterial { get; private set; }
		public Material AttackRangeMaterial { get; private set; }

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
			AttackRangeMaterial = await Load(config.AttackRangeMaterial);
			_whooshClip = await Load(config.SwordWhooshClip);
			_hitClip = await Load(config.HitImpactClip);
			_killClip = await Load(config.KillImpactClip);
			_currencyCollectPrefab = await Load(config.CurrencyCollectPrefab);
			_currencyDropClip = await Load(config.CurrencyDropClip);
			_currencyCollectClip = await Load(config.CurrencyCollectClip);
			_waveStartClip = await Load(config.WaveStartClip);
			_comboHitClip = await Load(config.ComboHitClip);
			_comboMilestoneClip = await Load(config.ComboMilestoneClip);
			_comboBreakClip = await Load(config.ComboBreakClip);
			await LoadCurrencyOrb(config);
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
			if (_orbRoot != null) Object.Destroy(_orbRoot.gameObject);
			_orbRoot = null;
			CurrencyTarget = null;
			_collectChain = 0;
			_currencyDropSource = _currencyCollectSource = null;
			_currencyDropClip = _currencyCollectClip = null;
			_waveStartClip = null;
			_waveStartSource = null;
			_comboHitClip = _comboMilestoneClip = _comboBreakClip = null;
			_comboHitSource = _comboStingSource = null;
			_currencyOrbPrefab = null;
			_currencyCollectPrefab = null;
			_sparkPrefab = null;
			_spawnPuffPrefab = null;
			_stepPuffPrefab = null;
			_heroRingPrefab = null;
			_attackWarningPrefab = null;
			_damageNumberPrefab = null;
			FlashMaterial = null;
			AttackWarnMaterial = null;
			AttackRangeMaterial = null;
			return UniTask.CompletedTask;
		}

		public void PlayHitSpark(Vector3 position, bool isCritical = false)
		{
			if (_sparkPrefab == null) return;

			// The prefab destroys itself when its particles are done (Stop Action = Destroy).
			GameObject spark = Object.Instantiate(_sparkPrefab, position, Quaternion.identity);
			if (isCritical) spark.transform.localScale *= GameFeelConfig.Instance.CritSparkScale;
		}

		// Freeze frame, kept for big impacts only: a regular hit or a lone kill never freezes, otherwise every swing would stutter.
		// Kills chained within the streak window count as a streak (a swing killing several enemies, or a crowd going down).
		// From HitStopMinKillStreak kills on, each kill freezes the game for a few frames, longer and longer with the streak.
		// A critical strike always freezes, even a lone hit: CritStopDuration, or the streak freeze when that one is longer.
		// Overlapping freezes do not stack: the longest one wins.
		public void PlayHitStop(bool isKillingBlow, bool isCritical = false)
		{
			if (!isKillingBlow && !isCritical) return;

			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			float now = Time.unscaledTime;
			float duration = 0f;
			if (isKillingBlow)
			{
				_killStreak = now - _lastKillTime <= config.KillStreakWindow ? _killStreak + 1 : 1;
				_lastKillTime = now;
				if (_killStreak >= config.HitStopMinKillStreak)
				{
					int extraKills = _killStreak - Mathf.Max(1, config.HitStopMinKillStreak);
					duration = config.KillStopDuration + extraKills * config.KillStreakStopBonus;
				}
			}

			if (isCritical) duration = Mathf.Max(duration, config.CritStopDuration);
			duration = Mathf.Min(duration, config.MaxHitStopDuration);
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

		// Thump when the sword hits, heavier crunch when it kills or crits. One of each per frame: a swing hitting several enemies is one blow, not three.
		// The kill sound climbs in pitch with the kill streak, so call this after PlayHitStop (which counts the streak).
		public void PlayHitSound(bool isKillingBlow, bool isCritical = false)
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			if (isKillingBlow || isCritical)
			{
				if (_killSoundFrame == Time.frameCount) return;

				_killSoundFrame = Time.frameCount;
				float pitch = isKillingBlow
					? Mathf.Min(config.MaxKillPitch, 1f + Mathf.Max(0, _killStreak - 1) * config.KillStreakPitchStep)
					: UnityEngine.Random.Range(0.95f, 1.05f);
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
			if (_whooshClip == null && _hitClip == null && _killClip == null && _currencyDropClip == null && _currencyCollectClip == null && _waveStartClip == null
				&& _comboHitClip == null && _comboMilestoneClip == null && _comboBreakClip == null) return;

			_audioHost = new GameObject("GameFeelAudio") { hideFlags = HideFlags.DontSave };

			// The base scene has no AudioListener, so nothing would be heard. The sounds are 2D, so the listener's position does not matter.
			if (Object.FindObjectOfType<AudioListener>() == null) _audioHost.AddComponent<AudioListener>();

			_whooshSource = CreateSource();
			_hitSource = CreateSource();
			_killSource = CreateSource();
			_currencyDropSource = CreateSource();
			_currencyCollectSource = CreateSource();
			_waveStartSource = CreateSource();
			_comboHitSource = CreateSource();
			_comboStingSource = CreateSource();
		}

		private AudioSource CreateSource()
		{
			AudioSource source = _audioHost.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.spatialBlend = 0f;
			return source;
		}

		// Stinger of the wave announcement. WaveCounterView calls it as the banner slams in.
		public void PlayWaveStart()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			PlaySound(_waveStartSource, _waveStartClip, config.WaveStartVolume, 1f);
		}

		// Bell of a combo kill. Step 0 is the first kill that shows the combo, each next kill climbs one note of the pentatonic scale (up to ComboMaxSemitones).
		// One bell per frame: a swing killing several enemies is one beat, not a chord.
		public void PlayComboHit(int step)
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null || _comboHitSoundFrame == Time.frameCount) return;

			_comboHitSoundFrame = Time.frameCount;
			step = Mathf.Max(0, step);
			int semitones = Mathf.Min(config.ComboMaxSemitones, 12 * (step / PentatonicSemitones.Length) + PentatonicSemitones[step % PentatonicSemitones.Length]);
			PlaySound(_comboHitSource, _comboHitClip, config.ComboHitVolume, Mathf.Pow(2f, semitones / 12f));
		}

		// Sparkle of a new combo tier.
		public void PlayComboMilestone()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			PlaySound(_comboStingSource, _comboMilestoneClip, config.ComboMilestoneVolume, 1f);
		}

		public void PlayComboBreak()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (config == null) return;

			PlaySound(_comboStingSource, _comboBreakClip, config.ComboBreakVolume, 1f);
		}

		// A dead enemy bursts into currency orbs: they hop out around it, bounce, rest, then CurrencyOrbView calls them to CurrencyTarget.
		// Nothing is dropped without a target or after a failed load: the kill just has no orbs.
		public void PlayCurrencyDrop(Vector3 position)
		{
			if (_currencyOrbPrefab == null || CurrencyTarget == null) return;

			GameFeelConfig config = GameFeelConfig.Instance;
			if (_orbRoot == null) _orbRoot = new GameObject("CurrencyOrbs").transform;

			int count = UnityEngine.Random.Range(config.CurrencyOrbsPerKill.x, config.CurrencyOrbsPerKill.y + 1);
			float firstAngle = UnityEngine.Random.value * 360f;
			Vector3 spawnPosition = position + Vector3.up * config.CurrencyDropHeight;

			for (int i = 0; i < count; i++)
			{
				// Evenly spread around the enemy with a little jitter, so the burst reads as a ring, not a clump.
				float angle = (firstAngle + (i + UnityEngine.Random.Range(-0.2f, 0.2f)) * 360f / count) * Mathf.Deg2Rad;
				float speed = UnityEngine.Random.Range(config.CurrencyBurstSpeed.x, config.CurrencyBurstSpeed.y);
				Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, config.CurrencyBurstUpSpeed * UnityEngine.Random.Range(0.8f, 1.2f), Mathf.Sin(angle) * speed);

				CurrencyOrbView orb = Object.Instantiate(_currencyOrbPrefab, spawnPosition, Quaternion.identity, _orbRoot);
				orb.Launch(this, velocity, position.y);
			}

			PlaySound(_currencyDropSource, _currencyDropClip, config.CurrencyDropVolume, UnityEngine.Random.Range(0.95f, 1.05f));
		}

		// An orb reached the hero: sparkle burst at the contact point and a blip that climbs in pitch with each orb of the chain.
		// One blip per frame, so orbs arriving together do not stack into a louder sound.
		public void PlayCurrencyCollected(Vector3 position)
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (_currencyCollectPrefab != null) Object.Instantiate(_currencyCollectPrefab, position, Quaternion.identity);

			float now = Time.unscaledTime;
			_collectChain = now - _lastCollectTime <= config.CurrencyChainWindow ? _collectChain + 1 : 0;
			_lastCollectTime = now;

			if (_collectSoundFrame == Time.frameCount) return;

			_collectSoundFrame = Time.frameCount;
			float pitch = Mathf.Min(config.CurrencyMaxPitch, 1f + _collectChain * config.CurrencyChainPitchStep);
			PlaySound(_currencyCollectSource, _currencyCollectClip, config.CurrencyCollectVolume, pitch);
		}

		// The hero died: orbs still on the floor or in flight shrink away instead of waiting for a hero who is gone.
		public void ClearCurrency()
		{
			if (_orbRoot == null) return;

			foreach (CurrencyOrbView orb in _orbRoot.GetComponentsInChildren<CurrencyOrbView>()) orb.Vanish();
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
		// A critical strike gets its own colors, a "!" and is bigger (on top of the kill size).
		public void PlayDamageNumber(Vector3 hitPoint, int damage, bool isKillingBlow, bool isCritical = false)
		{
			if (_damageNumberPrefab == null) return;

			GameFeelConfig config = GameFeelConfig.Instance;
			Vector3 position = hitPoint + Vector3.up * config.DamageNumberHeight;
			DamageNumber number = _damageNumberPrefab.Spawn(position, damage);

			// Set on every spawn: pooled numbers keep the look of their previous use.
			Color top = isCritical ? config.CritNumberTopColor : isKillingBlow ? config.KillNumberTopColor : config.HitNumberTopColor;
			Color bottom = isCritical ? config.CritNumberBottomColor : isKillingBlow ? config.KillNumberBottomColor : config.HitNumberBottomColor;
			number.SetGradientColor(top, top, bottom, bottom);

			float scale = config.HitNumberScale;
			if (isKillingBlow) scale *= config.KillNumberScale;
			if (isCritical) scale *= config.CritNumberScale;
			number.SetScale(scale);

			number.enableRightText = isCritical;
			number.rightText = isCritical ? CritNumberSuffix : "";
			number.UpdateText();
		}

		private async UniTask LoadCurrencyOrb(GameFeelConfig config)
		{
			GameObject prefab = await Load(config.CurrencyOrbPrefab);
			if (prefab == null) return;

			if (!prefab.TryGetComponent(out _currencyOrbPrefab))
			{
				Debug.LogWarning("Currency orb prefab has no CurrencyOrbView component. Currency orbs are disabled.");
			}
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
