# FEATURE: Multi-hit Great Sword

## 1. Summary
The equipped Great Sword now hits up to 3 enemies in range with the same swing. The crescent slash effect was removed and the camera shake on hits was lowered.

## 2. Why / issue fixed
The feature was created to increase the impact of the hero's attack, especially with the Great Sword. A big sword that only ever hit one enemy felt weak. The slash effect looked odd and the screen shake on hits was too strong, so both were toned down to keep the hit clean.

> Deduced from the human's one-line answer (not confirmed word for word).

## 3. Bottleneck
Not given by the human. Deduced from the work: with several enemies hit at once, every per-hit feedback fires several times, so the camera shake had to be capped to one kick per swing.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `~1 min` |
| Implementation | `~1.5 min` |
| Tuning / testing | `0 min` (compiled, not play-tested) |
| Asset generation / integration (if any) | `0 min` |
| **Total** | `2 min 26 s` |

Timestamps: start 17:19:55, end of implementation 17:22:21. The split between analysis and implementation is not timestamped, only the total is exact.

## 5. Files changed
- `WeaponConfig.cs`: new `maxTargets` value (min 1, default 1) and `MaxTargets` accessor.
- `GreatSword.asset`: `maxTargets` = 3.
- `HeroController.cs`: `Slash` hits every enemy found by the new `FindTargetsInRange`, instead of one.
- `CameraShakeView.cs`: enemy hit and kill shakes fire once per frame.
- `GameFeelConfig.cs` and `GameFeelConfig.asset`: lower shake values, slash settings removed.
- `GameFeelService.cs`, `HeroView.cs`: slash effect loading, playing and event subscription removed.

## 6. Technical flow
1. The swing starts, the damage lands at the weapon's `HitDelay` (unchanged).
2. `HeroController.Slash` collects every enemy closer than `Range`.
3. The enemy the hero aimed at sorts first, the others by distance. The list is cut to `MaxTargets`.
4. Each target goes through the existing `AttackEnemy`, which raises `OnEnemyDamaged` or `OnEnemyKilled`.
5. Views react per enemy as before (flash, squash, spark, number, hit-stop). `CameraShakeView` kicks only for the first enemy of the frame.
6. `OnAttacked` is raised once, with the position of the first enemy hit.

## 7. Tuning values
- `maxTargets` (weapon) = `3` on the Great Sword: enemies one swing can hit.
- `attackShake` = `0.02` (was 0.04): shake when the swing lands, hit or miss.
- `enemyHitShake` = `0.1` (was 0.25): shake when an enemy survives a hit.
- `enemyKillShake` = `0.2` (was 0.5): shake when an enemy dies.

## 8. Addressables
- **Used?** No new asset. The `VFX/SwordSlash` entry (prefab, material, texture) is no longer loaded by the game. The files and the Addressable entry are still in the project and can be deleted.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- The aimed enemy died or left range during the windup: it is simply not in the list, the closest ones are hit instead.
- Nothing in range: the list is empty and the swing still plays and announces itself.
- Killing an enemy during the loop is safe: the loop iterates its own list, not the controller's dictionary.
- Not built: no frontal arc. Enemies behind the hero within range are also hit.
- Not play-tested: the Unity editor is shared with other sessions. Compile check only.

## 11. Base project observations
- `EnemiesController.AttackEnemy` takes a snapshot of the enemy and re-reads the real state itself, which made multi-hit safe without changes.

## 12. Likely review questions
- **Why `maxTargets` on the weapon and not in `GameFeelConfig`?** It changes the gameplay result (who takes damage), so it belongs with the weapon's damage and range.
- **Why is the aimed enemy always first?** The hero visibly faces it, so it must always take the hit, even if another enemy became closer during the windup.
- **Why one camera shake per frame?** Three simultaneous hits would triple the shake. The kick should read as one blow.
- **Why not a cone or arc?** Smallest change first. It is a possible next step (`hitArc` on the weapon).
- **Does hit-stop stack with several enemies?** No: overlapping freezes keep the longest one. Kills in the same swing add to the kill streak bonus, capped by `MaxHitStopDuration`.

## 13. 30-second explanation
The Great Sword attacks felt weak because they only ever hit one enemy. The weapon now has a `maxTargets` value, and the swing hits the aimed enemy plus the closest ones in range, up to that number. Damage still goes through the same controller method, so all existing hit feedback plays per enemy. Camera shake was lowered and capped to one kick per swing, and the odd-looking slash effect was removed. Trade-off: no frontal arc yet, so enemies behind the hero can be hit.

## 14. README entry
### Multi-hit Great Sword — 2 min
- **Implemented:** the Great Sword now hits up to 3 enemies with one swing to give the attack more impact. The slash effect was removed and the screen shake on hits is lower. Details in [the feature note](Documentation/FeatureNotes/13_MultiHitSword.md).
- **Time:** 2 min
- **Bottleneck:** not recorded.
