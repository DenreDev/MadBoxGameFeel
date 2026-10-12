using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GameFeel;
using Game.GamePlay.Enemies;
using Game.GamePlay.Heroes;
using Game.Weapons;

namespace Game.GamePlay.Entities
{
	// Preloads the feedback assets of the hero, every enemy and wave, and every weapon in the content catalogs, during the services startup,
	// so the first hit, spawn or kill never waits for a load. New content is covered just by being in its catalog.
	// It only walks the catalogs: the handles belong to FeedbackAssetsService, and each feedback config lists its own assets.
	public class FeedbackPreloadService : IService
	{
		private FeedbackAssetsService _assets;

		public Type[] GetDependencies() => new[] { typeof(FeedbackAssetsService) };

		public async UniTask<bool> Initialize()
		{
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();

			await PreloadHero(HeroConfig.Instance);
			foreach (EnemyConfig enemy in EnemiesConfig.Instance.Enemies) await PreloadEnemy(enemy);
			if (EnemiesConfig.Instance.WaveSounds != null) await EnemiesConfig.Instance.WaveSounds.PreloadAssets(_assets);
			foreach (WeaponConfig weapon in WeaponsConfig.Instance.Weapons) await PreloadWeapon(weapon);

			// Failure must never break gameplay: a missing asset only disables its own feedback.
			return true;
		}

		public UniTask Reset() => UniTask.CompletedTask;

		private async UniTask PreloadHero(HeroConfig hero)
		{
			if (hero == null) return;

			await _assets.Preload(hero, hero.GroundRingPrefab);
			if (hero.HitFeedback != null) await hero.HitFeedback.PreloadAssets(_assets);
			if (hero.Footsteps != null) await hero.Footsteps.PreloadAssets(_assets);
			if (hero.ComboSounds != null) await hero.ComboSounds.PreloadAssets(_assets);
		}

		private async UniTask PreloadEnemy(EnemyConfig enemy)
		{
			if (enemy == null) return;

			if (enemy.HitFeedback != null) await enemy.HitFeedback.PreloadAssets(_assets);
			if (enemy.DamageNumbers != null) await enemy.DamageNumbers.PreloadAssets(_assets);
			if (enemy.SpawnFeedback != null) await enemy.SpawnFeedback.PreloadAssets(_assets);
			if (enemy.AttackTelegraph != null) await enemy.AttackTelegraph.PreloadAssets(_assets);
			if (enemy.CurrencyDrop != null) await enemy.CurrencyDrop.PreloadAssets(_assets);
		}

		private async UniTask PreloadWeapon(WeaponConfig weapon)
		{
			if (weapon == null) return;

			if (weapon.Sounds != null) await weapon.Sounds.PreloadAssets(_assets);
			if (weapon.Attack != null) await weapon.Attack.PreloadAssets(_assets);
		}
	}
}
