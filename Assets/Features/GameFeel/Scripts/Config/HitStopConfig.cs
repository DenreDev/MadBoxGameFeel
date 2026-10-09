using UnityEngine;

namespace Game.GameFeel
{
	// Freeze frame on big blows only: a kill chained with others, or a critical strike. A regular hit or a lone kill never freezes, otherwise every swing would stutter.
	// Plugged into the weapon (WeaponConfig), so a heavy weapon can freeze longer than a light one.
	[CreateAssetMenu(fileName = "HitStop", menuName = "Feedback/Hit Stop")]
	public class HitStopConfig : ScriptableObject
	{
		[Header("Kill streak")]
		[SerializeField]
		[Tooltip("Seconds after a kill during which the next kill counts as part of the streak. The kill sound's rising pitch uses the same streak")]
		private float killStreakWindow = 1.5f;
		[SerializeField]
		[Tooltip("Kills chained inside the streak window needed before a kill freezes the game (1 = every kill, 2 = the second kill of a crowd, 0 or less would act like 1)")]
		private int minKillStreak = 2;
		[SerializeField]
		[Tooltip("Freeze, in real seconds, on the first kill that qualifies (0 disables the kill freeze)")]
		private float killStopDuration = 0.1f;
		[SerializeField]
		[Tooltip("Extra freeze for each further kill chained in the streak window, so killing a crowd feels heavier and heavier")]
		private float killStreakStopBonus = 0.03f;

		[Header("Critical strike")]
		[SerializeField]
		[Tooltip("Freeze, in real seconds, on every critical strike, even if it does not kill (0 disables). A longer kill streak freeze wins")]
		private float critStopDuration = 0.08f;

		[Header("Freeze")]
		[SerializeField]
		[Tooltip("The longest a single freeze can last, in real seconds, whatever the streak")]
		private float maxDuration = 0.22f;
		[SerializeField, Range(0f, 1f)]
		[Tooltip("Game speed during the freeze (0 = fully stopped, 0.1 = crawling, which keeps a little motion)")]
		private float timeScale = 0.02f;

		public float KillStreakWindow => killStreakWindow;
		public int MinKillStreak => minKillStreak;
		public float KillStopDuration => killStopDuration;
		public float KillStreakStopBonus => killStreakStopBonus;
		public float CritStopDuration => critStopDuration;
		public float MaxDuration => maxDuration;
		public float TimeScale => timeScale;

		// Freeze length of one blow, 0 for none. killStreak counts this kill (0 when the blow did not kill).
		public float GetDuration(int killStreak, bool isCritical)
		{
			float duration = 0f;
			if (killStreak >= minKillStreak && killStreak > 0)
			{
				int extraKills = killStreak - Mathf.Max(1, minKillStreak);
				duration = killStopDuration + extraKills * killStreakStopBonus;
			}

			if (isCritical) duration = Mathf.Max(duration, critStopDuration);
			return Mathf.Min(duration, maxDuration);
		}
	}
}
