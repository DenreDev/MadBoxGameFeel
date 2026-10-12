using UnityEngine;

namespace Game.GameFeel
{
	// The two easing curves the hand-made feedback animations share. t goes from 0 to 1.
	public static class Easing
	{
		// 0 -> 1 fast, then slows down.
		public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

		// 0 -> 1 with a swing past 1 that settles back: the cartoon "pop". overshoot = 0 is a plain ease-out, 1.7 is the classic back-ease.
		public static float OutBack(float t, float overshoot = 1.70158f)
		{
			float u = t - 1f;
			return 1f + u * u * ((overshoot + 1f) * u + overshoot);
		}
	}
}
