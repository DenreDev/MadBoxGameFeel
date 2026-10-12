using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GameFeel;
using UnityEngine;

namespace Game.Weapons
{
	// How a weapon attacks: the one thing that differs between a sword, a spear or a bow. HeroAttack decides WHEN to attack (cooldown, a target in range)
	// and then asks the weapon to Perform. Plugged into WeaponConfig, so each weapon owns its attack and its tuning.
	public abstract class WeaponAttack : ScriptableObject
	{
		// Runs the whole attack, from its first frame until the hero is free to attack again, and lands the hits through the context.
		// Cancelled when the game shuts down.
		public abstract UniTask Perform(IAttackContext context, WeaponConfig weapon, CancellationToken cancellationToken);

		// Addressable assets of this attack's feedback, loaded before gameplay (see FeedbackPreloadService). Nothing by default.
		public virtual UniTask PreloadAssets(FeedbackAssetsService assets) => UniTask.CompletedTask;
	}
}
