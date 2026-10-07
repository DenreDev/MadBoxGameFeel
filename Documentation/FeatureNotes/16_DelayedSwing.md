# FEATURE: Delayed swing, aim and full animation

## 1. Summary
The hero's attack is no longer instant. The swing animation starts first, the damage lands 0.33 s later when the slash reaches its high point, the hero keeps facing his target for the whole swing (the aim wins over the movement direction), and the swing always plays completely.

## 2. Why / issue fixed
The attack did not feel right: the damage landed the moment the swing started, so the sword hit before it had visibly moved. While walking, the movement direction took over the body, so the hero did not look at what he hit. The swing could also be cut short by a hit reaction.

> Deduced from the human's request ("the attack rotation should prevail over the movement rotation, the animation should always play fully, the attack should wait for the slash high point"). Not confirmed word for word.

## 3. Bottleneck
Not given by the human. Deduced from the work: making the damage match the animation. Two things only showed up in play-mode measurements: back-to-back swings started their animation up to 0.13 s late (the previous swing was still blending out), and an aim direction fixed at the start of the swing was stale when the hero walked.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `1 min 26 s` (17:02:23 → 17:03:49) |
| Implementation | `1 min 11 s` (17:03:49 → 17:05:00) |
| Tuning / testing | `3 min 42 s` (17:06:22 → 17:10:04) |
| Asset generation / integration | `1 min 22 s` (17:05:00 → 17:06:22) |
| **Total** | `7 min 41 s` |

Real elapsed time from recorded timestamps. Testing is three play-mode probe runs and the two fixes they led to.

## 5. Files changed
- `HeroController.cs`: `Swing` starts the animation, waits `HitDelay`, calls `Slash`, then waits for the rest of `SwingDuration` before another swing can start. New event `OnAttackStarted(enemyId)`. `OnAttacked` now means "the slash landed".
- `HeroView.cs`: on swing start it triggers the Attack animation and keeps turning toward the aimed enemy (its current position, every frame) until the swing ends. A hit reaction no longer interrupts the swing (the flash still plays).
- `WeaponConfig.cs` and `GreatSword.asset`: `hitDelay` = 0.33, `swingDuration` = 0.86.
- `GameFeelService.cs`, `GameFeelConfig.cs`: sword slash effect (later removed, see section 9).

## 6. Technical flow
1. The controller finds the closest enemy in range and, if the cooldown is over and no swing is running, starts `Swing`.
2. `OnAttackStarted(enemyId)` is raised. The view triggers the Attack animation and starts following that enemy with its facing.
3. After `HitDelay` the controller deals the damage (`Slash`) and raises `OnAttacked`.
4. After `SwingDuration` the swing ends: the next swing is allowed and the facing goes back to the movement direction.
5. Damage numbers, hit sparks and camera shake react to the existing enemy events, so they now fire at the slash instead of at the swing start.

## 7. Tuning values
- `hitDelay` = `0.33` s (weapon): swing start to damage. Clip time of the impact (0.5 s) divided by the Attack state speed (1.5).
- `swingDuration` = `0.86` s (weapon): the animation (1.217 s / 1.5 = 0.81 s) plus the blend back to idle. Gates the next swing and holds the aim.

## 8. Addressables
- **Used?** Yes, only for the slash effect, which no longer exists in the game (see below).

## 9. Generated assets
- **Used?** Yes, a crescent slash (texture, material, particle prefab) at `VFX/SwordSlash`, group `VFX`, about 1 min 22 s. The human removed it later because it looked odd (see note 13). The game no longer loads it. The files and the Addressable entry can be deleted.

## 10. Edge cases and limits
- Target died or left the range during the windup: the closest enemy in range takes the hit instead (note 13 later extended this to several targets).
- Nothing in range at the slash: the swing still plays and announces itself.
- Hero dies mid-swing: no damage lands, the aim window is cleared.
- Measured in play mode: the damage lands 0.33 s after the swing start, the facing error at the slash is 0 to 5 degrees both standing and walking.
- Not tested: a hit received mid-swing, death mid-swing.
- The stale `attackFacingDuration` value stays serialized on the Alice prefab. Nothing reads it.

## 11. Base project observations
- The base controller damaged the enemy in the same frame as the swing trigger, so animation and damage could never line up.
- The attack animation sat in the same animator layer as movement. Another session moved it to an upper-body layer, which is what lets him swing while running.

## 12. Likely review questions
- **Why does the controller own the delay and not an animation event?** Presentation must not drive gameplay. A timer in the controller keeps damage independent of the animator (culling, speed changes). The cost is that `hitDelay` has to be kept in sync with the clip by hand.
- **Why a separate `swingDuration`?** So the swing cannot be restarted before it finishes, and so the aim is held for the full swing.
- **Why track the enemy and not store a direction?** The hero walks and enemies chase, so a direction fixed at the start was up to 90 degrees off at the slash.

## 13. 30-second explanation
The hero's attack dealt its damage at the same moment as the swing started, and walking turned his body away from his target. The controller now starts the animation, waits for the slash high point to deal the damage, and holds the swing until the animation is done. The view keeps aiming at the enemy for the whole swing. The trade-off is that the weapon's hit delay is tuned by hand to the clip.

## 14. README entry

### Delayed swing and aim — 8 min
- **Implemented:** the hero's attack now waits for the slash high point to deal damage, keeps facing its target for the whole swing and always plays its animation completely.
- **Time:** 8 min
- **Bottleneck:** not confirmed.
- **Addressables:** the slash effect used one (since removed).
