# FEATURE: Enemy Attack Telegraph (WIP)

## 1. Summary
Enemies no longer hit instantly. In range they stop and wind up (red body blinks, a red disc grows on the ground to the attack range), then play the attack animation, and the hero is hit at the end of it only if he is still in range.

## 2. Why / issue fixed
Enemies telegraph their attacks so the player can dodge. Before, they attacked instantly, with no warning.

## 3. Bottleneck
Tuning the timings.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `1 min 45 s` (16:45:50 → 16:47:35) |
| Implementation | not recorded separately |
| Tuning / testing | not recorded separately |
| Asset generation / integration (if any) | included in the total (texture, 2 materials, prefab) |
| **Total** | `7 min 3 s` (16:45:50 → 16:52:53, about 7 min) |

Real elapsed time from `Get-Date` / `date` timestamps. Only the end of analysis was recorded at a phase boundary, so implementation and testing are not split. The human marked the feature as still WIP, so tuning time is not final. Excludes waiting for answers and writing this note.

## 5. Files changed
- `EnemiesController.cs`: per-enemy attack phases (None, Windup, Strike), new events `OnEnemyWindupStarted` and `OnEnemyStrikeStarted`. The hit is resolved when the strike ends. `AttackEnemy` now keeps the enemy's attack state when it updates health.
- `EnemyState.cs`: new `EnemyAttackPhase` enum, `AttackPhase` and `PhaseEndTime`, small `With...` helpers.
- `EnemyConfig.cs` / `BeeNormal.asset`: new `attackWindupDuration` (0.6 s) and `attackDuration` (0.45 s). `attackCooldown` lowered from 1 to 0.5 s.
- `EnemyView.cs` / `EnemiesContainerView.cs`: `PlayWindup` and `PlayStrike` replace `PlayAttack`. Death stops the telegraph.
- `AttackTelegraphView.cs` (new, on the bee prefab): red blink and growing ground disc.
- `GameFeelService.cs` / `GameFeelConfig.cs` / `GameFeelConfig.asset`: load and expose the disc prefab and the tint material, blink rate and disc start size.
- `View/Local/AttackWarning/` and `View/Local/AttackWarnTint/`: the generated assets.
- `BeeNormal.prefab`: added `AttackTelegraphView`, assigned it on `EnemyView`.

## 6. Technical flow
1. `EnemiesController.UpdateEnemy` runs by phase. With no phase, the enemy chases the hero.
2. In range and cooldown elapsed: phase becomes Windup (ends at now + `AttackWindupDuration`), the enemy stops, `OnEnemyWindupStarted` is raised.
3. `EnemiesContainerView` calls `EnemyView.PlayWindup`, which starts `AttackTelegraphView`: the body blinks to a red unlit material, a red disc grows from 30% to the full attack range.
4. When the windup ends: phase becomes Strike (ends at now + `AttackDuration`), `OnEnemyStrikeStarted` is raised. `EnemyView.PlayStrike` stops the telegraph and triggers the Attack animation.
5. When the strike ends, the controller checks the distance to the hero. In range: `TakeHit` and `OnEnemyAttacked`. Out of range: the attack misses. Either way the cooldown starts from that moment and the phase goes back to None.
6. The enemy does not move during Windup and Strike, so walking out of range dodges the hit.

## 7. Tuning values
- `attackWindupDuration` = `0.6` (`BeeNormal.asset`): how long the warning lasts, the dodge window.
- `attackDuration` = `0.45` (`BeeNormal.asset`): length of the attack animation, the hit lands at its end. Match the animation clip.
- `attackCooldown` = `0.5` (`BeeNormal.asset`): pause after an attack before the next windup. Was 1.
- `attackWarnBlinkRate` = `6` (`GameFeelConfig`): red blinks per second.
- `attackWarnStartScale` = `0.3` (`GameFeelConfig`): disc size at the start of the windup, relative to the attack range.
- Disc color and alpha = `_BaseColor` of `AttackWarningMaterial` (red, alpha 0.8). Blink color = `_BaseColor` of `AttackWarnTint`.

## 8. Addressables
- **Used?** Yes
- **Assets / addresses / group:** `VFX/AttackWarning` (prefab, brings its material and texture) and `VFX/AttackWarnTint` (material), group `VFX`.
- **Load:** during `GameFeelService.Initialize`, before gameplay.
- **Owner:** `GameFeelService` holds both handles.
- **Lifetime / cache:** loaded once. Each enemy creates its disc from the cached prefab on its first windup and reuses it (hidden between windups). The tint material is shared by all enemies.
- **Release:** `GameFeelService.Reset` releases the handles. The disc is a child of the enemy, destroyed with it.
- **Failure:** an unassigned or failed load logs a warning. A missing disc or tint only disables that part of the telegraph, the attack logic is unchanged.
- **Remote readiness:** the group uses the profile's load paths, so moving `VFX` to a CDN is a group setting. Everything is loaded before play.
- **Why Addressables here:** same loader, group and lifetime as the other VFX, easy to replace or swap per enemy type.

## 9. Generated assets
- **Used?** Yes
- **AttackWarningTexture.png:** 128x128 white disc with a soft translucent fill and a brighter edge ring, generated with a short Python script. Tintable, the color lives in the material. Integrated through `AttackWarningMaterial` and `VFX/AttackWarning`. Replacement cost: drop in another white-on-transparent texture.
- **AttackWarningMaterial.mat:** transparent unlit material, a copy of the hero ring material with the new texture and a red color.
- **AttackWarning.prefab:** a flat quad lying just above the floor, a copy of the hero ring prefab. Its size is set from code.
- **AttackWarnTint.mat:** a copy of the hit flash material, red. Replacement cost: change one color.
- Time: part of the feature total.

## 10. Edge cases and limits
- **Enemy killed mid-windup or mid-strike:** it leaves the controller dictionary, so no hit lands. The death calls `End()` on the telegraph, so the body is restored and the disc hidden.
- **Hero leaves range during the strike:** the attack misses, the cooldown still starts.
- **Hero hits the enemy mid-attack:** the attack is not interrupted, the hit flash and animation play over it. This is a design choice, not built.
- **Hit flash overlap:** the telegraph and the hit flash swap the same renderer materials. If both overlap, the last write wins for a few frames (the hit flash lasts 0.08 s).
- **Pause:** attack end times use `Time.time`, like the existing cooldown. Rounds are cleared on restart, so this was not an issue.
- **Missing config or assets:** no telegraph, no error. The attack timing still runs.
- **Validated in play mode:** phase timings from an editor probe log (windup 0.6 s, strike about 0.46 s, hero health drops at the end of the strike, cooldown before the next windup), plus screenshots showing the red disc and the red body. Not run: the miss case (hero leaving range), and an enemy dying mid-windup.
- **Deliberately not built:** a "!" icon over the head, a lunge or anticipation squash, interrupting an attack on damage, per-enemy-type timing curves.

## 11. Base project observations
- `EnemiesController.AttackEnemy` rebuilt the enemy state and silently dropped `LastAttackTime`. It now updates only the health.
- `SpawnEnemy` always uses `Enemies[0]`, so the other bee configs are never spawned.
- Attack range was the only attack parameter, and the hit was applied the frame the enemy arrived, so the attack animation always played after the damage.

## 12. Likely review questions
- **Why phases in the state and not a timer in the view?** Gameplay rules stay in the controller (the hit decision depends on the hero's position), views only react to events.
- **Why does the enemy stop during the windup?** Otherwise the player cannot read or dodge the attack. It is committed, which is the dodge window.
- **Why check the range at the end of the strike?** That is what makes the warning meaningful: if the hero moved away, the attack misses.
- **Why swap materials for the blink instead of a shader property?** It reuses the hit flash pattern: a shared unlit material, no per-enemy material instances.
- **Why Addressables for a disc?** To follow the project's content rules, same group and loader as the other VFX.

## 13. 30-second explanation
Enemies hit the hero the instant they arrived, with no warning. The controller now runs an attack in phases: windup, strike, then a distance check at the end of the strike. The views react to two new events: `AttackTelegraphView` blinks the body red and grows a red disc to the attack range, then the attack animation plays. The hero is hit only if he is still in range, so he can dodge. Trade-off: the enemy is committed and not interrupted by damage, and timings still need tuning.

## 14. README entry

### Enemy attack telegraph — 7 min
- **Implemented:** enemies now stop and warn before attacking: the body blinks red and a red disc grows on the ground to the attack range. Then the attack animation plays, and the hero is hit at its end only if he is still in range, so he can dodge. Still work in progress. Details in [the feature note](Documentation/FeatureNotes/15_EnemyAttackTelegraph.md).
- **Time:** 7 min
- **Bottleneck:** tuning the timings.
- **Addressables:** the red disc prefab and the tint material are preloaded from the `VFX` group.
