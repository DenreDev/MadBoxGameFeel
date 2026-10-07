# FEATURE: Hit Feedback, Ragdoll-Style Death and Screen Shake

## 1. Summary
Every hit on the hero or on a bee now flashes the body white, squashes it and spits a small spark burst. A killed bee or the dead hero is knocked away from the attacker in a flipping hop (a ragdoll-style death). The camera gets small kicks when the hero attacks, is hit and dies.

## 2. Why / issue fixed
- **Why:** it was too hard for the player to understand when the hero got hit by the enemies.
- **Issue fixed:** a hit taken is now readable at the moment it lands, through the body, the sparks and the camera, instead of only through the health value.

> Deduced by the agent from one sentence of the human ("it was too hard to understand when you got hit by the enemies so this should be fixing it"). To confirm or refine before the review.

## 3. Bottleneck
Not recorded (the human chose to skip it).

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `5 min` (10:34:08 to 10:39:17) |
| Implementation | `4 min` (10:39:17 to 10:43:20) |
| Tuning / testing | `16 min` (10:43:20 to 10:59:11) |
| Asset generation / integration | included in implementation (no separate timestamp) |
| **Total** | `25 min` |

Real elapsed time from recorded timestamps (10:34:08 to 10:59:11 = 25 min 3 s). Tuning / testing is an upper bound: it includes time lost on an editor shared with other sessions (play mode reset by their recompiles, then a compile block caused by a third-party asset imported by someone else). To be confirmed by the human.

## 5. Files changed
- `Assets/Features/GameFeel/Scripts/GameFeelService.cs`: new `IService`. Preloads the spark prefab and the flash material through Addressables, owns the handles, spawns sparks.
- `Assets/Features/GameFeel/Scripts/Config/GameFeelConfig.cs`: new ScriptableObject singleton (in `Resources`) with the content references and the tuning values.
- `Assets/Features/GameFeel/Scripts/View/HitFeedbackView.cs`: flash, squash and sparks on the entity it sits on.
- `Assets/Features/GameFeel/Scripts/View/DeathLaunchView.cs`: the ragdoll-style launch of the model.
- `Assets/Features/GameFeel/Scripts/Game.GameFeel.asmdef`: new assembly.
- `Assets/Features/Entities/Scripts/View/Heroes/CameraShakeView.cs`: listens to the hero events and fires Cinemachine impulses.
- `HeroController.cs`: new event `OnDied`, raised right after `OnStateChanged` on the killing hit. Damage rules untouched.
- `HeroView.cs`: plays the hit feedback (behind the existing `hitReactionCooldown`) and the death launch, restores the model on restart.
- `EnemyView.cs`: plays the hit feedback on hit and the feedback plus the launch on death.
- `Alice.prefab`, `BeeNormal.prefab`: `HitFeedbackView` and `DeathLaunchView` added and assigned on the views.
- `World.prefab`: `CinemachineImpulseSource` and `CameraShakeView` on the root, `CinemachineImpulseListener` on the virtual camera.
- `Game.Entities.asmdef`: references `Game.GameFeel`.
- `Assets/Features/GameFeel/View/Local/`: `HitFlash/HitFlash.mat`, `HitSpark/HitSpark.prefab`, `HitSpark/HitSparkMaterial.mat`.
- `Assets/Resources/GameFeelConfig.asset`, `AddressableAssetsData/AssetGroups/VFX.asset`: config instance and the new group.

## 6. Technical flow
1. A gameplay controller raises an event. Controllers know nothing about feedback.
2. A view reacts:
   - **Bee hit:** `OnEnemyDamaged` → `EnemiesContainerView` → `EnemyView.PlayHit` → `HitFeedbackView.Play()`.
   - **Hero hit:** `OnDamaged` → `HeroView.OnHeroDamaged` → `HitFeedbackView.Play()`, at most once per `hitReactionCooldown`.
   - **Bee killed:** `OnEnemyKilled` → `EnemyView.PlayDeath` → `HitFeedbackView.Play()` and `DeathLaunchView.Play(away from the hero)`.
   - **Hero killed:** `OnDied` → `HeroView.OnHeroDied` → `HitFeedbackView.Play()` and `DeathLaunchView.Play(away from the closest enemy)`.
   - **Restart:** `OnStateChanged` with `IsDead` false → `HeroView` calls `DeathLaunchView.Restore()`.
   - **Camera:** `CameraShakeView` listens to `OnAttacked`, `OnDamaged` and `OnDied`. Each one calls `CinemachineImpulseSource.GenerateImpulseWithVelocity` with a random direction on the ground plane.
3. `HitFeedbackView.Play()` swaps every body renderer to the shared white `HitFlash` material for `flashDuration`, squashes the model for `punchDuration`, and asks `GameFeelService` for a spark burst at the body center. It runs in `LateUpdate` and disables itself when done.
4. `DeathLaunchView` throws the body center along an arc away from the attacker and flips the model around that center. It bounces and settles at the starting height. The existing Death animation plays at the same time. It moves only the model child, never the entity root.
5. The Cinemachine impulse reaches the virtual camera through the `CinemachineImpulseListener` extension (same channel).

## 7. Tuning values
In `GameFeelConfig` (one asset, `Resources/GameFeelConfig`):
- `flashDuration` = `0.08` s: how long the body stays white.
- `punchSquash` = `0.15`: how much the body is flattened at the instant of a hit.
- `punchDuration` = `0.18` s: time to settle back.
- `attackShake` = `0.04`, `hitShake` = `0.15`, `deathShake` = `0.3`: camera impulse strength in world units, 0 disables.
- `hitShakeCooldown` = `0.5` s: minimum time between two hit shakes.

On each `DeathLaunchView` (per prefab):
- Bee: `distance` = `2` m, `hopHeight` = `0.8` m, `airTime` = `0.5` s, `flips` = `1`.
- Hero: `distance` = `1.5` m, `hopHeight` = `0.4` m, `airTime` = `0.55` s, `flips` = `1`.
- Both: `bounciness` = `0.35` (speed kept on each landing), `variation` = `0.2` (random spread of distance and height).

Existing value reused: `HeroView.hitReactionCooldown` = `0.5` s.

## 8. Addressables
- **Used?** Yes.
- **Assets / addresses / group:** `HitSpark.prefab` at `VFX/HitSpark` and `HitFlash.mat` at `VFX/HitFlash`, in the new `VFX` group. The group was created by copying the `UI` group schemas, so it uses the profile variables `LocalBuildPath` and `LocalLoadPath`.
- **Load:** at startup, in `GameFeelService.Initialize`, awaited before gameplay (UniTask, no `WaitForCompletion`).
- **Owner:** `GameFeelService` keeps every handle in one list.
- **Lifetime / cache:** loaded once and kept for the whole session. The flash material is shared by all renderers. The spark prefab is instantiated from the cached asset on each hit (it destroys itself when its particles end), nothing loads on a hit.
- **Release:** `GameFeelService.Reset` releases all handles.
- **Failure:** a missing config or a failed load logs a warning and skips only that feedback. The game keeps running. This was seen for real when another feature's reference was still unassigned: a caught `InvalidKeyException` warning, no crash.
- **Remote readiness:** moving the `VFX` group to a CDN is a group configuration change (switch its build and load path to the Remote variables). The load is awaited at startup, so a remote group would need the download hidden behind a loading step. No code change.
- **Why Addressables here:** the spark and the flash look are new runtime-loaded content, so they follow the project rule (dedicated group, address, one owner, preload, release) and can be swapped or served remotely without touching code.

## 9. Generated assets
- **Used?** Yes.
- **`HitFlash.mat`:** a white URP Unlit material. Why: a tint on the Lit material cannot get brighter than the character texture, so swapping to a plain white unlit material for a few frames is the simplest reliable flash. Integrated as Addressable `VFX/HitFlash`, referenced from `GameFeelConfig`. Replacement cost: very low (change the color or the shader).
- **`HitSpark.prefab` + `HitSparkMaterial.mat`:** a one-shot particle burst, 8 yellow-orange stretched sparks that shrink to nothing in 0.15 to 0.3 s, no texture. Why: the classic impact cue, cheap and readable. Integrated as Addressable `VFX/HitSpark`, referenced from `GameFeelConfig`. Replacement cost: low (replace the prefab behind the same address).
- Time is included in implementation. The assets were built with a one-off editor script that was deleted after use (no generation pipeline kept).

## 10. Edge cases and limits
- **Missing `GameFeelConfig`:** found in the first play test. `GameFeelService.Initialize` threw and would have stopped the whole service startup. It now returns with the feedback disabled. The views still read the config every frame without a null check, like the other configs in the project.
- **Crowd of bees:** without limits the hero would strobe and the screen would shake non-stop. The hero reaction is gated by the existing `hitReactionCooldown`, the camera hit shake by `hitShakeCooldown`.
- **Camera follows the hero root:** the death launch moves only the model child. Measured in play mode: the root moved `0.000` while the hero was launched.
- **Animator overwriting transforms:** squash and launch run in `LateUpdate`, after the Animator.
- **Landing pose:** `flips` are whole numbers, so the body lands upright and the existing Death clip lays it down. A half flip would land upside-down on top of the clip's pose.
- **Restart:** the model returns exactly to its prefab pose (measured: local position 0, rotation 180°, scale 2.31). Restart happens when the game flow goes back to the start screen, so the launch plays under the Game Over state.
- **Killer of the hero:** the view does not know which bee killed the hero, so it uses the closest enemy. With none left, the launch goes straight behind the model.
- **Flat ground:** the launch assumes the ground is at the body's starting height.
- **Not verified:** the camera shake was not measured, only checked in the prefab and code. A first measurement mixed the shake with the camera settling, and a clean one was blocked by a compile error from a third-party asset. Its strength and feel need a human check. The launch distance and height were lowered after the last measured run.
- **Deliberately not built:** a real bone ragdoll (colliders and joints on every bone, too heavy for the scope), hit stop, hit sounds, spark pooling, a flash on the weapon (only renderers present at load time flash).

## 11. Base project observations
- `ServicesLocator` has no `try/catch` around `Initialize`, so one throwing service stops all the startup.
- `EnemyView.OnDestroy` (and the other views) unsubscribe through `ServicesLocator.Instance`, which throws a `NullReferenceException` at play-mode exit.
- `ServicesLocator.ResetServices` throws when `_orderedServices` is null, which happens when a duplicate instance is destroyed in `Awake`.
- The virtual camera in the World prefab had no impulse listener, so any camera effect needed that extension added.
- `HeroController.OnDamaged` is deliberately not raised on the killing hit (feature 01), which is why a separate `OnDied` was needed.

## 12. Likely review questions
- **Why a fake ragdoll and not a real one?** A real ragdoll needs a collider, a rigid body and a joint on every bone of the hero and the bee, plus a ground, and it needs resetting on restart. A ballistic arc plus a flip around the body center gives the "big hit" read with one small component, and the existing Death animation supplies the limbs.
- **Why Cinemachine impulses and not a custom shake?** Unity already ships it and the camera is already a Cinemachine rig. The source fires, the listener on the virtual camera applies it, and strengths stay in one config. No camera code to maintain.
- **Why is `CameraShakeView` in `Game.Entities` and not in `Game.GameFeel`?** `Game.Entities` references `Game.GameFeel` (for the hit and death components) and the shake needs `HeroController`. Putting it in `Game.GameFeel` would create an assembly cycle. The World prefab already hosts other `Game.Entities` views.
- **Why swap materials for the flash?** The characters use a textured Lit material, where a color tint cannot go brighter than the texture. A white unlit swap is the simplest reliable result, and it is a shared Addressable material.
- **Why move the model child and not the root?** The camera follows the hero root. Moving the root would drag the camera with the hero's launch. The model child is free.

## 13. 30-second explanation
The player could not tell when the hero was hit by the enemies. Every hit on the hero or a bee now flashes the body white, squashes it and spits sparks, and the camera gets a small kick when the hero attacks, is hit or dies. Killed bees and the dead hero are knocked away in a flipping hop, a ragdoll-style death on top of the existing Death animation. Controllers only raise events, small views react, and the flash and spark come from a `VFX` Addressable group preloaded by one service. The main trade-off is that the ragdoll is a rigid-body tumble, not a bone-level one, to keep it small and explainable.

## 14. README entry

### Hit feedback, ragdoll death and screen shake — 25 min
- **Implemented:** every hit on the hero or a bee flashes the body white, squashes it and spits a small spark burst. Killed bees and the dead hero are knocked away in a flipping, ragdoll-style hop, and the camera gets small shakes when the hero attacks, is hit and dies. Details in [the feature note](Documentation/FeatureNotes/07_HitFeedbackRagdollShake.md).
- **Time:** 25 min
- **Addressables:** the spark prefab and the flash material are preloaded from the `VFX` group.
