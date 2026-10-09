using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.GameFeel
{
	// Currency orbs shared by the whole round: knows where they fly to (the hero), keeps every live orb under one root so a round end
	// reaches them all, and counts the collect chain that raises the blip's pitch. How a drop looks comes from the dead enemy's CurrencyDropConfig.
	public class CurrencyFeedbackService : IService
	{
		private FeedbackAssetsService _assets;
		private FeedbackAudioService _audio;
		private Transform _orbRoot;
		private float _lastCollectTime = float.NegativeInfinity;
		private int _collectChain;

		// Where the orbs fly to (the hero). Set by the hero's view, null while there is nobody to reach.
		public Transform Target { get; set; }

		public Type[] GetDependencies() => new[] { typeof(FeedbackAssetsService), typeof(FeedbackAudioService) };

		public UniTask<bool> Initialize()
		{
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			_audio = ServicesLocator.Instance.GetService<FeedbackAudioService>();
			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			if (_orbRoot != null) Object.Destroy(_orbRoot.gameObject);
			_orbRoot = null;
			Target = null;
			_collectChain = 0;
			return UniTask.CompletedTask;
		}

		// A dead enemy bursts into orbs: they hop out around it, bounce, rest, then CurrencyOrbView calls them to Target.
		// Nothing is dropped without a config, a target or after a failed load: the kill just has no orbs.
		public void Drop(Vector3 position, CurrencyDropConfig config)
		{
			if (config == null || Target == null) return;

			CurrencyOrbView orbPrefab = config.GetOrbPrefab(_assets);
			if (orbPrefab == null) return;

			if (_orbRoot == null) _orbRoot = new GameObject("CurrencyOrbs").transform;

			int count = UnityEngine.Random.Range(config.OrbsPerKill.x, config.OrbsPerKill.y + 1);
			float firstAngle = UnityEngine.Random.value * 360f;
			Vector3 spawnPosition = position + Vector3.up * config.DropHeight;

			for (int i = 0; i < count; i++)
			{
				// Evenly spread around the enemy with a little jitter, so the burst reads as a ring, not a clump.
				float angle = (firstAngle + (i + UnityEngine.Random.Range(-0.2f, 0.2f)) * 360f / count) * Mathf.Deg2Rad;
				float speed = UnityEngine.Random.Range(config.BurstSpeed.x, config.BurstSpeed.y);
				Vector3 velocity = new Vector3(Mathf.Cos(angle) * speed, config.BurstUpSpeed * UnityEngine.Random.Range(0.8f, 1.2f), Mathf.Sin(angle) * speed);

				CurrencyOrbView orb = Object.Instantiate(orbPrefab, spawnPosition, Quaternion.identity, _orbRoot);
				orb.Launch(this, config, velocity, position.y);
			}

			_audio.PlayVaried(_assets.Get(config.DropClip), config.DropVolume);
		}

		// An orb reached the hero: sparkle burst at the contact point and a blip that climbs in pitch with each orb of the chain.
		public void Collect(Vector3 position, CurrencyDropConfig config)
		{
			_assets.Spawn(config.CollectPrefab, position);

			float now = Time.unscaledTime;
			_collectChain = now - _lastCollectTime <= config.ChainWindow ? _collectChain + 1 : 0;
			_lastCollectTime = now;

			float pitch = Mathf.Min(config.MaxPitch, 1f + _collectChain * config.ChainPitchStep);
			_audio.Play(_assets.Get(config.CollectClip), config.CollectVolume, pitch);
		}

		// The hero died: orbs still on the floor or in flight shrink away instead of waiting for a hero who is gone.
		public void Clear()
		{
			if (_orbRoot == null) return;

			foreach (CurrencyOrbView orb in _orbRoot.GetComponentsInChildren<CurrencyOrbView>()) orb.Vanish();
		}
	}
}
