using System;
using System.Collections.Generic;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.GameFeel
{
	// Plays the game feel sounds as plain 2D one-shots: they are interface-like feedback, not positional.
	// Each clip gets its own source, so a pitch change on one sound never bends another one still playing.
	// A clip plays at most once per frame: a swing hitting three enemies, or orbs arriving together, is one sound, not a louder stack.
	public class FeedbackAudioService : IService
	{
		private readonly Dictionary<AudioClip, AudioSource> _sources = new Dictionary<AudioClip, AudioSource>();
		private readonly Dictionary<AudioClip, int> _playedFrames = new Dictionary<AudioClip, int>();
		private GameObject _host;

		public Type[] GetDependencies() => null;

		public UniTask<bool> Initialize()
		{
			_host = new GameObject("FeedbackAudio") { hideFlags = HideFlags.DontSave };

			// The base scene has no AudioListener, so nothing would be heard. The sounds are 2D, so the listener's position does not matter.
			if (Object.FindObjectOfType<AudioListener>() == null) _host.AddComponent<AudioListener>();

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			if (_host != null) Object.Destroy(_host);
			_host = null;
			_sources.Clear();
			_playedFrames.Clear();
			return UniTask.CompletedTask;
		}

		// A null clip (unassigned or failed to load) plays nothing.
		public void Play(AudioClip clip, float volume, float pitch = 1f)
		{
			if (clip == null || _host == null) return;
			if (_playedFrames.TryGetValue(clip, out int frame) && frame == Time.frameCount) return;

			_playedFrames[clip] = Time.frameCount;
			if (!_sources.TryGetValue(clip, out AudioSource source))
			{
				source = _host.AddComponent<AudioSource>();
				source.playOnAwake = false;
				source.spatialBlend = 0f;
				_sources[clip] = source;
			}

			source.pitch = pitch;
			source.PlayOneShot(clip, volume);
		}

		// Small random pitch, so a sound repeated quickly does not sound mechanical.
		public void PlayVaried(AudioClip clip, float volume) => Play(clip, volume, UnityEngine.Random.Range(0.95f, 1.05f));
	}
}
