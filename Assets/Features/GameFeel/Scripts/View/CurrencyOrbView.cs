using UnityEngine;

namespace Game.GameFeel
{
	// One currency orb: pops out of a dead enemy, bounces, rests for a moment, winds up, is pulled to the hero and collected.
	// Pure presentation: it holds no amount and no rule. GameFeelService spawns it, tells it where the hero is and plays the collect feedback.
	public class CurrencyOrbView : MonoBehaviour
	{
		private enum Phase { Burst, Rest, Windup, Magnet, Vanish }

		// Implementation details, not designer values.
		private const float PopDuration = 0.2f; // spawn pop
		private const float PopOvershoot = 2.5f;
		private const float LandSquash = 0.3f;
		private const float LandSquashDuration = 0.15f;
		private const float MinBounceSpeed = 2f; // landing slower than this ends the bounce
		private const float BounceFriction = 0.7f; // horizontal speed kept at each bounce
		private const float BobHeight = 0.08f;
		private const float BobSpeed = 4f;
		private const float WindupSquash = 0.7f; // body size at the end of the windup
		private const float MagnetGrowDuration = 0.15f;
		private const float MagnetScale = 1.25f;
		private const float ArrivalShrinkDistance = 1.5f; // the orb shrinks as it gets sucked into the hero
		private const float SteerRate = 10f; // how fast the velocity turns toward the hero
		private const float VanishDuration = 0.15f;

		[SerializeField, Tooltip("Billboard root: faces the camera, gets the pop, squash and bob")]
		private Transform visual;
		[SerializeField, Tooltip("Soft halo behind the orb, pulses while resting and swells when the orb is called")]
		private Transform glow;
		[SerializeField, Tooltip("Flat blob on the floor, shrinks while the orb is in the air")]
		private Transform shadow;
		[SerializeField, Tooltip("Streak left behind while the orb flies to the hero")]
		private TrailRenderer trail;

		private GameFeelService _service;
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

		// Called right after Instantiate. velocity is the burst, floorY the ground the orb will bounce on.
		public void Launch(GameFeelService service, Vector3 velocity, float floorY)
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			_service = service;
			_velocity = velocity;
			_floorY = floorY;
			_spawnTime = Time.time;
			_attractTime = Time.time + config.CurrencyAttractDelay + Random.value * config.CurrencyAttractDelayJitter;
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
			GameFeelConfig config = GameFeelConfig.Instance;
			float restY = _floorY + config.CurrencyRestHeight;

			_velocity.y -= config.CurrencyGravity * Time.deltaTime;
			Vector3 position = transform.position + _velocity * Time.deltaTime;

			if (position.y <= restY && _velocity.y < 0f)
			{
				position.y = restY;
				_landTime = Time.time;

				if (-_velocity.y < MinBounceSpeed)
				{
					transform.position = position;
					SetPhase(Phase.Rest);
					return;
				}

				_velocity.y = -_velocity.y * config.CurrencyBounciness;
				_velocity.x *= BounceFriction;
				_velocity.z *= BounceFriction;
			}

			transform.position = position;
		}

		// Waits on the floor for its turn, with a slow bob, until the hero is there to be reached.
		private void UpdateRest()
		{
			if (Time.time < _attractTime || _service.CurrencyTarget == null) return;

			StartWindup();
		}

		// The orb does not leave straight away: it squashes and recoils from the hero first, like a spring being loaded.
		private void StartWindup()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			Vector3 away = -FlatDirectionToTarget();
			_windupFrom = transform.position;
			_windupTo = _windupFrom + away * config.CurrencyWindupDistance + Vector3.up * (config.CurrencyWindupDistance * 0.5f);
			SetPhase(Phase.Windup);
		}

		private void UpdateWindup()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			float progress = Mathf.Clamp01((Time.time - _phaseStart) / Mathf.Max(0.0001f, config.CurrencyWindupDuration));
			float eased = 1f - (1f - progress) * (1f - progress);

			transform.position = Vector3.Lerp(_windupFrom, _windupTo, eased);
			_phaseScale = Mathf.Lerp(1f, WindupSquash, eased);
			_glowBoost = Mathf.Lerp(1f, 1.8f, eased);

			if (progress >= 1f) StartMagnet();
		}

		// Leaves with the recoil speed plus a sideways kick, so the path curves into the hero instead of being a straight line.
		private void StartMagnet()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			Vector3 away = (_windupTo - _windupFrom);
			away.y = 0f;
			away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.back;
			Vector3 side = Vector3.Cross(Vector3.up, away) * _curveSide;

			_velocity = away * (config.CurrencyAttractCurve * 0.5f) + side * config.CurrencyAttractCurve;
			_speed = 0f;
			trail.Clear();
			trail.emitting = true;
			SetPhase(Phase.Magnet);
		}

		private void UpdateMagnet()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			if (_service.CurrencyTarget == null)
			{
				Vanish();
				return;
			}

			Vector3 toTarget = TargetPoint() - transform.position;
			float distance = toTarget.magnitude;

			_speed = Mathf.Min(config.CurrencyMaxSpeed, _speed + config.CurrencyAttractAcceleration * Time.deltaTime);
			Vector3 desired = distance > 0.0001f ? toTarget / distance * _speed : Vector3.zero;
			_velocity = Vector3.Lerp(_velocity, desired, 1f - Mathf.Exp(-SteerRate * Time.deltaTime));

			if (distance <= config.CurrencyCollectRadius || _velocity.magnitude * Time.deltaTime >= distance)
			{
				Collect();
				return;
			}

			transform.position += _velocity * Time.deltaTime;

			float grow = Mathf.Lerp(WindupSquash, MagnetScale, Mathf.Clamp01((Time.time - _phaseStart) / MagnetGrowDuration));
			_phaseScale = grow * Mathf.Clamp01(distance / ArrivalShrinkDistance);
			_glowBoost = 1.4f;
		}

		private void UpdateVanish()
		{
			float progress = Mathf.Clamp01((Time.time - _phaseStart) / VanishDuration);
			_phaseScale = 1f - progress;

			if (progress >= 1f) Destroy(gameObject);
		}

		private void Collect()
		{
			_service.PlayCurrencyCollected(transform.position);
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
			return _service.CurrencyTarget.position + Vector3.up * GameFeelConfig.Instance.CurrencyTargetHeight;
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
			GameFeelConfig config = GameFeelConfig.Instance;
			float time = Time.time;

			float pop = EaseOutBack(Mathf.Clamp01((time - _spawnTime) / PopDuration), PopOvershoot);
			float landing = 1f - Mathf.Clamp01((time - _landTime) / LandSquashDuration);
			float squash = LandSquash * landing * landing;
			float bob = _phase == Phase.Rest ? Mathf.Sin(time * BobSpeed + _bobOffset) * BobHeight : 0f;

			visual.localPosition = Vector3.up * bob;
			visual.localScale = new Vector3(1f + squash, 1f - squash, 1f) * (_phaseScale * pop);

			float pulse = _phase == Phase.Rest ? 1f + Mathf.Sin(time * BobSpeed * 1.5f + _bobOffset) * 0.15f : 1f;
			glow.localScale = _glowBaseScale * (_glowBoost * pulse * pop);

			// The shadow stays on the floor under the orb and shrinks with the height.
			float height = Mathf.Max(0f, transform.position.y - (_floorY + config.CurrencyRestHeight));
			shadow.position = new Vector3(transform.position.x, _floorY + 0.03f, transform.position.z);
			shadow.localScale = _shadowBaseScale * (Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(height / 3f)) * pop * _phaseScale);
		}

		// 0 -> 1 with a swing past 1 that settles back (same curve as the enemy spawn pop).
		private static float EaseOutBack(float t, float overshoot)
		{
			float u = t - 1f;
			return 1f + u * u * ((overshoot + 1f) * u + overshoot);
		}
	}
}
