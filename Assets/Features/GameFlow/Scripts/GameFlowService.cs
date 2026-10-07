using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;

namespace Game.GameFlow
{
	public enum GameState
	{
		Start, // non-gameplay: nothing moves, the player waits to tap
		Playing, // gameplay: hero and enemies tick
		GameOver // non-gameplay: the hero died, the player waits for the restart button
	}

	// The game manager. It owns which phase the game is in and nothing else: it knows nothing about heroes, enemies or views.
	//   Start --StartGame--> Playing --EndGame--> GameOver --Restart--> Start
	// Gameplay loops only tick while Playing, views react to OnStateChanged, and EntitiesService resets the round when the flow returns to Start.
	public class GameFlowService : IService
	{
		public event Action<GameState> OnStateChanged;

		public GameState State { get; private set; } = GameState.Start;
		public bool IsPlaying => State == GameState.Playing;

		public Type[] GetDependencies() => null;

		public UniTask<bool> Initialize()
		{
			State = GameState.Start;
			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			State = GameState.Start;
			return UniTask.CompletedTask;
		}

		// Tap on the start screen.
		public void StartGame() => Transition(GameState.Start, GameState.Playing);

		// The hero died.
		public void EndGame() => Transition(GameState.Playing, GameState.GameOver);

		// Restart button: back to the start screen, not straight into gameplay.
		public void Restart() => Transition(GameState.GameOver, GameState.Start);

		// A call from the wrong state is ignored, so a double tap or a late event can never skip a phase.
		private void Transition(GameState from, GameState to)
		{
			if (State != from) return;

			State = to;
			OnStateChanged?.Invoke(State);
		}
	}
}
