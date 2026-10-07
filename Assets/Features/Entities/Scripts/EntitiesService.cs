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
		public Type[] GetDependencies() => new[] { typeof(JoystickInputService), typeof(WeaponsService), typeof(GameFlowService) };

		private GameFlowService _gameFlowService;

		public EnemiesController EnemiesController { get; private set; }
		public HeroController HeroController { get; private set; }

		public async UniTask<bool> Initialize()
		{
			JoystickInputService joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			WeaponsService weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();

			EnemiesController = new EnemiesController();
			HeroController = new HeroController();

			await HeroController.Initialize(EnemiesController, joystickInputService, weaponsService, _gameFlowService);
			await EnemiesController.Initialize(HeroController, _gameFlowService);

			// The only link between gameplay and the game flow: the hero's death ends the game, and a new round starts from scratch.
			HeroController.OnDied += OnHeroDied;
			_gameFlowService.OnStateChanged += OnGameStateChanged;

			return true;
		}

		public UniTask Reset()
		{
			if (HeroController != null) HeroController.OnDied -= OnHeroDied;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
			return default;
		}

		private void OnHeroDied() => _gameFlowService.EndGame();

		// Back on the start screen: no bee left over and a fresh hero, ready for the next tap.
		private void OnGameStateChanged(GameState state)
		{
			if (state != GameState.Start) return;

			EnemiesController.ClearAllEnemies();
			HeroController.Restart();
		}
	}
}