# FEATURE: Attack impact pass

## 1. Summary
Three impact feedbacks on the hero's sword hits: a short freeze frame, a shove of the enemy's model away from the hero, and a camera kick along the slash direction.

## 2. Why / issue fixed
The attack did not feel impactful enough. The player could not feel the power of the sword because feedbacks such as hit stop, freeze frames when killing many enemies, and screen shake were missing.

> Deduced from the human's one-line request, not confirmed word for word.

## 3. Bottleneck
Not given by the human. Deduced from the work: the effect could not be seen in the validation. The probe measured the freeze (0.05 s and 0.08 s) and the shove (0.45 units), but the screen shake could not be measured and the first hits of a run were missed because the probe started the game before the services were ready.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis + implementation | `2 min 12 s` (17:14:04 → 17:16:16) |
| Tuning / testing | `1 min 10 s` (17:16:16 → 17:17:26) |
| Asset generation / integration | `0 min` |
| **Total** | `3 min 22 s` |

Real elapsed time from recorded timestamps. The split between analysis and implementation was not timestamped.

## 5. Files changed
- `GameFeelService.cs`: `PlayHitStop` (time scale dip on unscaled time, kill streak counter, restored on `Reset`).
- `HitFeedbackView.cs`: new `awayDirection` parameter, enemy shove applied as a position delta in `LateUpdate`, calls the hit stop.
- `EnemyView.cs`: passes the direction away from the hero on a hit and on a kill.
- `CameraShakeView.cs`: kicks on enemy hit and enemy kill, along hero to enemy.
- `GameFeelConfig.cs`: recoil, hit stop and shake values.

## 6. Technical flow
1. The hero's slash damages an enemy: the controller raises `OnEnemyDamaged` or `OnEnemyKilled`.
2. `EnemyView` plays the hit feedback with the direction away from the hero.
3. `HitFeedbackView` flashes, squashes, spawns the spark and the number, asks the service for a hit stop, and starts the shove (not on a killing blow: the death launch takes over).
4. `GameFeelService.PlayHitStop` sets `Time.timeScale` to a near-zero value for a few real milliseconds, then restores it. Overlapping freezes do not stack.
5. `CameraShakeView` kicks the camera impulse along the direction hero to enemy.

## 7. Tuning values
- `recoilDistance` = `0.45`, `recoilDuration` = `0.22` s: how far and how long the enemy's model is shoved.
- `killStopDuration`, `killStreakStopBonus`, `killStreakWindow`, `maxHitStopDuration`, `hitStopTimeScale`: freeze length and how it grows with chained kills.
- `enemyHitShake`, `enemyKillShake`: camera kick strength.

The human changed the first values after this pass (see section 10).

## 8. Addressables
- **Used?** No new asset.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- **Changed by the human afterwards** (commit "Freeze frame only on chained kills, bigger hit sparks" and note 13): a regular hit or a lone kill no longer freezes, the freeze starts at the second chained kill, the shake values were lowered (hit 0.25 to 0.1, kill 0.5 to 0.2) and the sword now hits up to 3 enemies per swing. A first version that froze on every hit was too heavy.
- The shove is cancelled before the death launch starts, so the two never fight over the model.
- The freeze loop is frame based and `Reset` restores the time scale, so a restart can never leave the game slowed.
- Not measured: the camera shake amplitude. Not tested: a long kill streak in play mode.

## 11. Base project observations
- The base project had no hit reaction on the enemy's position and no impact feedback on the camera.

## 12. Likely review questions
- **Why change `Time.timeScale` and not pause only the animators?** One value freezes everything the player sees (animators, particles, movement). The cost is that it affects the whole game, so it is short, capped and always restored.
- **Why is the shove a delta and not an absolute position?** Other components also move the model. Adding and removing an exact offset stays correct whatever order they run in.
- **Why does the streak live in the service?** The streak only drives presentation (freeze length), so it does not belong in the game rules.

## 13. 30-second explanation
The sword hits had no weight: no freeze, no shove, a tiny random camera shake. Now a hit shoves the enemy's model back, a chain of kills freezes the game for a few frames, and the camera kicks along the slash. It is view code only, tunable from one config. The trade-off is the global time scale, which is why the freeze is short, capped and restored on reset.

## 14. README entry

### Attack impact pass — 3 min
- **Implemented:** sword hits shove the enemy back, chained kills freeze the game for a few frames, and the camera kicks along the slash.
- **Time:** 3 min
- **Bottleneck:** not confirmed.
