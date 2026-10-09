using System;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.GameFlow;
using Game.GamePlay.Heroes;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;

namespace Game.GamePlay.Entities
{
	public class EntitiesService : IService
	{
		public Type[] GetDependencies() => new[] { typeof(JoystickInputService), typeof(WeaponsService), typeof(GameFlowService), typeof(EnemyAssetsService) };

		private GameFlowService _gameFlowService;
		private EnemyWaveSpawner _waveSpawner;

		public EnemiesController EnemiesController { get; private set; }
		public HeroController HeroController { get; private set; }

		public async UniTask<bool> Initialize()
		{
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			WeaponsService weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			EnemyAssetsService enemyAssetsService = ServicesLocator.Instance.GetService<EnemyAssetsService>();

			EnemiesController = new EnemiesController();
			HeroController = new HeroController();

			await HeroController.Initialize(EnemiesController, joystickInputService, weaponsService, _gameFlowService);
			await EnemiesController.Initialize(HeroController, _gameFlowService);

			_waveSpawner = new EnemyWaveSpawner();
			_waveSpawner.Initialize(EnemiesController, HeroController, _gameFlowService, enemyAssetsService);

			// The only link between gameplay and the game flow: the hero's death ends the game, and a new round starts from scratch.
			HeroController.OnDied += OnHeroDied;
			_gameFlowService.OnStateChanged += OnGameStateChanged;

			return true;
		}

		public UniTask Reset()
		{
			if (HeroController != null) HeroController.OnDied -= OnHeroDied;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
			_waveSpawner?.Reset();
			return default;
		}

		private void OnHeroDied() => _gameFlowService.EndGame();

		// Back on the start screen: no bee left over, waves back to the first one and a fresh hero, ready for the next tap.
		private void OnGameStateChanged(GameState state)
		{
			if (state != GameState.Start) return;

			EnemiesController.ClearAllEnemies();
			_waveSpawner.Restart();
			HeroController.Restart();
		}
	}
}