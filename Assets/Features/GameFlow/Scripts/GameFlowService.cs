using System;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;

namespace Game.GameFlow
{
	// Tells the game whether the player has left the start screen. Gameplay loops wait for it, views react to OnGameStarted.
	public class GameFlowService : IService
	{
		public event Action OnGameStarted;

		public bool IsStarted { get; private set; }

		public Type[] GetDependencies() => null;

		public UniTask<bool> Initialize()
		{
			IsStarted = false;
			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			IsStarted = false;
			return UniTask.CompletedTask;
		}

		public void StartGame()
		{
			if (IsStarted) return;

			IsStarted = true;
			OnGameStarted?.Invoke();
		}

		public UniTask WaitUntilStarted(CancellationToken cancellationToken) =>
			UniTask.WaitUntil(() => IsStarted, cancellationToken: cancellationToken);
	}
}
