using Core.ServicesManager;
using Game.GameFeel;
using Game.GameFlow;
using Game.GamePlay.Entities;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	// Shows the cone the hero's sword covers: a faint fan on the ground in front of him that fades out toward his feet, with a thin arc at the exact reach.
	// Anything whose center is inside it when the slash lands is hit, so the player reads what he hits. The slash landing makes it pulse once.
	// The cone is a child of the hero, so it turns with him. It is rebuilt when the weapon changes, and hidden on the start screen and while he is dead.
	// Only presentation: the hit rule lives in HeroController, this reads the same WeaponConfig values.
	public class AttackRangeView : MonoBehaviour
	{
		private const float GroundOffset = 0.03f; // just above the hero ring (0.02), under the feet effects
		private const float DegreesPerSegment = 6f;
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		private GameObject _cone;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;
		private Mesh _mesh;
		private MaterialPropertyBlock _block;
		private Color _baseColor;
		private HeroController _heroController;
		private WeaponsService _weaponsService;
		private GameFlowService _gameFlowService;
		private float _pulseStartTime;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
			enabled = false; // only updates during a pulse
		}

		private void OnServicesInitialized()
		{
			GameFeelService gameFeelService = ServicesLocator.Instance.GetService<GameFeelService>();
			if (gameFeelService == null || gameFeelService.AttackRangeMaterial == null) return;

			Material material = gameFeelService.AttackRangeMaterial;
			_baseColor = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
			_block = new MaterialPropertyBlock();

			_cone = new GameObject("AttackRange");
			_cone.transform.SetParent(transform, false);
			_cone.transform.localPosition = new Vector3(0f, GroundOffset, 0f);
			_meshFilter = _cone.AddComponent<MeshFilter>();
			_meshRenderer = _cone.AddComponent<MeshRenderer>();
			_meshRenderer.sharedMaterial = material;
			_meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			_meshRenderer.receiveShadows = false;

			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.OnAttacked += OnHeroAttacked;
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
				_heroController.OnAttacked -= OnHeroAttacked;
			}
			if (_weaponsService != null) _weaponsService.OnWeaponChanged -= OnWeaponChanged;
			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
			if (_mesh != null) Destroy(_mesh);
		}

		private void OnHeroStateChanged(HeroState state) => Refresh();
		private void OnGameStateChanged(GameState state) => Refresh();
		private void OnWeaponChanged(WeaponConfig weapon) => Rebuild(weapon);

		private void OnHeroAttacked(Vector3 slashPosition)
		{
			if (_cone == null || !_cone.activeSelf || GameFeelConfig.Instance.AttackRangePulseDuration <= 0f) return;

			_pulseStartTime = Time.time;
			enabled = true;
		}

		private void Update()
		{
			GameFeelConfig config = GameFeelConfig.Instance;
			float progress = Mathf.Clamp01((Time.time - _pulseStartTime) / config.AttackRangePulseDuration);
			float strength = (1f - progress) * (1f - progress); // starts strong, settles slowly

			ApplyPulse(1f + (config.AttackRangePulseBoost - 1f) * strength, 1f + config.AttackRangePulsePop * strength);
			if (progress >= 1f) enabled = false;
		}

		// A property block keeps the shared Addressable material untouched.
		private void ApplyPulse(float alphaMultiplier, float scale)
		{
			Color color = _baseColor;
			color.a *= alphaMultiplier;
			_block.SetColor(BaseColorId, color);
			_meshRenderer.SetPropertyBlock(_block);
			_cone.transform.localScale = new Vector3(scale, 1f, scale);
		}

		private void Refresh()
		{
			if (_cone == null) return;

			bool visible = _meshFilter.sharedMesh != null && _gameFlowService.State != GameState.Start && !_heroController.CurrentState.IsDead;
			if (_cone.activeSelf != visible) _cone.SetActive(visible);
		}

		private void Rebuild(WeaponConfig weapon)
		{
			if (_mesh != null) Destroy(_mesh);
			_mesh = null;
			_meshFilter.sharedMesh = null;

			if (weapon != null)
			{
				GameFeelConfig config = GameFeelConfig.Instance;
				_mesh = BuildCone(weapon.Range, weapon.ArcAngle, config.AttackRangeEdgeWidth, config.AttackRangeFillAlpha, config.AttackRangeEdgeAlpha);
				_meshFilter.sharedMesh = _mesh;
			}

			enabled = false;
			ApplyPulse(1f, 1f);
			Refresh();
		}

		// Flat fan centered on the model's front (the root's -Z). The alpha lives in the vertex colors: 0 at the hero, fillAlpha at the arc, then a thin band at edgeAlpha.
		// Per step along the arc: the fan's rim, the band's inner side, the band's outer side.
		private static Mesh BuildCone(float range, float arcDegrees, float edgeWidth, float fillAlpha, float edgeAlpha)
		{
			int segments = Mathf.Max(2, Mathf.CeilToInt(arcDegrees / DegreesPerSegment));
			float halfArc = arcDegrees * 0.5f * Mathf.Deg2Rad;
			float innerRadius = Mathf.Max(0.01f, range - Mathf.Min(edgeWidth, range * 0.5f));

			Vector3[] vertices = new Vector3[1 + (segments + 1) * 3];
			Color[] colors = new Color[vertices.Length];
			int[] triangles = new int[segments * 9]; // per segment: one fan triangle and the band's two (the material does not cull, so the winding does not matter)

			vertices[0] = Vector3.zero;
			colors[0] = new Color(1f, 1f, 1f, 0f);

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
