using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.GameFeel
{
	// The only owner of Time.timeScale for game feel: freezes the game for a few frames on a big blow, then restores it.
	// It only applies freezes. When to freeze and for how long is decided by the caller from its own config (see HitStopConfig).
	public class HitStopService : IService
	{
		private float _endTime = float.NegativeInfinity; // unscaled time: the freeze must not depend on the time it freezes

		public Type[] GetDependencies() => null;

		public UniTask<bool> Initialize() => UniTask.FromResult(true);

		public UniTask Reset()
		{
			_endTime = float.NegativeInfinity;
			Time.timeScale = 1f;
			return UniTask.CompletedTask;
		}

		// Overlapping freezes do not stack: the one ending last wins. duration is in real seconds, 0 or less does nothing.
		public void Freeze(float duration, float timeScale)
		{
			float now = Time.unscaledTime;
			if (duration <= 0f || now + duration <= _endTime) return;

			bool alreadyFrozen = _endTime > now;
			_endTime = now + duration;
			Time.timeScale = timeScale;
			if (!alreadyFrozen) EndWhenOver().Forget();
		}

		// Frame based wait, so it keeps running while the time scale is near zero.
		private async UniTaskVoid EndWhenOver()
		{
			while (Time.unscaledTime < _endTime) await UniTask.Yield(PlayerLoopTiming.Update);

			Time.timeScale = 1f;
		}
	}
}
