# FEATURE: Smooth Hero Rotation

## 1. Summary
The hero no longer snaps to face his target when he auto-attacks. He eases toward it for a short moment, then eases back to his movement direction. All the hero's turns (attack, movement, start screen) now go through one easing helper.

## 2. Why / issue fixed
It looked weird to see the character rotate instantly. The hero swung at an enemy and his body jumped to face it in a single frame, then slid back to the walking direction.

## 3. Bottleneck
None.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `39 s` (16:16:14 → 16:16:53) |
| Implementation | `37 s` (16:16:53 → 16:17:30) |
| Tuning / testing | `2 min 31 s` (16:17:30 → 16:20:01) |
| Asset generation / integration | `0 min` |
| **Total** | `3 min 47 s` (about 4 min) |

Real elapsed time from recorded timestamps. The analysis start is the first timestamp of the session transcript, not a `Get-Date` taken at the time. Implementation includes the compile check. The testing block is the compile validation plus a play-mode probe that was written but never run (see section 10). It excludes waiting for answers and writing this note.

## 5. Files changed
- `HeroView.cs`: `OnHeroAttacked` no longer sets the rotation, it stores the target direction and an end time. `Update` turns toward that direction while the window is open, otherwise toward the movement direction. A new `TurnToward` helper does the easing for all three cases (attack, movement, `FaceCamera`). New serialized field `attackFacingDuration`.

## 6. Technical flow
1. `HeroController` finds the closest enemy in range and damages it right away (game rule, untouched). It raises `OnAttacked(targetPosition)`.
2. `HeroView.OnHeroAttacked` flattens the direction to the target. If it is long enough, it stores it as `_attackDirection` and sets `_attackFacingEndTime = now + attackFacingDuration`. It also fires the Attack animation trigger. It does not touch the rotation.
3. Every frame `HeroView.Update` picks one direction, in this order:
   - hero dead: nothing
   - start screen: toward the camera
   - inside the attack window: toward `_attackDirection`
   - joystick active: toward the movement direction
   - otherwise: keep the current facing
4. `TurnToward(direction)` does `Slerp(current, LookRotation(-direction), rotationSpeed * deltaTime)`. The `-` is because the model faces the root's -Z.

## 7. Tuning values
- `rotationSpeed` = `10` (on the Alice prefab): easing speed of every hero turn. Higher turns faster, lower feels heavier.
- `attackFacingDuration` = `0.3` s (default in code): how long the hero keeps aiming at his target after a swing before going back to the movement direction. `0` brings back the old "movement always wins" look.

## 8. Addressables
- **Used?** No.
- **Why not:** no asset is loaded. This is pure view logic.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- **Target on top of the hero:** a flat direction shorter than 0.1 units is ignored, so the hero keeps his facing.
- **Hero dies, then restarts:** `Update` returns early while dead and the window is only 0.3 s, so no stale direction survives a restart. The start-screen check comes before the attack window, so a stale window can never override the turn to the camera.
- **Moving target:** the direction is taken at the moment of the swing and is not tracked.
- **Walking away from an enemy:** the hero turns to the enemy for the swing, then back to the walking direction. The swing animation starts at once, so the body finishes turning during the swing.
- **Compile validated:** `Game.Entities.dll` was rebuilt after the edit and the console had no errors. The script validator reported duplicate methods, which is a false positive (each method appears once).
- **Not validated in play mode:** another session was playing in the shared editor, and running a probe there was denied. The probe (a forced 180° attack turn, sampled per frame) was deleted without being run. Needs a manual check: attack while standing still, then while walking away.
- **Deliberately not built:** constant angular speed, tracking a moving target, changes to the enemy turn (it already eased).

## 11. Base project observations
- `HeroView` and `EnemyView` each rotate themselves in `Update` with `Slerp(current, target, speed * deltaTime)`. That is an exponential ease, so the turn is fast at first and the speed depends a little on the frame rate.
- The model-faces-`-Z` convention was repeated in three places in `HeroView`. It is now in one helper.
- The attack handler set the rotation directly while the rest of the view eased it. That mismatch was the bug.

## 12. Likely review questions
- **Why not just `Slerp` once inside `OnHeroAttacked`?** It runs once per swing, so it would still be a one-step jump. The easing has to run every frame in `Update`.
- **Why does the swing win over the movement direction only for 0.3 s?** The hero moves and attacks at the same time. Aiming forever would break the link between the joystick and where he faces. 0.3 s is enough to see him aim and still lets steering take over.
- **Why keep the exponential `Slerp` and not `RotateTowards` in degrees per second?** It matches the enemy and the old hero movement. Switching would change what `rotationSpeed` means on existing prefabs. The trade-off is that the speed is not constant.
- **Is this a gameplay change?** No. The damage still lands in the controller at the same moment. Only the model's facing changes, and it lives in the view.
- **Why a serialized duration?** A designer can tune how long he aims, or set it to `0`.

## 13. 30-second explanation
The hero's auto-attack set his rotation directly, so he snapped to every target and then slid back to the walking direction. Now the attack only stores the direction and a short window. The view eases toward it every frame through the same helper used for movement and the start screen, then goes back to the movement direction. It is view-only, one file, and two values are tunable. The trade-off is the existing exponential easing, which is fast at first rather than constant speed.

## 14. README entry

### Smooth hero rotation — 4 min
- **Implemented:** the hero no longer snaps to face his target when he auto-attacks. He eases toward it for a moment, then back to his movement direction, and every hero turn now shares the same easing.
- **Time:** 4 min
- **Bottleneck:** none.
