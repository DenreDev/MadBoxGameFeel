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

		public string Id => id;
		public int Damage => damage;
		public float Range => range;
		public float Cooldown => cooldown;
		public WeaponView Prefab => prefab;
		public float HitDelay => hitDelay;
		public float SwingDuration => swingDuration;
		public int MaxTargets => Mathf.Max(1, maxTargets);
	}
}