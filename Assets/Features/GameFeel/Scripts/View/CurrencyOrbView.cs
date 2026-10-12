using UnityEngine;

namespace Game.GameFeel
{
	// One currency orb: pops out of a dead enemy, bounces, rests for a moment, winds up, is pulled to the hero and collected.
	// Pure presentation: it holds no amount and no rule. CurrencyFeedbackService spawns it with the dead enemy's CurrencyDropConfig, tells it where the hero is and plays the collect feedback.
	public class CurrencyOrbView : MonoBehaviour
	{
		private enum Phase { Burst, Rest, Windup, Magnet, Vanish }

		[SerializeField, Tooltip("Billboard root: faces the camera, gets the pop, squash and bob")]
		private Transform visual;
		[SerializeField, Tooltip("Soft halo behind the orb, pulses while resting and swells when the orb is called")]
		private Transform glow;
		[SerializeField, Tooltip("Flat blob on the floor, shrinks while the orb is in the air")]
		private Transform shadow;
		[SerializeField, Tooltip("Streak left behind while the orb flies to the hero")]
		private TrailRenderer trail;

		private CurrencyFeedbackService _service;
		private CurrencyDropConfig _config;
		private Camera _camera;
		private Phase _phase;
		private Vector3 _velocity;
		private Vector3 _windupFrom;
		private Vector3 _windupTo;
		private Vector3 _glowBaseScale;
		private Vector3 _shadowBaseScale;
		private float _floorY;
		private float _spawnTime;
		private float _attractTime;
		private float _phaseStart;
		private float _landTime = float.NegativeInfinity;
		private float _bobOffset;
		private float _curveSide;
		private float _speed;
		private float _phaseScale = 1f; // body size set by the current phase
		private float _glowBoost = 1f;

		private void Awake()
		{
			_glowBaseScale = glow.localScale;
			_shadowBaseScale = shadow.localScale;
			trail.emitting = false;
		}

		// Called right after Instantiate. config is the dead enemy's drop, velocity the burst, floorY the ground the orb will bounce on.
		public void Launch(CurrencyFeedbackService service, CurrencyDropConfig config, Vector3 velocity, float floorY)
		{
			_service = service;
			_config = config;
			_velocity = velocity;
			_floorY = floorY;
			_spawnTime = Time.time;
			_attractTime = Time.time + _config.AttractDelay + Random.value * _config.AttractDelayJitter;
			_bobOffset = Random.value * Mathf.PI * 2f;
			_curveSide = Random.value < 0.5f ? -1f : 1f;
			SetPhase(Phase.Burst);
			UpdateVisual();
		}

		// The hero died or the round ended: the orb shrinks away instead of being collected.
		public void Vanish()
		{
			if (_phase == Phase.Vanish) return;

			trail.emitting = false;
			SetPhase(Phase.Vanish);
		}

		private void Update()
		{
			if (_service == null) return;

			switch (_phase)
			{
				case Phase.Burst: UpdateBurst(); break;
				case Phase.Rest: UpdateRest(); break;
				case Phase.Windup: UpdateWindup(); break;
				case Phase.Magnet: UpdateMagnet(); break;
				case Phase.Vanish: UpdateVanish(); break;
			}

			UpdateVisual();
		}

		// Billboard in LateUpdate so it follows the camera that Cinemachine moved this frame.
		private void LateUpdate()
		{
			if (_camera == null) _camera = Camera.main;
			if (_camera != null) visual.rotation = _camera.transform.rotation;
		}

		private void UpdateBurst()
		{
			float restY = _floorY + _config.RestHeight;

			_velocity.y -= _config.Gravity * Time.deltaTime;
			Vector3 position = transform.position + _velocity * Time.deltaTime;

			if (position.y <= restY && _velocity.y < 0f)
			{
				position.y = restY;
				_landTime = Time.time;

				if (-_velocity.y < _config.MinBounceSpeed)
				{
					transform.position = position;
					SetPhase(Phase.Rest);
					return;
				}

				_velocity.y = -_velocity.y * _config.Bounciness;
				_velocity.x *= _config.BounceFriction;
				_velocity.z *= _config.BounceFriction;
			}

			transform.position = position;
		}

		// Waits on the floor for its turn, with a slow bob, until the hero is there to be reached.
		private void UpdateRest()
		{
			if (Time.time < _attractTime || _service.Target == null) return;

			StartWindup();
		}

		// The orb does not leave straight away: it squashes and recoils from the hero first, like a spring being loaded.
		private void StartWindup()
		{
			Vector3 away = -FlatDirectionToTarget();
			_windupFrom = transform.position;
			_windupTo = _windupFrom + away * _config.WindupDistance + Vector3.up * (_config.WindupDistance * 0.5f);
			SetPhase(Phase.Windup);
		}

		private void UpdateWindup()
		{
			float progress = Mathf.Clamp01((Time.time - _phaseStart) / Mathf.Max(0.0001f, _config.WindupDuration));
			float eased = 1f - (1f - progress) * (1f - progress);

			transform.position = Vector3.Lerp(_windupFrom, _windupTo, eased);
			_phaseScale = Mathf.Lerp(1f, _config.WindupSquash, eased);
			_glowBoost = Mathf.Lerp(1f, 1.8f, eased);

			if (progress >= 1f) StartMagnet();
		}

		// Leaves with the recoil speed plus a sideways kick, so the path curves into the hero instead of being a straight line.
		private void StartMagnet()
		{
			Vector3 away = (_windupTo - _windupFrom);
			away.y = 0f;
			away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.back;
			Vector3 side = Vector3.Cross(Vector3.up, away) * _curveSide;

			_velocity = away * (_config.AttractCurve * 0.5f) + side * _config.AttractCurve;
			_speed = 0f;
			trail.Clear();
			trail.emitting = true;
			SetPhase(Phase.Magnet);
		}

		private void UpdateMagnet()
		{
			if (_service.Target == null)
			{
				Vanish();
				return;
			}

			Vector3 toTarget = TargetPoint() - transform.position;
			float distance = toTarget.magnitude;

			_speed = Mathf.Min(_config.MaxSpeed, _speed + _config.AttractAcceleration * Time.deltaTime);
			Vector3 desired = distance > 0.0001f ? toTarget / distance * _speed : Vector3.zero;
			_velocity = Vector3.Lerp(_velocity, desired, 1f - Mathf.Exp(-_config.SteerRate * Time.deltaTime));

			if (distance <= _config.CollectRadius || _velocity.magnitude * Time.deltaTime >= distance)
			{
				Collect();
				return;
			}

			transform.position += _velocity * Time.deltaTime;

			float grow = Mathf.Lerp(_config.WindupSquash, _config.MagnetScale, Mathf.Clamp01((Time.time - _phaseStart) / _config.MagnetGrowDuration));
			_phaseScale = grow * Mathf.Clamp01(distance / _config.ArrivalShrinkDistance);
			_glowBoost = 1.4f;
		}

		private void UpdateVanish()
		{
			float progress = Mathf.Clamp01((Time.time - _phaseStart) / _config.VanishDuration);
			_phaseScale = 1f - progress;

			if (progress >= 1f) Destroy(gameObject);
		}

		private void Collect()
		{
			_service.Collect(transform.position, _config);
			Destroy(gameObject);
		}

		private void SetPhase(Phase phase)
		{
			_phase = phase;
			_phaseStart = Time.time;
			_phaseScale = 1f;
			_glowBoost = 1f;
		}

		private Vector3 TargetPoint()
		{
			return _service.Target.position + Vector3.up * _config.TargetHeight;
		}

		private Vector3 FlatDirectionToTarget()
		{
			Vector3 direction = TargetPoint() - transform.position;
			direction.y = 0f;
			return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
		}

		// Everything seen on screen: pop-in, landing squash, bob, glow pulse and the ground shadow.
		private void UpdateVisual()
		{
			float time = Time.time;

			float pop = Easing.OutBack(Mathf.Clamp01((time - _spawnTime) / _config.PopDuration), _config.PopOvershoot);
			float landing = 1f - Mathf.Clamp01((time - _landTime) / _config.LandSquashDuration);
			float squash = _config.LandSquash * landing * landing;
			float bob = _phase == Phase.Rest ? Mathf.Sin(time * _config.BobSpeed + _bobOffset) * _config.BobHeight : 0f;

			visual.localPosition = Vector3.up * bob;
			visual.localScale = new Vector3(1f + squash, 1f - squash, 1f) * (_phaseScale * pop);

			float pulse = _phase == Phase.Rest ? 1f + Mathf.Sin(time * _config.BobSpeed * 1.5f + _bobOffset) * 0.15f : 1f;
			glow.localScale = _glowBaseScale * (_glowBoost * pulse * pop);

			// The shadow stays on the floor under the orb and shrinks with the height.
			float height = Mathf.Max(0f, transform.position.y - (_floorY + _config.RestHeight));
			shadow.position = new Vector3(transform.position.x, _floorY + 0.03f, transform.position.z);
			shadow.localScale = _shadowBaseScale * (Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(height / 3f)) * pop * _phaseScale);
		}
	}
}
