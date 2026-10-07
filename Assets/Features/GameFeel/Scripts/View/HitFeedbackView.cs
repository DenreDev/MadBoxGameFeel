using Core.ServicesManager;
using UnityEngine;

namespace Game.GameFeel
{
	// Hit reaction of the entity it sits on: white flash, squash, a spark burst and, when a damage value is given, a floating number.
	// The entity's own view calls Play() when it takes a hit, so no gameplay rule lives here.
	public class HitFeedbackView : MonoBehaviour
	{
		[SerializeField, Tooltip("Animated model root that gets squashed")]
		private Transform visual;

		private Renderer[] _renderers;
		private Material[][] _normalMaterials;
		private Material[][] _flashMaterials; // built on the first flash
		private Vector3 _baseScale;
		private float _hitTime;
		private bool _flashing;

		private void Awake()
		{
			_renderers = GetComponentsInChildren<Renderer>();
			_normalMaterials = new Material[_renderers.Length][];
			for (int i = 0; i < _renderers.Length; i++)
			{
				_normalMaterials[i] = _renderers[i].sharedMaterials;
			}

			_baseScale = visual.localScale;
			enabled = false; // only updates while a reaction is playing
		}

		// damage 0 means no number (the hero's hits). The killing blow gets a bigger, hotter number.
		public void Play(int damage = 0, bool isKillingBlow = false)
		{
			GameFeelService service = ServicesLocator.Instance.GetService<GameFeelService>();
			if (service == null) return;

			_hitTime = Time.time;
			enabled = true;

			Vector3 hitPoint = _renderers.Length > 0 ? _renderers[0].bounds.center : transform.position;
			if (service.FlashMaterial != null) SetFlash(service.FlashMaterial);
			service.PlayHitSpark(hitPoint);
			if (damage > 0) service.PlayDamageNumber(hitPoint, damage, isKillingBlow);
		}

		// LateUpdate so the Animator can never overwrite the squash.
		private void LateUpdate()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			float sinceHit = Time.time - _hitTime;

			if (_flashing && sinceHit >= config.FlashDuration) ClearFlash();

			float punch = 1f - Mathf.Clamp01(sinceHit / Mathf.Max(0.0001f, config.PunchDuration));
			float squash = config.PunchSquash * punch * punch;
			visual.localScale = Vector3.Scale(_baseScale, new Vector3(1f + squash, 1f - squash, 1f + squash));

			if (!_flashing && punch <= 0f) enabled = false;
		}

		private void SetFlash(Material flashMaterial)
		{
			if (_flashMaterials == null)
			{
				_flashMaterials = new Material[_renderers.Length][];
				for (int i = 0; i < _renderers.Length; i++)
				{
					_flashMaterials[i] = new Material[_normalMaterials[i].Length];
					System.Array.Fill(_flashMaterials[i], flashMaterial);
				}
			}

			for (int i = 0; i < _renderers.Length; i++)
			{
				_renderers[i].sharedMaterials = _flashMaterials[i];
			}
			_flashing = true;
		}

		private void ClearFlash()
		{
			for (int i = 0; i < _renderers.Length; i++)
			{
				_renderers[i].sharedMaterials = _normalMaterials[i];
			}
			_flashing = false;
		}
	}
}
