using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.GamePlay.Enemies
{
	[CreateAssetMenu(fileName = "EnemyX", menuName = "Content/Enemy")]
	public class EnemyConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int initialHealth;
		[SerializeField] private float speed;
		[SerializeField] private float attackCooldown;
		[SerializeField] private int attackDamage;
		[SerializeField, Tooltip("Distance to the hero at which the enemy starts its attack, and within which the hero must still be when the hit lands")]
		private float attackRange;
		[SerializeField, Tooltip("Seconds the enemy stands still and telegraphs the attack once the hero is in range")]
		private float attackWindupDuration = 0.6f;
		[SerializeField, Tooltip("Seconds of the attack animation. The hit lands at its end if the hero is still in range. Match the animation clip length")]
		private float attackDuration = 0.45f;
		[SerializeField, Min(0f), Tooltip("Meters the enemy dashes toward the hero at the start of its attack animation, so a hero who ignores the telegraph gets hit. 0 = attacks in place")]
		private float lungeDistance;
		[SerializeField, Tooltip("Kamikaze: hits the instant it reaches the attack range, with no windup, and dies doing so (no reward for the hero)")]
		private bool explodesOnContact;
		[SerializeField, Tooltip("Enemy prefab (with an EnemyView), loaded through Addressables at startup")]
		private AssetReferenceGameObject prefab;

		public string Id => id;
		public int InitialHealth => initialHealth;
		public float Speed => speed;
		public float AttackCooldown => attackCooldown;
		public int AttackDamage => attackDamage;
		public float AttackRange => attackRange;
		public float AttackWindupDuration => attackWindupDuration;
		public float AttackDuration => attackDuration;
		public float LungeDistance => lungeDistance;
		public bool ExplodesOnContact => explodesOnContact;
		public AssetReferenceGameObject Prefab => prefab;
	}
}
