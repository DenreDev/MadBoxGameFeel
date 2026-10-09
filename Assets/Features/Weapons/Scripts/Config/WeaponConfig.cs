using UnityEngine;

namespace Game.Weapons
{
	[CreateAssetMenu(fileName = "WeaponX", menuName = "Content/Weapon")]
	public class WeaponConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int damage;
		[SerializeField] private float range;
		[SerializeField] private float cooldown;
		[SerializeField] private WeaponView prefab;
		[SerializeField, Tooltip("Seconds between the start of the swing and the slash's high point, when the damage lands. Match the hero's Attack animation (clip time of the impact / Attack state speed).")]
		private float hitDelay = 0.33f;
		[SerializeField, Tooltip("Seconds the whole swing animation lasts: clip length / Attack state speed (0.81), plus the blend back to idle. The hero keeps facing his target and cannot start another swing until it is over.")]
		private float swingDuration = 0.86f;
		[SerializeField, Min(1), Tooltip("How many enemies one swing can hit at the same time. The one the hero aims at is always hit first, then the closest ones in range. 1 = single target.")]
		private int maxTargets = 1;
		[SerializeField, Range(10f, 360f), Tooltip("Width in degrees of the cone in front of the hero that a swing covers, centered on the enemy he aims at. Enemies outside of it are not hit, even when they are closer than the range. 360 = all around him.")]
		private float arcAngle = 120f;
		[SerializeField, Range(0f, 1f), Tooltip("Chance for each enemy hit to be a critical strike (0.15 = 15%). Rolled per enemy, so a multi-hit swing can crit on some targets only. 0 disables criticals.")]
		private float critChance = 0.15f;
		[SerializeField, Min(1f), Tooltip("Damage multiplier of a critical strike (1.5 = +50%). The result is rounded to a whole number.")]
		private float critMultiplier = 1.5f;

		public string Id => id;
		public int Damage => damage;
		public float Range => range;
		public float Cooldown => cooldown;
		public WeaponView Prefab => prefab;
		public float HitDelay => hitDelay;
		public float SwingDuration => swingDuration;
		public int MaxTargets => Mathf.Max(1, maxTargets);
		public float ArcAngle => Mathf.Clamp(arcAngle, 10f, 360f);
		public float CritChance => critChance;
		public float CritMultiplier => Mathf.Max(1f, critMultiplier);
	}
}