# FEATURE: Run Animation Speed

## 1. Summary
The hero's Run animation now plays at a speed proportional to his real move speed, instead of a fixed 1×. The speed comes from `HeroConfig.MoveSpeed` and the joystick amount, so changing the move speed changes the animation with it.

## 2. Why / issue fixed
The run animation speed felt weird next to the hero's actual speed. The legs did not match how fast he was really moving, so the feet slid on the ground.

## 3. Bottleneck
Not confirmed by the human. What took the longest in practice: finding the real ground speed of the Run clip. The leg bone positions gave misleading numbers (the rig has an odd bone scale), so it was measured from the skinned mesh instead.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | not recorded |
| Implementation | not recorded |
| Tuning / testing | not recorded |
| Asset generation / integration | `0 min` |
| **Total** | not recorded |

No timestamps were taken during this feature, so no time is written. To be filled by the human.

## 5. Files changed
- `HeroAnimationController.controller`: new float parameter `RunSpeed` (default 1). The Run state now uses it as its speed multiplier, so only the run plays faster or slower. Attack, Hit and Death are untouched.
- `HeroView.cs`: new serialized `runClipGroundSpeed`. `UpdateAnimator` sets `RunSpeed` next to the existing `Speed` parameter.

## 6. Technical flow
1. The joystick state changes and `HeroView.UpdateAnimator` runs.
2. `Speed` (stick amount, 0 to 1) is set as before. It only decides Idle versus Run.
3. When the stick is pushed, `RunSpeed = HeroConfig.MoveSpeed * stickAmount / runClipGroundSpeed`. This is the same speed `HeroController.UpdatePosition` moves the hero at.
4. The Run state multiplies its playback speed by `RunSpeed`. With `MoveSpeed` 5 and a clip ground speed of 2, the run plays at 2.5×.
5. When the stick is released, `RunSpeed` is left as it was, so the run pose does not freeze while it blends out to Idle.

## 7. Tuning values
- `runClipGroundSpeed` = `2` units per second (on the hero prefab): the ground speed the Run clip covers at normal speed. Lower it to make the legs faster, raise it to make them slower.
- `HeroConfig.MoveSpeed` = `5`: unchanged, but it now drives the animation too.

## 8. Addressables
- **Used?** No.
- **Why not:** no asset is loaded. It is an animator parameter and a view value.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- **Partial stick push:** movement scales with stick amount in the controller, and so does `RunSpeed`, so a half push gives a half-speed run.
- **Stopping:** `RunSpeed` is not reset to zero, otherwise the run would freeze during the 0.19 s blend to Idle.
- **Move speed changed at runtime:** it is picked up at the next joystick change, not every frame.
- **Measured, not tuned in play:** the clip's ground speed was measured offline by sampling the skinned mesh vertices that touch the floor (median about 1.8 units/s, spread 1.3 to 2.4). The default of 2 is the middle of that. The look in play mode was not checked, because the editor was shared with other sessions. Needs a manual check while walking at full speed, and `runClipGroundSpeed` may need a small adjustment.
- **Deliberately not built:** a blend tree on speed, a walk clip for slow speeds, tracking the real displacement per frame.

## 11. Base project observations
- The `Speed` parameter was only the stick amount (0 to 1), so it could not describe how fast the hero really moves. The animation never depended on `MoveSpeed`.
- `HeroController` and `HeroView` each read the joystick separately, so the "real speed" formula exists in two places.
- The hero move clip is 2.57 s long and its leg bones sit at an odd scale, so bone positions cannot be used to measure strides.

## 12. Likely review questions
- **Why a state speed parameter and not `animator.speed`?** `animator.speed` would also speed up Attack and Hit. The parameter only affects Run.
- **Why a designer value instead of reading the clip?** The clip has no root motion to read. Its ground speed has to be measured or tuned, so it is exposed.
- **Why compute the speed in the view and not read the real displacement?** Per-frame displacement is noisy and the view would depend on the controller internals. The view uses the same inputs the controller does.
- **Why not a blend tree?** One clip is enough, and scaling its playback keeps the state machine as it was.

## 13. 30-second explanation
The Run animation always played at normal speed while the hero moved at his own speed, so the legs looked wrong and the feet slid. The Run state now has a speed multiplier driven by an animator parameter. The view sets it to the hero's move speed divided by the ground speed the clip covers, which is a tunable value. It is view-only, two files, and changing the move speed in the config changes the animation with it. The trade-off is that the clip ground speed was measured offline and still needs a look in play mode.

## 14. README entry

### Run animation speed — time not recorded
- **Implemented:** the hero's run animation now plays at a speed based on his real move speed, so the legs match how fast he moves. Details in [the feature note](Documentation/FeatureNotes/14_RunAnimationSpeed.md).
- **Time:** not recorded
- **Bottleneck:** not confirmed.
