using Core.ServicesManager;
using UnityEngine;

namespace Game.GameFeel
{
	// Warns the player that the entity it sits on is about to attack: the body blinks red and a red disc on the ground grows to the attack range,
	// so the player sees both that a hit is coming and which area it covers.
	// The enemy's own view calls Begin() when the windup starts and End() when it is over, so no gameplay rule lives here.
	public class AttackTelegraphView : MonoBehaviour
	{
		private Renderer[] _renderers;
		private Material[][] _normalMaterials;
		private Material[][] _warnMaterials; // built on the first blink
		private GameObject _warning; // created on the first windup, then reused
		private float _beginTime;
		private float _duration;
		private float _diameter;
		private bool _tinted;

		private void Awake()
		{
			_renderers = GetComponentsInChildren<Renderer>();
			_normalMaterials = new Material[_renderers.Length][];
			for (int i = 0; i < _renderers.Length; i++)
			{
				_normalMaterials[i] = _renderers[i].sharedMaterials;
			}

			enabled = false; // only updates during a windup
		}

		// range: the radius of the area the attack covers, in world units.
		public void Begin(float duration, float range)
		{
			GameFeelService service = ServicesLocator.Instance.GetService<GameFeelService>();
			if (service == null || duration <= 0f) return;

			_beginTime = Time.time;
			_duration = duration;
			_diameter = range * 2f;

			if (_warning == null) _warning = service.CreateAttackWarning(transform);
			if (_warning != null) _warning.SetActive(true);

			enabled = true;
			Update();
		}

		// Safe to call when nothing is playing (the enemy died or the windup ended).
		public void End()
		{
			enabled = false;
			SetTint(null);
			if (_warning != null) _warning.SetActive(false);
		}

		private void Update()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			float elapsed = Time.time - _beginTime;
			float progress = Mathf.Clamp01(elapsed / _duration);

			if (_warning != null)
			{
				// Eased out: the disc opens quickly then slowly closes in on the full range, like the hit getting closer.
				float eased = 1f - (1f - progress) * (1f - progress);
				float size = _diameter * Mathf.Lerp(config.AttackWarnStartScale, 1f, eased);
				_warning.transform.localScale = Vector3.one * (size / transform.lossyScale.x);
			}

			GameFeelService service = ServicesLocator.Instance.GetService<GameFeelService>();
			bool tinted = service != null && Mathf.Repeat(elapsed * config.AttackWarnBlinkRate, 1f) < 0.5f;
			SetTint(tinted ? service.AttackWarnMaterial : null);
		}

		// null restores the body's own materials.
		private void SetTint(Material warnMaterial)
		{
			bool tint = warnMaterial != null;
			if (tint == _tinted) return;

			if (tint && _warnMaterials == null)
			{
				_warnMaterials = new Material[_renderers.Length][];
				for (int i = 0; i < _renderers.Length; i++)
				{
					_warnMaterials[i] = new Material[_normalMaterials[i].Length];
					System.Array.Fill(_warnMaterials[i], warnMaterial);
				}
			}

			for (int i = 0; i < _renderers.Length; i++)
			{
				_renderers[i].sharedMaterials = tint ? _warnMaterials[i] : _normalMaterials[i];
			}
			_tinted = tint;
		}
	}
}
