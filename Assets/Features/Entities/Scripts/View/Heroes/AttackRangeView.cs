using Core.ServicesManager;
using Game.GameFeel;
using Game.GameFlow;
using Game.GamePlay.Entities;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	// Shows the cone the hero's sword covers: a faint fan on the ground in front of him that fades out toward his feet, with a thin arc at the exact reach.
	// Anything whose center is inside it when the slash lands is hit, so the player reads what he hits.
	// During the swing windup a brighter fill grows from the hero to the arc and touches it when the damage lands, then the slash makes the cone pulse once and the fill fades.
	// The cone is a child of the hero, so it turns with him. It is rebuilt when the weapon changes, and hidden on the start screen and while he is dead.
	// Only presentation: the hit rule lives in HeroAttack, this reads the same WeaponConfig values. The look is the weapon's SlashAttack.RangeDisplay (none, or another kind of attack = no cone).
	public class AttackRangeView : MonoBehaviour
	{
		private const float GroundOffset = 0.03f; // just above the hero ring (0.02), under the feet effects
		private const float FillOffset = 0.005f; // the charge fill sits just above the cone
		private const float DegreesPerSegment = 6f;
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private GameObject _cone;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;
		private Mesh _mesh;
		private GameObject _fill;
		private MeshFilter _fillMeshFilter;
		private MeshRenderer _fillMeshRenderer;
		private Mesh _fillMesh;
		private MaterialPropertyBlock _block;
		private Color _baseColor;
		private AttackRangeConfig _look; // the current weapon's, null when it has none
		private FeedbackAssetsService _assets;
		private HeroController _heroController;
		private WeaponsService _weaponsService;
		private GameFlowService _gameFlowService;
		private bool _isCharging;
		private float _chargeStartTime;
		private float _chargeDuration;
		private bool _isPulsing;
		private float _pulseStartTime;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
			enabled = false; // only updates during a charge or a pulse
		}

		private void OnServicesInitialized()
		{
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			if (_assets == null) return;

			_block = new MaterialPropertyBlock();

			_cone = CreateLayer("AttackRange", transform, GroundOffset, out _meshFilter, out _meshRenderer);
			_fill = CreateLayer("AttackRangeFill", _cone.transform, FillOffset, out _fillMeshFilter, out _fillMeshRenderer); // child of the cone: it turns, hides and pops with it

			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.Attack.OnAttackStarted += OnHeroAttackStarted;
			_heroController.Attack.OnAttacked += OnHeroAttacked;
			_weaponsService.OnWeaponChanged += OnWeaponChanged;
			_gameFlowService.OnStateChanged += OnGameStateChanged;

			Rebuild(_weaponsService.CurrentWeapon);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_heroController.Attack.OnAttackStarted -= OnHeroAttackStarted;
				_heroController.Attack.OnAttacked -= OnHeroAttacked;
			}
			if (_weaponsService != null) _weaponsService.OnWeaponChanged -= OnWeaponChanged;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
			if (_mesh != null) Destroy(_mesh);
			if (_fillMesh != null) Destroy(_fillMesh);
		}

		private void OnHeroStateChanged(HeroState state) => Refresh();
		private void OnWeaponChanged(WeaponConfig weapon) => Rebuild(weapon);

		// A swing in flight is dropped when the game state changes (game over, restart): its slash will not land.
		private void OnGameStateChanged(GameState state)
		{
			StopAnimations();
			Refresh();
		}

		// The fill grows over the windup, so it reaches the arc when the damage lands (the slash waits HitDelay before it hits).
		private void OnHeroAttackStarted(int targetId)
		{
			SlashAttack slash = _weaponsService.CurrentWeapon != null ? _weaponsService.CurrentWeapon.Attack as SlashAttack : null;
			if (_cone == null || !_cone.activeSelf || _fillMeshFilter.sharedMesh == null || slash == null) return;

			_isCharging = true;
			_chargeStartTime = Time.time;
			_chargeDuration = slash.HitDelay;
			enabled = true;
		}

		private void OnHeroAttacked(Vector3 slashPosition)
		{
			_isCharging = false;
			if (_cone == null || !_cone.activeSelf || _look == null || _look.PulseDuration <= 0f)
			{
				ApplyFill(0f, 0f);
				return;
			}

			_isPulsing = true;
			_pulseStartTime = Time.time;
			enabled = true;
		}

		private void Update()
		{
			if (_isCharging)
			{
				float charge = _chargeDuration > 0f ? Mathf.Clamp01((Time.time - _chargeStartTime) / _chargeDuration) : 1f;
				ApplyFill(_look.ChargeCurve.Evaluate(charge), 1f); // stays full if the slash is late
			}

			if (_isPulsing)
			{
				float progress = Mathf.Clamp01((Time.time - _pulseStartTime) / _look.PulseDuration);
				float strength = (1f - progress) * (1f - progress); // starts strong, settles slowly

				ApplyPulse(1f + (_look.PulseBoost - 1f) * strength, 1f + _look.PulsePop * strength);
				if (!_isCharging) ApplyFill(1f, strength); // a new swing already started takes the fill over
				if (progress >= 1f) _isPulsing = false;
			}

			enabled = _isCharging || _isPulsing;
		}

		private void StopAnimations()
		{
			_isCharging = false;
			_isPulsing = false;
			enabled = false;
			if (_cone == null) return;

			ApplyPulse(1f, 1f);
			ApplyFill(0f, 0f);
		}

		// A property block keeps the shared Addressable material untouched.
		private void ApplyPulse(float alphaMultiplier, float scale)
		{
			SetAlpha(_meshRenderer, alphaMultiplier);
			_cone.transform.localScale = new Vector3(scale, 1f, scale);
		}

		// Scaling the fan from the hero grows its radius, the arc angle stays the same.
		private void ApplyFill(float size, float alphaMultiplier)
		{
			bool visible = size > 0.001f && alphaMultiplier > 0.001f;
			if (_fill.activeSelf != visible) _fill.SetActive(visible);
			if (!visible) return;

			SetAlpha(_fillMeshRenderer, alphaMultiplier);
			_fill.transform.localScale = new Vector3(size, 1f, size);
		}

		private void SetAlpha(MeshRenderer meshRenderer, float alphaMultiplier)
		{
			Color color = _baseColor;
			color.a *= alphaMultiplier;
			_block.SetColor(BaseColorId, color);
			meshRenderer.SetPropertyBlock(_block);
		}

		private void Refresh()
		{
			if (_cone == null) return;

			bool visible = _meshFilter.sharedMesh != null && _gameFlowService.State != GameState.Start && !_heroController.CurrentState.IsDead;
			if (_cone.activeSelf == visible) return;

			_cone.SetActive(visible);
			if (!visible) StopAnimations();
		}

		// No weapon, no look or a material that failed to load: no mesh, so Refresh keeps the cone hidden.
		private void Rebuild(WeaponConfig weapon)
		{
			if (_mesh != null) Destroy(_mesh);
			if (_fillMesh != null) Destroy(_fillMesh);
			_mesh = null;
			_fillMesh = null;
			_meshFilter.sharedMesh = null;
			_fillMeshFilter.sharedMesh = null;

			// Only a slash has a cone to show: any other attack leaves the weapon without one.
			SlashAttack slash = weapon != null ? weapon.Attack as SlashAttack : null;
			_look = slash != null ? slash.RangeDisplay : null;
			Material material = _look != null ? _assets.Get(_look.Material) : null;
			_meshRenderer.sharedMaterial = material;
			_fillMeshRenderer.sharedMaterial = material;

			if (material != null)
			{
				_baseColor = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
				_mesh = BuildCone(weapon.Range, slash.ArcAngle, _look.EdgeWidth, 0f, _look.FillAlpha, _look.EdgeAlpha);
				_meshFilter.sharedMesh = _mesh;

				if (_look.ChargeAlpha > 0f || _look.ChargeEdgeAlpha > 0f)
				{
					// Even strength from the hero to the rim, so it reads as a solid area filling up, with a brighter front.
					_fillMesh = BuildCone(weapon.Range, slash.ArcAngle, _look.EdgeWidth, _look.ChargeAlpha, _look.ChargeAlpha, _look.ChargeEdgeAlpha);
					_fillMeshFilter.sharedMesh = _fillMesh;
				}
			}

			StopAnimations();
			Refresh();
		}

		private static GameObject CreateLayer(string name, Transform parent, float height, out MeshFilter meshFilter, out MeshRenderer meshRenderer)
		{
			GameObject layer = new GameObject(name);
			layer.transform.SetParent(parent, false);
			layer.transform.localPosition = new Vector3(0f, height, 0f);
			meshFilter = layer.AddComponent<MeshFilter>();
			meshRenderer = layer.AddComponent<MeshRenderer>();
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			return layer;
		}

		// Flat fan centered on the model's front (the root's -Z). The alpha lives in the vertex colors: centerAlpha at the hero, fillAlpha at the arc, then a thin band at edgeAlpha.
		// Per step along the arc: the fan's rim, the band's inner side, the band's outer side.
		private static Mesh BuildCone(float range, float arcDegrees, float edgeWidth, float centerAlpha, float fillAlpha, float edgeAlpha)
		{
			int segments = Mathf.Max(2, Mathf.CeilToInt(arcDegrees / DegreesPerSegment));
			float halfArc = arcDegrees * 0.5f * Mathf.Deg2Rad;
			float innerRadius = Mathf.Max(0.01f, range - Mathf.Min(edgeWidth, range * 0.5f));

			Vector3[] vertices = new Vector3[1 + (segments + 1) * 3];
			Color[] colors = new Color[vertices.Length];
			int[] triangles = new int[segments * 9]; // per segment: one fan triangle and the band's two (the material does not cull, so the winding does not matter)

			vertices[0] = Vector3.zero;
			colors[0] = new Color(1f, 1f, 1f, centerAlpha);

			for (int i = 0; i <= segments; i++)
			{
				float angle = Mathf.Lerp(-halfArc, halfArc, (float)i / segments);
				Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, -Mathf.Cos(angle));
				int v = 1 + i * 3;

				vertices[v] = direction * innerRadius;
				colors[v] = new Color(1f, 1f, 1f, fillAlpha);
				vertices[v + 1] = direction * innerRadius;
				colors[v + 1] = new Color(1f, 1f, 1f, edgeAlpha);
				vertices[v + 2] = direction * range;
				colors[v + 2] = new Color(1f, 1f, 1f, edgeAlpha);
			}

			int t = 0;
			for (int i = 0; i < segments; i++)
			{
				int a = 1 + i * 3;
				int b = a + 3;

				t = AddTriangle(triangles, t, 0, a, b);
				t = AddTriangle(triangles, t, a + 1, a + 2, b + 2);
				t = AddTriangle(triangles, t, a + 1, b + 2, b + 1);
			}

			Mesh mesh = new Mesh { name = "AttackRangeCone", hideFlags = HideFlags.DontSave };
			mesh.vertices = vertices;
			mesh.colors = colors;
			mesh.triangles = triangles;
			mesh.RecalculateBounds();
			return mesh;
		}

		private static int AddTriangle(int[] triangles, int index, int a, int b, int c)
		{
			triangles[index++] = a;
			triangles[index++] = b;
			triangles[index++] = c;
			return index;
		}
	}
}
