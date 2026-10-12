using Game.GameFeel;
using UnityEngine;

namespace Game.Weapons
{
	[CreateAssetMenu(fileName = "WeaponX", menuName = "Content/Weapon")]
	public class WeaponConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int damage;
		[SerializeField, Tooltip("How far an enemy can be for the hero to start attacking, and the reach of the attack itself")]
		private float range;
		[SerializeField] private float cooldown;
		[SerializeField] private WeaponView prefab;
		[SerializeField, Tooltip("How this weapon attacks (a slash, ...). The hero decides when to attack from the Cooldown and Range above, the attack decides what happens then. Without one the hero cannot attack.")]
		private WeaponAttack attack;
		[SerializeField, Range(0f, 1f), Tooltip("Chance for each enemy hit to be a critical strike (0.15 = 15%). Rolled per enemy, so a multi-hit swing can crit on some targets only. 0 disables criticals.")]
		private float critChance = 0.15f;
		[SerializeField, Min(1f), Tooltip("Damage multiplier of a critical strike (1.5 = +50%). The result is rounded to a whole number.")]
		private float critMultiplier = 1.5f;

		[Header("Feedback (each one is optional: leave it empty and the weapon simply has no such feedback)")]
		[SerializeField, Tooltip("Swing whoosh and the hit, kill and critical impact sounds")]
		private WeaponSoundsConfig sounds;
		[SerializeField, Tooltip("Freeze frame on chained kills and critical strikes")]
		private HitStopConfig hitStop;

		public string Id => id;
		public int Damage => damage;
		public float Range => range;
		public float Cooldown => cooldown;
		public WeaponView Prefab => prefab;
		public WeaponAttack Attack => attack;
		public float CritChance => critChance;
		public float CritMultiplier => Mathf.Max(1f, critMultiplier);
		public WeaponSoundsConfig Sounds => sounds;
		public HitStopConfig HitStop => hitStop;
	}
}