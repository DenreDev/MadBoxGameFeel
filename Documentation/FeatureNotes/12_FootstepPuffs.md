# FEATURE: Footstep Puffs

## 1. Summary
While the hero moves, a small dust puff pops at his feet every 1.2 units of ground covered (about 4 per second at his speed). It is purely visual: gameplay is untouched.

## 2. Why / issue fixed
- **Why:** highlight more the feeling of walking.
- **Issue fixed:** walking had no ground contact feedback.

## 3. Bottleneck
No real bottleneck.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `1 min` (16:29:41 to 16:30:28) |
| Implementation | `1 min` (16:30:28 to 16:31:05) |
| Tuning / testing | `8 min` (16:31:05 to 16:39:14) |
| Asset generation / integration | included above (no separate timestamp) |
| **Total** | `10 min` |

Real elapsed time from recorded timestamps: 47 s + 37 s + 8 min 9 s = 9 min 33 s, rounded to 10. Tuning / testing is mostly waiting for, and working around, a Unity editor shared with other sessions (the play-mode probe had to be rewritten several times, and a last play-mode capture was refused). The follow-up change that raised the puffs off the floor (shape offset 0.22 in the prefab) has no timestamp and is not counted. To be confirmed by the human.

## 5. Files changed
- `Assets/Features/GameFeel/Scripts/View/FootstepFeedbackView.cs`: new. Watches the ground distance walked by the entity it sits on and asks the service for a puff every `StepDistance`.
- `Assets/Features/GameFeel/Scripts/GameFeelService.cs`: preloads the step puff prefab (same pattern as the spark and the spawn puff), new `PlayStepPuff(position)`.
- `Assets/Features/GameFeel/Scripts/Config/GameFeelConfig.cs`: new `stepPuffPrefab` reference and `stepDistance` value.
- `Alice.prefab` (hero): `FootstepFeedbackView` added.
- `Assets/Features/GameFeel/View/Local/StepPuff/StepPuff.prefab`: new.
- `Assets/Resources/GameFeelConfig.asset`, `AddressableAssetsData/AssetGroups/VFX.asset`: reference, tuning value, new entry in the existing group.

`HeroView`, `HeroController` and the game rules were not touched.

## 6. Technical flow
1. At startup `GameFeelService.Initialize` loads `VFX/StepPuff` through Addressables and caches it.
2. Each frame `FootstepFeedbackView` compares the hero's position with the previous frame and adds the ground distance (Y ignored) to a counter.
3. When the counter reaches `StepDistance`, it subtracts `StepDistance` (the remainder is kept, so the rhythm does not depend on the frame rate) and calls `GameFeelService.PlayStepPuff` at the hero's position.
4. The service instantiates the cached prefab. It is one particle system: 3 to 4 warm off-white dust puffs that spread out low and fade, emitted 0.22 above the floor so they are not half buried in it. The prefab destroys itself when its particles end.
5. A move bigger than 1 unit in one frame (restart, initial placement) is treated as a teleport: the counter resets and no puff is played.
6. The start screen, game over and standing still produce no movement, so no puffs.

## 7. Tuning values
In `GameFeelConfig` (`Resources/GameFeelConfig`):
- `stepDistance` = `1.2` units: ground covered between two puffs (0 disables). Hero speed is 5 units/s, so 1.2 is about 4 puffs per second. Lower = denser trail.

In the puff prefab (designer-edited in the prefab, not in code): colors, particle count, size, spread, lift, and the 0.22 height offset (Shape module, Position Y).

## 8. Addressables
- **Used?** Yes.
- **Assets / addresses / group:** `StepPuff.prefab` at `VFX/StepPuff`, in the existing `VFX` group (local build and load path variables). Its material is the spawn puff's, an implicit dependency bundled with it.
- **Load:** once, in `GameFeelService.Initialize`, awaited before gameplay (UniTask, no `WaitForCompletion`).
- **Owner:** `GameFeelService`, in the same handle list as the other VFX.
- **Lifetime / cache:** loaded for the whole session. Every step is an `Instantiate` of the cached prefab, nothing loads while walking.
- **Release:** `GameFeelService.Reset` releases all handles.
- **Failure:** an unassigned or failing reference logs a warning and skips the puffs. The hero still walks normally.
- **Remote readiness:** moving the `VFX` group to a CDN is a group configuration change. The load is awaited at startup, so a remote group would need the download hidden behind a loading step. No code change.
- **Why Addressables here:** the puff is new runtime-loaded content, so it follows the project rule (dedicated group by content type, address, one owner, preload, release) and can be replaced or served remotely without touching code.

## 9. Generated assets
- **Used?** Yes.
- **`StepPuff.prefab`:** one small particle system, built with a one-off editor script that was deleted after use (no generation pipeline kept). Why: the spawn puff is lavender, bigger and has glow and sparkles, so a separate small neutral dust puff reads better under the feet. Integrated as Addressable `VFX/StepPuff`, referenced from `GameFeelConfig`. Replacement cost: low (replace the prefab behind the same address).
- No new texture or material: it reuses `SpawnPuffMaterial.mat` (soft white disc, vertex-colored).
- Time is included in implementation and testing.

## 10. Edge cases and limits
- **Not synced to the animation:** puffs follow distance walked, not the run clip's footfalls, so they will not line up exactly with each foot. Syncing would need animation events on the run clip.
- **Teleport (restart, placement):** moves over 1 unit in one frame reset the counter and play nothing.
- **Hero dead or on the start screen:** he does not move, so nothing plays.
- **Missing service or prefab:** `FootstepFeedbackView` skips the puff if the service is absent, the service skips it if the prefab did not load.
- **Measured in play mode:** the hero was walked 7.5 units at speed 5 for 1.5 s: 6 puffs spawned (7.5 / 1.2 = 6.25). No console errors.
- **Not verified visually:** the play-mode capture was taken on the start-screen camera and showed no puff, and a second capture was not possible because the shared editor refused to start a play session. The size, colors and the 0.22 height offset were set by reasoning, not seen. To be checked by the human.
- **Deliberately not built:** alternating left/right foot offset, enemy footsteps, a footstep sound, puff pooling, animation-event sync.

## 11. Base project observations
- `HeroView` moves the transform only from `HeroController.OnStateChanged`, so any movement-driven feedback has to read the transform (or subscribe to that event) rather than the joystick.
- The Unity editor is shared by several sessions, so play-mode checks are slow and unreliable. Entering play mode reloads the domain, which also drops editor update hooks and static state in probe scripts.

## 12. Likely review questions
- **Why distance and not animation events?** Distance is one small component and one tunable value, and it works whatever the clip does. Animation events would sync to the real footfalls but need edits on the imported run clip and a receiver on the model. For a dust puff at about 4 per second, distance is close enough.
- **Why poll the transform instead of listening to the controller?** The component stays independent: it needs no reference to the hero controller, so it could be reused on any walking entity. The cost is one position compare per frame.
- **Why keep the remainder when a puff plays?** So the rhythm stays the same at 30 or 144 fps. Resetting the counter to 0 would lose the overshoot at each step.
- **Why a separate prefab from the spawn puff?** Different role: tiny, neutral, short. The spawn puff is bigger with glow and sparkles. They share the material.
- **Why Addressables and no pool?** The prefab is new runtime content, so it follows the project rules. At about 4 small instances per second that destroy themselves, a pool would be extra code for no visible gain.

## 13. 30-second explanation
Walking had no contact with the ground. Now a small component on the hero counts the ground distance he covers and every 1.2 units asks the game feel service for a small dust puff at his feet. The puff is a particle prefab preloaded from the `VFX` Addressable group, lifted slightly so it sits on top of the floor. Gameplay is untouched. The trade-off is that puffs follow distance, not the exact footfalls of the run animation.

## 14. README entry
Copied into `README.md` under "Features".

### Footstep puffs — 10 min
- **Implemented:** a small dust puff pops at the hero's feet every few steps while he walks, to highlight the feeling of walking. Details in [the feature note](Documentation/FeatureNotes/12_FootstepPuffs.md).
- **Time:** 10 min
- **Bottleneck:** no real bottleneck.
- **Addressables:** the puff prefab is preloaded from the `VFX` group.
