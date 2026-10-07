using UnityEngine;

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
		[SerializeField] private float attackRange;
		[SerializeField, Tooltip("Seconds the enemy stands still and telegraphs the attack once the hero is in range")]
		private float attackWindupDuration = 0.6f;
		[SerializeField, Tooltip("Seconds of the attack animation. The hit lands at its end if the hero is still in range. Match the animation clip length")]
		private float attackDuration = 0.45f;
		[SerializeField] private EnemyView prefab;

		public string Id => id;
		public int InitialHealth => initialHealth;
		public float Speed => speed;
		public float AttackCooldown => attackCooldown;
		public int AttackDamage => attackDamage;
		public float AttackRange => attackRange;
		public float AttackWindupDuration => attackWindupDuration;
		public float AttackDuration => attackDuration;
		public EnemyView Prefab => prefab;
	}
}
