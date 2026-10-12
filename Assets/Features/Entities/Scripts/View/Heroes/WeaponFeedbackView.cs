using Core.ServicesManager;
using Game.GameFeel;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	// The feel of the hero's current weapon when it swings and lands: the whoosh, the impact sounds and the freeze frame on big blows.
	// Every value comes from the weapon's own configs (WeaponConfig.Sounds and HitStop), so switching weapons switches the feel.
	// Only presentation: it listens to the gameplay events, the damage rules live in HeroAttack and EnemiesController.
	public class WeaponFeedbackView : MonoBehaviour
	{
		private HeroController _heroController;
		private EnemiesController _enemiesController;
		private WeaponsService _weaponsService;
		private FeedbackAssetsService _assets;
		private FeedbackAudioService _audio;
		private HitStopService _hitStop;
		private float _lastKillTime = float.NegativeInfinity; // unscaled time: a freeze must not stretch the streak window
		private int _killStreak;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_heroController = entitiesService.HeroController;
			_enemiesController = entitiesService.EnemiesController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			_audio = ServicesLocator.Instance.GetService<FeedbackAudioService>();
			_hitStop = ServicesLocator.Instance.GetService<HitStopService>();

			_heroController.Attack.OnAttackStarted += OnHeroAttackStarted;
			_enemiesController.OnEnemyDamaged += OnEnemyDamaged;
			_enemiesController.OnEnemyKilled += OnEnemyKilled;
		}

		private void OnDestroy()
		{
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_heroController != null) _heroController.Attack.OnAttackStarted -= OnHeroAttackStarted;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemyDamaged -= OnEnemyDamaged;
				_enemiesController.OnEnemyKilled -= OnEnemyKilled;
			}
		}

		// Whoosh when the swing starts: the sound peaks just before the slash lands.
		private void OnHeroAttackStarted(int targetEnemyId)
		{
			WeaponSoundsConfig sounds = _weaponsService.CurrentWeapon != null ? _weaponsService.CurrentWeapon.Sounds : null;
			if (sounds != null) _audio.PlayVaried(_assets.Get(sounds.SwingClip), sounds.SwingVolume);
		}

		private void OnEnemyDamaged(int enemyId, int damage, bool isCritical) => PlayImpact(false, isCritical);
		private void OnEnemyKilled(int enemyId, int damage, bool isCritical) => PlayImpact(true, isCritical);

		// Kills chained within the weapon's streak window count as a streak (a swing killing several enemies, or a crowd going down):
		// the freeze grows with it and the kill sound climbs in pitch. A critical strike always gets the crunch and its own freeze.
		// A regular hit only thumps. The audio plays each clip once per frame, so a multi-hit swing is one blow, not three.
		private void PlayImpact(bool isKillingBlow, bool isCritical)
		{
			WeaponConfig weapon = _weaponsService.CurrentWeapon;
			if (weapon == null) return;

			HitStopConfig hitStop = weapon.HitStop;
			if (isKillingBlow)
			{
				float now = Time.unscaledTime;
				float window = hitStop != null ? hitStop.KillStreakWindow : 0f;
				_killStreak = now - _lastKillTime <= window ? _killStreak + 1 : 1;
				_lastKillTime = now;
			}

			if (hitStop != null) _hitStop.Freeze(hitStop.GetDuration(isKillingBlow ? _killStreak : 0, isCritical), hitStop.TimeScale);

			WeaponSoundsConfig sounds = weapon.Sounds;
			if (sounds == null) return;

			if (isKillingBlow)
			{
				float pitch = Mathf.Min(sounds.MaxKillPitch, 1f + (_killStreak - 1) * sounds.KillStreakPitchStep);
				_audio.Play(_assets.Get(sounds.KillClip), sounds.KillVolume, pitch);
			}
			else if (isCritical) _audio.PlayVaried(_assets.Get(sounds.KillClip), sounds.KillVolume);
			else _audio.PlayVaried(_assets.Get(sounds.HitClip), sounds.HitVolume);
		}
	}
}
