# FEATURE: Critical strikes

## 1. Summary
The hero's sword hits can now critical strike (15% chance per enemy hit, x1.5 damage). A crit gets its own stronger feedback: bigger pink damage number with a "!", bigger spark, stronger squash and shove, a freeze frame even without a kill, a harder camera kick and the heavy crunch sound.

## 2. Why / issue fixed
The player's feeling on attack needed to go further, and the human wanted to play with the different hit feedbacks. A crit is a rare, readable moment where every feedback can be pushed higher at once.

> Deduced from the human's one-line answer ("increase player feeling on attack and especially play with the different hit feedbacks"), not confirmed word for word.

## 3. Bottleneck
Not given by the human.

> Deduced from the work, to be confirmed: validating in the shared Unity editor (no Unity MCP connection, so a self-driving probe and window focus were needed to force a recompile), and seeing the damage number at full size in a screenshot.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis + implementation | `2 min 2 s` (10:25:43 → 10:27:45) |
| Tuning / testing | `9 min 24 s` (10:27:45 → 10:37:09) |
| Asset generation / integration | `0 min` |
| **Total** | `11 min 26 s` |

Real elapsed time from recorded timestamps. The split between analysis and implementation was not timestamped. Testing includes waiting on the shared editor.

## 5. Files changed
- `WeaponConfig.cs`: `critChance` (0.15) and `critMultiplier` (1.5).
- `HeroController.cs`: `Slash` rolls the crit per enemy hit and applies the multiplier.
- `EnemiesController.cs`: `OnEnemyDamaged` / `OnEnemyKilled` now carry `isCritical`.
- `EnemiesContainerView.cs`, `EnemyView.cs`: pass the flag down to the hit feedback.
- `HitFeedbackView.cs`: crit scales the squash and the shove, forwards the flag to the service.
- `GameFeelService.cs`: crit spark size, always-on crit freeze, crit number look, heavy sound on a crit.
- `CameraShakeView.cs`: crit shake multiplier.
- `GameFeelConfig.cs`: new "Critical strike" block.

## 6. Technical flow
1. `HeroController.Slash` rolls `Random.value < CritChance` for each enemy hit. On a crit the damage is `Damage x CritMultiplier`, rounded.
2. `EnemiesController.AttackEnemy` applies the damage and raises `OnEnemyDamaged` or `OnEnemyKilled` with `isCritical`.
3. `EnemyView` plays the hit or the death with the flag. `HitFeedbackView` scales squash and shove, then asks `GameFeelService` for the spark, the number, the freeze and the sound.
4. `CameraShakeView` multiplies its kick when the flag is set.
5. The rule stays in the controller. The views only react to the flag.

## 7. Tuning values
On `WeaponConfig`:
- `critChance` = `0.15`: chance per enemy hit (0 disables crits).
- `critMultiplier` = `1.5`: damage multiplier.

On `GameFeelConfig` (Critical strike):
- `critSparkScale` = `1.8`: spark size.
- `critImpactScale` = `1.6`: squash and shove strength.
- `critStopDuration` = `0.08` s: freeze on every crit. A longer kill-streak freeze wins.
- `critShakeMultiplier` = `2`: camera kick.
- `critNumberScale` = `1.4`, `critNumberTopColor`, `critNumberBottomColor`: number size and pink-purple gradient. A crit kill stacks with the kill size.

## 8. Addressables
- **Used?** No new asset. The crit reuses the loaded spark, number prefab and kill sound.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- The roll is per enemy, so a multi-hit swing can crit on some targets only. The camera still kicks once per frame.
- Pooled third-party package numbers keep their previous look, so the suffix, colors and scale are set on every spawn (checked: a regular number right after a crit has no "!").
- A crit on a lone, non-killing hit freezes for `critStopDuration`. A regular hit never freezes.
- Crit is applied to the hero's attacks only. Enemy attacks do not crit.
- Not built: crit-specific sound or particle assets, a crit flash on the screen, a stronger death launch on a crit kill.
- Validated in play mode with crit forced to 100% then 0%: damage 45 / 30, freezes of 70 to 98 ms on every crit (also on a non-kill), no freeze on regular non-kill hits. The sound change was not listened to. The "!" is small next to the number.

## 11. Base project observations
- The game feel config is one ScriptableObject singleton, so adding a feedback block was quick.
- The enemy events carried only a damage value, so a hit "type" had to be added to the event signature.

## 12. Likely review questions
- **Why is the crit decided in `HeroController` and not in the view?** It changes the damage dealt, so it is a game rule. The views only receive a flag and make it feel stronger.
- **Why a bool on the events and not a hit-info struct?** One flag is all that is needed today. A struct would be the next step if more hit types (elemental, backstab) appear.
- **Why per enemy and not per swing?** A multi-hit swing feels more varied, and each number shows its own crit.
- **Why does the freeze take the longest value instead of adding?** Stacked freezes would stall the game on a chained crit kill. Overlapping freezes already do not stack in the service.

## 13. 30-second explanation
Hits all felt alike. Now each enemy hit has a 15% chance to crit for x1.5 damage. The controller rolls it and raises the flag on the existing hit events. The views make the crit feel stronger: bigger pink number with a "!", bigger spark, stronger squash and shove, a short freeze, a harder camera kick and the heavy crunch. All values are in configs. The trade-off is a plain bool on the events instead of a richer hit-info type.

## 14. README entry

### Critical strikes — 11 min
- **Implemented:** sword hits can now critical strike (15% chance, x1.5 damage). A crit gets a bigger pink damage number with a "!", a bigger spark, a stronger squash and shove, a freeze frame, a harder camera kick and the heavy crunch sound. Details in [the feature note](Documentation/FeatureNotes/20_CriticalStrikes.md).
- **Time:** 11 min
- **Bottleneck:** not confirmed.
