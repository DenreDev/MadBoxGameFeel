using Game.GameFeel;
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
		[SerializeField, Min(0.05f), Tooltip("Radius of the body on the ground, in meters. Enemies steer around each other and never overlap closer than their radii. Bigger bodies are heavier: they shove small ones aside")]
		private float bodyRadius = 0.5f;
		[SerializeField] private float attackCooldown;
		[SerializeField] private int attackDamage;
		[SerializeField, Tooltip("Distance to the hero at which the enemy starts its attack, and within which the hero must still be when the hit lands")]
		private float attackRange;
		[SerializeField, Tooltip("What the attack is (Melee: telegraphed bite with an optional lunge, Explode: kamikaze). Empty = the enemy chases but never attacks")]
		private EnemyAttack attack;
		[SerializeField, Tooltip("Enemy prefab (with an EnemyView), loaded through Addressables at startup")]
		private AssetReferenceGameObject prefab;

		[Header("Feedback (each one is optional: leave it empty and the enemy simply has no such feedback)")]
		[SerializeField, Tooltip("Flash, squash, shove and spark when this enemy is hit")]
		private HitFeedbackConfig hitFeedback;
		[SerializeField, Tooltip("Floating number over this enemy when it is hit")]
		private DamageNumberConfig damageNumbers;
		[SerializeField, Tooltip("Pop-in and dust puff when this enemy appears")]
		private SpawnFeedbackConfig spawnFeedback;
		[SerializeField, Tooltip("Red blink and ground disc while this enemy winds up its attack")]
		private AttackTelegraphConfig attackTelegraph;
		[SerializeField, Tooltip("Currency orbs this enemy bursts into when it dies")]
		private CurrencyDropConfig currencyDrop;

		public string Id => id;
		public int InitialHealth => initialHealth;
		public float Speed => speed;
		public float BodyRadius => bodyRadius;
		public float AttackCooldown => attackCooldown;
		public int AttackDamage => attackDamage;
		public float AttackRange => attackRange;
		public EnemyAttack Attack => attack;
		public float AttackReach => attackRange + (attack != null ? attack.ExtraReach : 0f); // the attack range plus what the attack can still cover (a lunge)
		public AssetReferenceGameObject Prefab => prefab;
		public HitFeedbackConfig HitFeedback => hitFeedback;
		public DamageNumberConfig DamageNumbers => damageNumbers;
		public SpawnFeedbackConfig SpawnFeedback => spawnFeedback;
		public AttackTelegraphConfig AttackTelegraph => attackTelegraph;
		public CurrencyDropConfig CurrencyDrop => currencyDrop;
	}
}
