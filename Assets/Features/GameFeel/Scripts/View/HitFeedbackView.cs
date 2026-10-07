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
		private Vector3 _recoilDirection;
		private Vector3 _recoilApplied; // offset currently added to the model, so it can be removed exactly

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

		// damage 0 means no number, no freeze frame (the hero's hits). The killing blow gets a bigger, hotter number and, when kills are chained, a freeze.
		// awayDirection (from the attacker to this body) shoves the model back for a moment. Zero or a killing blow means no shove:
		// the death launch takes over.
		public void Play(int damage = 0, bool isKillingBlow = false, Vector3 awayDirection = default)
		{
			GameFeelService service = ServicesLocator.Instance.GetService<GameFeelService>();
			if (service == null) return;

			_hitTime = Time.time;
			enabled = true;

			Vector3 hitPoint = _renderers.Length > 0 ? _renderers[0].bounds.center : transform.position;
			if (service.FlashMaterial != null) SetFlash(service.FlashMaterial);
			service.PlayHitSpark(hitPoint);
			if (damage > 0)
			{
				service.PlayDamageNumber(hitPoint, damage, isKillingBlow);
				service.PlayHitStop(isKillingBlow);
			}

			ClearRecoil();
			awayDirection.y = 0f;
			if (!isKillingBlow && awayDirection.sqrMagnitude > 0.0001f) _recoilDirection = awayDirection.normalized;
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

			// Shoved instantly, then eases back. Applied as a delta so it adds to whatever else moves the model.
			float recoil = 1f - Mathf.Clamp01(sinceHit / Mathf.Max(0.0001f, config.RecoilDuration));
			Vector3 recoilOffset = _recoilDirection * (config.RecoilDistance * recoil * recoil);
			visual.position += recoilOffset - _recoilApplied;
			_recoilApplied = recoilOffset;

			if (!_flashing && punch <= 0f && recoil <= 0f) enabled = false;
		}

		// Takes back the shove, before a new hit or the death launch (which starts from the model's current position).
		private void ClearRecoil()
		{
			visual.position -= _recoilApplied;
			_recoilApplied = Vector3.zero;
			_recoilDirection = Vector3.zero;
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
