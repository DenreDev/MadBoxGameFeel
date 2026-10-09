using System;
using Core.ServicesManager;
using Game.GameFeel;
using Game.GameFlow;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	// Combo HUD on the right of the screen: kills in a row make "COMBO x12" climb, with a bar that drains until the next kill.
	// The combo breaks when the bar runs out or when the hero is hit. Every few kills the combo reaches a new tier: new colors, a bigger number and a slammed title.
	// It only listens to EnemiesController.OnEnemyKilled and HeroController.OnDamaged / OnDied: gameplay knows nothing about it.
	// Animations run on unscaled time so the hit-stop freeze frames do not stall them. The combo window runs on scaled time, so a freeze does not eat it.
	public class ComboCounterView : MonoBehaviour
	{
		private enum State { Hidden, Active, Breaking }

		[Serializable]
		private struct ComboTier
		{
			[Tooltip("Combo count at which this tier starts")]
			public int fromCount;
			[Tooltip("Title slammed on screen when the combo reaches this tier (empty = none, used for the first tier)")]
			public string title;
			public Color topColor;
			public Color bottomColor;
		}

		[Header("Scene references")]
		[SerializeField] private RectTransform panel;
		[SerializeField] private CanvasGroup panelGroup;
		[SerializeField] private RectTransform numberRect;
		[SerializeField] private TMP_Text numberText;
		[SerializeField] private TMP_Text labelText;
		[SerializeField] private Image glow;
		[SerializeField] private Image ring;
		[SerializeField] private RectTransform barFill;
		[SerializeField] private Image barFillImage;
		[SerializeField] private CanvasGroup milestone;
		[SerializeField] private RectTransform milestoneRect;
		[SerializeField] private TMP_Text milestoneText;
		[SerializeField] private Image[] sparks;

		[Header("Combo")]
		[SerializeField, Min(0.1f), Tooltip("Seconds after a kill to get the next one before the combo breaks")]
		private float comboWindow = 3f;
		[SerializeField, Min(2), Tooltip("Kills in a row needed before the counter shows up (1 kill is not a combo)")]
		private int minShownCount = 2;
		[SerializeField, Tooltip("Color tiers, from the first kills to the biggest combos. fromCount must grow from one tier to the next")]
		private ComboTier[] tiers =
		{
			new ComboTier { fromCount = 0, title = "", topColor = new Color(1f, 0.96f, 0.6f), bottomColor = new Color(1f, 0.7f, 0.15f) },
			new ComboTier { fromCount = 5, title = "NICE!", topColor = new Color(1f, 0.8f, 0.3f), bottomColor = new Color(1f, 0.4f, 0.05f) },
			new ComboTier { fromCount = 10, title = "GREAT!", topColor = new Color(1f, 0.5f, 0.3f), bottomColor = new Color(0.95f, 0.12f, 0.12f) },
			new ComboTier { fromCount = 20, title = "AWESOME!", topColor = new Color(1f, 0.55f, 0.95f), bottomColor = new Color(0.6f, 0.15f, 0.9f) },
			new ComboTier { fromCount = 35, title = "UNSTOPPABLE!", topColor = new Color(0.6f, 1f, 1f), bottomColor = new Color(0.15f, 0.5f, 1f) },
			new ComboTier { fromCount = 50, title = "LEGENDARY!", topColor = new Color(1f, 1f, 0.85f), bottomColor = new Color(1f, 0.78f, 0.1f) }
		};

		[Header("Kill punch")]
		[SerializeField, Min(1f), Tooltip("Scale of the number the moment a kill lands (1.6 = 60% bigger), then it springs back")]
		private float punchScale = 1.55f;
		[SerializeField, Min(1f), Tooltip("How much stronger the punch is when the kill reaches a new tier")]
		private float tierPunchMultiplier = 1.8f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the number to settle back")]
		private float punchDuration = 0.4f;
		[SerializeField, Min(0f), Tooltip("Degrees the number is thrown off its tilt by a kill, then it wobbles back")]
		private float kickAngle = 9f;
		[SerializeField, Min(0f), Tooltip("Extra size of the number for each tier reached (0.08 = 8% bigger per tier)")]
		private float tierScaleStep = 0.08f;
		[SerializeField, Min(0f), Tooltip("Pixels the number is shaken by a kill, fading with the punch")]
		private float shakeAmount = 7f;
		[SerializeField, Min(1f), Tooltip("How much bigger the ring around the number grows while it fades out")]
		private float ringScale = 1.9f;
		[SerializeField, Range(0f, 1f), Tooltip("Opacity of the glow behind the number when nothing happens")]
		private float glowRestAlpha = 0.28f;
		[SerializeField, Min(0f), Tooltip("How much the number breathes while the combo is alive (0.03 = 3%)")]
		private float idlePulse = 0.025f;

		[Header("Time bar")]
		[SerializeField, Range(0f, 1f), Tooltip("Share of the bar left under which it starts blinking red and the number trembles")]
		private float lowTimeThreshold = 0.3f;
		[SerializeField, Min(0f), Tooltip("Pixels the number trembles while the bar is low")]
		private float lowTimeShake = 3f;
		[SerializeField, Min(0f), Tooltip("Red blinks per second of the bar when time is low")]
		private float lowTimeBlinkRate = 8f;
		[SerializeField, Tooltip("Color the bar blinks to when time is low")]
		private Color lowTimeColor = new Color(1f, 0.2f, 0.2f);

		[Header("Appear and break")]
		[SerializeField, Min(0.01f), Tooltip("Seconds for the counter to slide in")]
		private float appearDuration = 0.35f;
		[SerializeField, Tooltip("Pixels the counter slides in from, to the right")]
		private float appearOffset = 380f;
		[SerializeField, Min(0.01f), Tooltip("Seconds the broken combo takes to fall away")]
		private float breakDuration = 0.7f;
		[SerializeField, Range(0f, 1f), Tooltip("Share of the break spent shaking in place before it falls")]
		private float breakShakeShare = 0.3f;
		[SerializeField, Tooltip("Pixels the broken combo falls")]
		private float breakFallDistance = 260f;
		[SerializeField, Tooltip("Degrees the broken combo tumbles while it falls")]
		private float breakSpin = 24f;
		[SerializeField, Min(0f), Tooltip("Pixels the number is shaken by a break caused by a hit")]
		private float breakShakeAmount = 14f;
		[SerializeField, Tooltip("Tint of the number once broken")]
		private Color brokenColor = new Color(0.5f, 0.5f, 0.55f);
		[SerializeField] private string brokenByHitLabel = "BROKEN!";
		[SerializeField] private string brokenByTimeLabel = "TIME'S UP";
		[SerializeField] private string comboLabel = "COMBO";

		[Header("Tier title and sparks")]
		[SerializeField, Min(0.01f), Tooltip("Seconds for the tier title to slam in")]
		private float titleSlamDuration = 0.25f;
		[SerializeField, Min(1f), Tooltip("Scale the title starts at before it slams down")]
		private float titleStartScale = 2.8f;
		[SerializeField, Min(0f), Tooltip("Seconds the title stays readable")]
		private float titleHoldDuration = 0.7f;
		[SerializeField, Min(0.01f), Tooltip("Seconds for the title to fade out")]
		private float titleFadeDuration = 0.35f;
		[SerializeField, Tooltip("Pixels the title drifts up while it fades")]
		private float titleRiseDistance = 50f;
		[SerializeField, Min(0), Tooltip("Sparks thrown by a regular kill")]
		private int killSparks = 4;
		[SerializeField, Min(0), Tooltip("Sparks thrown when the combo reaches a new tier")]
		private int tierSparks = 14;
		[SerializeField, Tooltip("Start speed of a spark, in pixels per second (x = slowest, y = fastest)")]
		private Vector2 sparkSpeed = new Vector2(250f, 650f);
		[SerializeField, Tooltip("Pull on a spark, in pixels per second squared")]
		private float sparkGravity = 900f;
		[SerializeField, Min(0.05f), Tooltip("Seconds a spark lives")]
		private float sparkLife = 0.55f;

		private readonly ComboTracker _tracker = new ComboTracker();
		private GameFlowService _gameFlowService;
		private FeedbackAssetsService _assets;
		private FeedbackAudioService _audio;
		private ComboSoundsConfig _sounds; // the hero's (HeroConfig.ComboSounds), null = silent combo
		private EnemiesController _enemiesController;
		private HeroController _heroController;

		private State _state = State.Hidden;
		private int _tierIndex;
		private Vector2 _panelHome;
		private Vector2 _numberHome;
		private Vector2 _titleHome;
		private float _panelTilt;
		private float _appearTime;
		private float _breakTime;
		private bool _brokenByHit;
		private float _punchTime;
		private float _punchStrength;
		private float _kickSign = 1f;
		private float _titleTime;
		private Color _titleColor;
		private float _ringStrength;

		private Vector2[] _sparkVelocity;
		private float[] _sparkAge;
		private Color[] _sparkColor;

		private float TitleDuration => titleSlamDuration + titleHoldDuration + titleFadeDuration;

		private void Awake()
		{
			_panelHome = panel.anchoredPosition;
			_numberHome = numberRect.anchoredPosition;
			_titleHome = milestoneRect.anchoredPosition;
			_panelTilt = panel.localEulerAngles.z;
			_sparkVelocity = new Vector2[sparks.Length];
			_sparkAge = new float[sparks.Length];
			_sparkColor = new Color[sparks.Length];

			_tracker.OnIncreased += OnComboIncreased;
			_tracker.OnBroken += OnComboBroken;
			ResetVisuals();
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_assets = ServicesLocator.Instance.GetService<FeedbackAssetsService>();
			_audio = ServicesLocator.Instance.GetService<FeedbackAudioService>();
			_sounds = HeroConfig.Instance.ComboSounds;

			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_enemiesController = entitiesService.EnemiesController;
			_heroController = entitiesService.HeroController;
			_enemiesController.OnEnemyKilled += OnEnemyKilled;
			_heroController.OnDamaged += OnHeroHit;
			_heroController.OnDied += OnHeroHit;

			_gameFlowService = ServicesLocator.Instance.GetService<GameFlowService>();
			_gameFlowService.OnStateChanged += OnGameStateChanged;
		}

		private void OnDestroy()
		{
			_tracker.OnIncreased -= OnComboIncreased;
			_tracker.OnBroken -= OnComboBroken;

			// The locator can already be gone when the scene is torn down.
			if (ServicesLocator.Instance != null) ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null) _enemiesController.OnEnemyKilled -= OnEnemyKilled;
			if (_heroController != null)
			{
				_heroController.OnDamaged -= OnHeroHit;
				_heroController.OnDied -= OnHeroHit;
			}

			if (_gameFlowService != null) _gameFlowService.OnStateChanged -= OnGameStateChanged;
		}

		// Kamikaze explosions are not kills (no OnEnemyKilled), and they hit the hero: they break the combo through OnHeroHit.
		private void OnEnemyKilled(int enemyId, int damage, bool isCritical) => _tracker.RegisterKill(comboWindow);

		private void OnHeroHit() => _tracker.Break();

		// Back on the start screen: the next round starts from nothing, with no break animation to play.
		private void OnGameStateChanged(GameState state)
		{
			if (state != GameState.Start) return;

			_tracker.Reset();
			ResetVisuals();
		}

		private void OnComboIncreased(int count)
		{
			if (count < minShownCount) return;

			int tierIndex = TierFor(count);
			bool isNewTier = _state == State.Active && tierIndex > _tierIndex;
			bool isFresh = _state != State.Active;

			if (isFresh)
			{
				_state = State.Active;
				_appearTime = 0f;
				_breakTime = 0f;
				labelText.text = comboLabel;
			}

			_tierIndex = tierIndex;
			ComboTier tier = tiers[tierIndex];
			numberText.text = $"<size=55%>x</size>{count}";
			numberText.colorGradient = new VertexGradient(tier.topColor, tier.topColor, tier.bottomColor, tier.bottomColor);
			numberText.color = Color.white;
			labelText.color = tier.topColor;
			glow.color = new Color(tier.topColor.r, tier.topColor.g, tier.topColor.b, 1f);
			ring.color = new Color(tier.topColor.r, tier.topColor.g, tier.topColor.b, 1f);

			_punchTime = 0f;
			_punchStrength = isNewTier ? tierPunchMultiplier : 1f;
			_ringStrength = isNewTier ? 1f : 0.6f;
			_kickSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
			EmitSparks(isNewTier ? tierSparks : killSparks, isNewTier, tier.topColor);

			if (_sounds != null) _audio.Play(_assets.Get(_sounds.HitClip), _sounds.HitVolume, _sounds.GetHitPitch(count - minShownCount));

			if (isNewTier && !string.IsNullOrEmpty(tier.title))
			{
				milestoneText.text = tier.title;
				milestoneText.colorGradient = new VertexGradient(tier.topColor, tier.topColor, tier.bottomColor, tier.bottomColor);
				_titleTime = 0f;
				if (_sounds != null) _audio.Play(_assets.Get(_sounds.MilestoneClip), _sounds.MilestoneVolume);
			}
		}

		private void OnComboBroken(int finalCount, bool expired)
		{
			if (_state != State.Active) return;

			_state = State.Breaking;
			_breakTime = 0f;
			_brokenByHit = !expired;
			labelText.text = expired ? brokenByTimeLabel : brokenByHitLabel;
			labelText.color = brokenColor;
			if (!expired)
			{
				// A hit flares a red glow and ring, with no size punch: the number just shakes and falls.
				glow.color = lowTimeColor;
				ring.color = lowTimeColor;
				_punchTime = 0f;
				_punchStrength = 0f;
				_ringStrength = 0.8f;
			}

			if (_sounds != null) _audio.Play(_assets.Get(_sounds.BreakClip), _sounds.BreakVolume);
		}

		private int TierFor(int count)
		{
			for (int i = tiers.Length - 1; i > 0; i--)
			{
				if (count >= tiers[i].fromCount) return i;
			}

			return 0;
		}

		private void ResetVisuals()
		{
			_state = State.Hidden;
			_tierIndex = 0;
			_titleTime = TitleDuration;
			panelGroup.alpha = 0f;
			ApplyTitle();

			for (int i = 0; i < sparks.Length; i++)
			{
				_sparkAge[i] = sparkLife;
				sparks[i].enabled = false;
			}
		}

		private void Update()
		{
			float deltaTime = Time.unscaledDeltaTime;
			_tracker.Tick(Time.deltaTime);

			_titleTime += deltaTime;
			ApplyTitle();
			UpdateSparks(deltaTime);

			if (_state == State.Hidden) return;

			_appearTime += deltaTime;
			_punchTime += deltaTime;

			if (_state == State.Breaking)
			{
				_breakTime += deltaTime;
				if (_breakTime >= breakDuration)
				{
					_state = State.Hidden;
					panelGroup.alpha = 0f;
					return;
				}
			}

			ApplyPanel();
			ApplyNumber();
			ApplyBar();
		}

		// The panel slides in from the right, and falls, tumbles and fades when the combo breaks.
		private void ApplyPanel()
		{
			float appear = Mathf.Clamp01(_appearTime / appearDuration);
			float breakT = _state == State.Breaking ? Mathf.Clamp01(_breakTime / breakDuration) : 0f;
			float fall = Mathf.Clamp01((breakT - breakShakeShare) / Mathf.Max(0.01f, 1f - breakShakeShare));

			panel.anchoredPosition = _panelHome + new Vector2(Mathf.LerpUnclamped(appearOffset, 0f, EaseOutBack(appear)), -breakFallDistance * fall * fall);
			panel.localRotation = Quaternion.Euler(0f, 0f, _panelTilt - breakSpin * fall);
			panelGroup.alpha = Mathf.Clamp01(appear * 3f) * (1f - fall * fall);
		}

		// Punch (big, springs back), kick (thrown off tilt, wobbles back), shake, breathing, tier size. Greys out and shakes when broken.
		private void ApplyNumber()
		{
			float punchT = Mathf.Clamp01(_punchTime / punchDuration);
			float punch = Mathf.LerpUnclamped(1f + (punchScale - 1f) * _punchStrength, 1f, EaseOutBack(punchT));
			float breathing = 1f + idlePulse * Mathf.Sin(Time.unscaledTime * 7f);
			float tierScale = 1f + tierScaleStep * _tierIndex;

			float shake = shakeAmount * _punchStrength * (1f - punchT);
			if (_state == State.Active && _tracker.Fill < lowTimeThreshold) shake += lowTimeShake;
			if (_state == State.Breaking && _brokenByHit) shake += breakShakeAmount * Mathf.Clamp01(1f - _breakTime / (breakDuration * breakShakeShare));

			numberRect.localScale = Vector3.one * (punch * tierScale * breathing);
			numberRect.localRotation = Quaternion.Euler(0f, 0f, kickAngle * _kickSign * Mathf.Cos(punchT * Mathf.PI * 2.5f) * (1f - punchT));
			numberRect.anchoredPosition = _numberHome + UnityEngine.Random.insideUnitCircle * shake;

			if (_state == State.Breaking) numberText.color = Color.Lerp(Color.white, brokenColor, Mathf.Clamp01(_breakTime * 8f));

			// The glow flares on a kill and settles; the ring grows off the number and fades.
			Color glowColor = glow.color;
			glowColor.a = Mathf.Lerp(1f, glowRestAlpha, EaseOutCubic(punchT));
			glow.color = glowColor;
			glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, EaseOutCubic(punchT)) * tierScale;

			Color ringColor = ring.color;
			ringColor.a = _ringStrength * (1f - punchT);
			ring.color = ringColor;
			ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, ringScale, EaseOutCubic(punchT));
		}

		// The bar drains until the next kill, flashes white on a kill, and blinks red when the combo is about to break.
		private void ApplyBar()
		{
			float fill = _state == State.Active ? _tracker.Fill : 0f;
			barFill.localScale = new Vector3(fill, 1f, 1f);

			ComboTier tier = tiers[_tierIndex];
			Color color = Color.Lerp(Color.white, tier.bottomColor, EaseOutCubic(Mathf.Clamp01(_punchTime / 0.2f)));
			if (_state == State.Active && fill < lowTimeThreshold)
			{
				bool blink = Mathf.Repeat(Time.unscaledTime * lowTimeBlinkRate, 1f) < 0.5f;
				color = blink ? lowTimeColor : tier.bottomColor;
			}

			barFillImage.color = color;
		}

		// Slam (drops in from big), hold, then fade while drifting up.
		private void ApplyTitle()
		{
			float slam = Mathf.Clamp01(_titleTime / titleSlamDuration);
			float fade = Mathf.Clamp01((_titleTime - titleSlamDuration - titleHoldDuration) / titleFadeDuration);

			milestone.alpha = Mathf.Clamp01(slam * 3f) * (1f - fade);
			milestoneRect.localScale = Vector3.one * Mathf.LerpUnclamped(titleStartScale, 1f, EaseOutBack(slam));
			milestoneRect.anchoredPosition = _titleHome + new Vector2(0f, titleRiseDistance * EaseOutCubic(fade));
		}

		// A regular kill throws a few sparks to the left (into the screen). A new tier bursts all around.
		private void EmitSparks(int count, bool allAround, Color color)
		{
			for (int emitted = 0, i = 0; emitted < count && i < sparks.Length; i++)
			{
				if (_sparkAge[i] < sparkLife) continue;

				float angle = allAround ? UnityEngine.Random.value * 360f : UnityEngine.Random.Range(100f, 260f);
				float speed = UnityEngine.Random.Range(sparkSpeed.x, sparkSpeed.y);
				_sparkVelocity[i] = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * speed;
				_sparkColor[i] = Color.Lerp(color, Color.white, UnityEngine.Random.Range(0.2f, 0.7f));
				_sparkAge[i] = 0f;
				sparks[i].rectTransform.anchoredPosition = numberRect.anchoredPosition;
				sparks[i].enabled = true;
				emitted++;
			}
		}

		private void UpdateSparks(float deltaTime)
		{
			for (int i = 0; i < sparks.Length; i++)
			{
				if (_sparkAge[i] >= sparkLife) continue;

				_sparkAge[i] += deltaTime;
				if (_sparkAge[i] >= sparkLife)
				{
					sparks[i].enabled = false;
					continue;
				}

				float life = _sparkAge[i] / sparkLife;
				_sparkVelocity[i] += Vector2.down * (sparkGravity * deltaTime);
				RectTransform rect = sparks[i].rectTransform;
				rect.anchoredPosition += _sparkVelocity[i] * deltaTime;
				rect.localScale = Vector3.one * (1f - life * life);
				Color color = _sparkColor[i];
				color.a = 1f - life;
				sparks[i].color = color;
			}
		}

		private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

		// Overshoots 1 before settling, which gives the slam and the punch their bounce.
		private static float EaseOutBack(float t)
		{
			const float overshoot = 1.70158f;
			float shifted = t - 1f;
			return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
		}
	}
}
