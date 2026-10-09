using System;

namespace Game.UI
{
	// Counts kills in a row. Each kill refills the time window, and the combo breaks when the window runs out or when Break is called (the hero got hit).
	// Plain C# with no Unity types: ComboCounterView feeds it the time and reacts to its events.
	public class ComboTracker
	{
		public event Action<int> OnIncreased; // new count, raised by every kill
		public event Action<int, bool> OnBroken; // count the combo reached, true when the window ran out (false when Break was called)

		public int Count { get; private set; }
		public float Fill => _window > 0f ? _remaining / _window : 0f; // 1 right after a kill, 0 when the combo is about to break

		private float _window;
		private float _remaining;

		public void RegisterKill(float window)
		{
			Count++;
			_window = window;
			_remaining = window;
			OnIncreased?.Invoke(Count);
		}

		public void Tick(float deltaTime)
		{
			if (Count == 0) return;

			_remaining -= deltaTime;
			if (_remaining <= 0f) Finish(true);
		}

		public void Break()
		{
			if (Count > 0) Finish(false);
		}

		// Silent: a new round starts from nothing, there is nothing to announce.
		public void Reset()
		{
			Count = 0;
			_remaining = 0f;
		}

		private void Finish(bool expired)
		{
			int finalCount = Count;
			Reset();
			OnBroken?.Invoke(finalCount, expired);
		}
	}
}
