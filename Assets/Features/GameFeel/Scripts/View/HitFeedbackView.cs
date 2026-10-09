using Core.ServicesManager;
using UnityEngine;

namespace Game.GameFeel
{
	// Hit reaction of the entity it sits on: white flash, squash, shove, a spark burst and, when it has a DamageNumberConfig, a floating number.
	// The entity's own view binds its configs and calls Play() when it takes a hit, so no gameplay rule lives here.
	// The blow's own feel (freeze frame, impact sound) belongs to the weapon that struck: see WeaponFeedbackView.
	public class HitFeedbackView : MonoBehaviour
	{
		private const string CritNumberSuffix = "!";

		[SerializeField, Tooltip("Animated model root that gets squashed")]
		private Transform visual;

		private HitFeedbackConfig _config;
		private DamageNumberConfig _numbers;
		private DamageNumber _numberPrefab;
		private FeedbackAssetsService _assets;
		private Material _flashMaterial;
		private Renderer[] _renderers;
		private Material[][] _normalMaterials;
		private Material[][] _flashMaterials; // built on the first flash
		private Vector3 _baseScale;
		private float _hitTime;
		private bool _flashing;
		private Vector3 _recoilDirection;
		private Vector3 _recoilApplied; // offset currently added to the model, so it can be removed exactly
		private float _impactScale = 1f; // squash and shove strength of the current hit: bigger on a critical strike

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

		// Called by the entity's view once the services are ready. A null config disables the reaction, a null numbers config shows no number (the hero).
		public void Bind(HitFeedbackConfig config, DamageNumberConfig numbers = null)
		{
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			_config = config;
			_numbers = numbers;
			_flashMaterial = config != null ? _assets.Get(config.FlashMaterial) : null;
			_numberPrefab = numbers != null ? numbers.GetPrefab(_assets) : null;
		}

		// damage 0 means no number. The killing blow gets a bigger, hotter number.
		// awayDirection (from the attacker to this body) shoves the model back for a moment. Zero or a killing blow means no shove:
		// the death launch takes over. A critical strike makes the spark, squash, shove and number bigger.
		public void Play(int damage = 0, bool isKillingBlow = false, Vector3 awayDirection = default, bool isCritical = false)
		{
			if (_config == null) return;

			_hitTime = Time.time;
			_impactScale = isCritical ? _config.CritImpactScale : 1f;
			enabled = true;

			Vector3 hitPoint = _renderers.Length > 0 ? _renderers[0].bounds.center : transform.position;
			if (_flashMaterial != null) SetFlash();

			GameObject spark = _assets.Spawn(_config.SparkPrefab, hitPoint);
			if (spark != null && isCritical) spark.transform.localScale *= _config.CritSparkScale;

			if (damage > 0) PlayDamageNumber(hitPoint, damage, isKillingBlow, isCritical);

			ClearRecoil();
			awayDirection.y = 0f;
			if (!isKillingBlow && awayDirection.sqrMagnitude > 0.0001f) _recoilDirection = awayDirection.normalized;
		}

		// Cuts the reaction in progress and puts the model back as the prefab had it (a pooled enemy coming back to life).
		public void Stop()
		{
			if (_flashing) ClearFlash();
			ClearRecoil();
			visual.localScale = _baseScale;
			enabled = false;
		}

		// LateUpdate so the Animator can never overwrite the squash.
		private void LateUpdate()
		{
			float sinceHit = Time.time - _hitTime;

			if (_flashing && sinceHit >= _config.FlashDuration) ClearFlash();

			float punch = 1f - Mathf.Clamp01(sinceHit / Mathf.Max(0.0001f, _config.PunchDuration));
			float squash = _config.PunchSquash * _impactScale * punch * punch;
			visual.localScale = Vector3.Scale(_baseScale, new Vector3(1f + squash, 1f - squash, 1f + squash));

			// Shoved instantly, then eases back. Applied as a delta so it adds to whatever else moves the model.
			float recoil = 1f - Mathf.Clamp01(sinceHit / Mathf.Max(0.0001f, _config.RecoilDuration));
			Vector3 recoilOffset = _recoilDirection * (_config.RecoilDistance * _impactScale * recoil * recoil);
			visual.position += recoilOffset - _recoilApplied;
			_recoilApplied = recoilOffset;

			if (!_flashing && punch <= 0f && recoil <= 0f) enabled = false;
		}

		// Floating damage number (third-party package). Position, colors and size come from the DamageNumberConfig, the motion and fades from the prefab.
		// A critical strike gets its own colors, a "!" and is bigger (on top of the kill size).
		private void PlayDamageNumber(Vector3 hitPoint, int damage, bool isKillingBlow, bool isCritical)
		{
			if (_numberPrefab == null) return;

			DamageNumber number = _numberPrefab.Spawn(hitPoint + Vector3.up * _numbers.Height, damage);

			// Set on every spawn: pooled numbers keep the look of their previous use.
			Color top = isCritical ? _numbers.CritTopColor : isKillingBlow ? _numbers.KillTopColor : _numbers.HitTopColor;
			Color bottom = isCritical ? _numbers.CritBottomColor : isKillingBlow ? _numbers.KillBottomColor : _numbers.HitBottomColor;
			number.SetGradientColor(top, top, bottom, bottom);

			float scale = _numbers.HitScale;
			if (isKillingBlow) scale *= _numbers.KillScale;
			if (isCritical) scale *= _numbers.CritScale;
			number.SetScale(scale);

			number.enableRightText = isCritical;
			number.rightText = isCritical ? CritNumberSuffix : "";
			number.UpdateText();
		}

		// Takes back the shove, before a new hit or the death launch (which starts from the model's current position).
		private void ClearRecoil()
		{
			visual.position -= _recoilApplied;
			_recoilApplied = Vector3.zero;
			_recoilDirection = Vector3.zero;
		}

		private void SetFlash()
		{
			if (_flashMaterials == null)
			{
				_flashMaterials = new Material[_renderers.Length][];
				for (int i = 0; i < _renderers.Length; i++)
				{
					_flashMaterials[i] = new Material[_normalMaterials[i].Length];
					System.Array.Fill(_flashMaterials[i], _flashMaterial);
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
