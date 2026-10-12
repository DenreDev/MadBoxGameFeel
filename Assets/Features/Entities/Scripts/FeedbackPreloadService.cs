using System;
using System.Collections.Generic;
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

			// Every config starts its loads at once, so from a CDN the whole set downloads in parallel and the boot waits for the slowest, not the sum.
			List<UniTask> loads = new List<UniTask> { PreloadHero(HeroConfig.Instance) };
			foreach (EnemyConfig enemy in EnemiesConfig.Instance.Enemies) loads.Add(PreloadEnemy(enemy));
			if (EnemiesConfig.Instance.WaveSounds != null) loads.Add(EnemiesConfig.Instance.WaveSounds.PreloadAssets(_assets));
			foreach (WeaponConfig weapon in WeaponsConfig.Instance.Weapons) loads.Add(PreloadWeapon(weapon));
			await UniTask.WhenAll(loads);

			// Failure must never break gameplay: a missing asset only disables its own feedback.
			return true;
		}

		public UniTask Reset() => UniTask.CompletedTask;

		private async UniTask PreloadHero(HeroConfig hero)
		{
			if (hero == null) return;

			List<UniTask> loads = new List<UniTask> { _assets.Preload(hero, hero.GroundRingPrefab) };
			if (hero.HitFeedback != null) loads.Add(hero.HitFeedback.PreloadAssets(_assets));
			if (hero.Footsteps != null) loads.Add(hero.Footsteps.PreloadAssets(_assets));
			if (hero.ComboSounds != null) loads.Add(hero.ComboSounds.PreloadAssets(_assets));
			await UniTask.WhenAll(loads);
		}

		private async UniTask PreloadEnemy(EnemyConfig enemy)
		{
			if (enemy == null) return;

			List<UniTask> loads = new List<UniTask>();
			if (enemy.HitFeedback != null) loads.Add(enemy.HitFeedback.PreloadAssets(_assets));
			if (enemy.DamageNumbers != null) loads.Add(enemy.DamageNumbers.PreloadAssets(_assets));
			if (enemy.SpawnFeedback != null) loads.Add(enemy.SpawnFeedback.PreloadAssets(_assets));
			if (enemy.AttackTelegraph != null) loads.Add(enemy.AttackTelegraph.PreloadAssets(_assets));
			if (enemy.CurrencyDrop != null) loads.Add(enemy.CurrencyDrop.PreloadAssets(_assets));
			await UniTask.WhenAll(loads);
		}

		private async UniTask PreloadWeapon(WeaponConfig weapon)
		{
			if (weapon == null) return;

			List<UniTask> loads = new List<UniTask>();
			if (weapon.Sounds != null) loads.Add(weapon.Sounds.PreloadAssets(_assets));
			if (weapon.Attack != null) loads.Add(weapon.Attack.PreloadAssets(_assets));
			await UniTask.WhenAll(loads);
		}
	}
}
