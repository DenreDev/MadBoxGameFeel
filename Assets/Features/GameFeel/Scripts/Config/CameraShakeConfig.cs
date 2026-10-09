using UnityEngine;

namespace Game.GameFeel
{
	// Camera kicks on combat events. Impulse strengths in world units, 0 disables one.
	// Plugged into the CameraShakeView that sits on the World prefab next to the virtual camera.
	[CreateAssetMenu(fileName = "CameraShake", menuName = "Feedback/Camera Shake")]
	public class CameraShakeConfig : ScriptableObject
	{
		[Header("Hero attacks")]
		[SerializeField]
		[Tooltip("The hero's slash reaches its high point, hit or miss")]
		private float attackShake = 0.02f;
		[SerializeField]
		[Tooltip("The hero's weapon hits an enemy that survives. The kick goes along the slash. A swing hitting several enemies kicks once")]
		private float enemyHitShake = 0.1f;
		[SerializeField]
		[Tooltip("The hero's weapon kills an enemy. The kick goes along the slash. A swing hitting several enemies kicks once")]
		private float enemyKillShake = 0.2f;
		[SerializeField]
		[Tooltip("Kick multiplier on a critical strike (2 = twice the regular hit or kill shake)")]
		private float critShakeMultiplier = 2f;

		[Header("Hero takes damage")]
		[SerializeField]
		[Tooltip("Hero takes a hit")]
		private float hitShake = 0.15f;
		[SerializeField]
		[Tooltip("Hero dies")]
		private float deathShake = 0.3f;
		[SerializeField]
		[Tooltip("Minimum seconds between two hit shakes, so a crowd hitting the hero does not shake the screen non-stop")]
		private float hitShakeCooldown = 0.5f;

		public float AttackShake => attackShake;
		public float EnemyHitShake => enemyHitShake;
		public float EnemyKillShake => enemyKillShake;
		public float CritShakeMultiplier => critShakeMultiplier;
		public float HitShake => hitShake;
		public float DeathShake => deathShake;
		public float HitShakeCooldown => hitShakeCooldown;
	}
}
