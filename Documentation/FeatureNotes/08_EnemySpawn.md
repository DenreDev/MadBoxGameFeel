# FEATURE: Enemy Spawn

## 1. Summary
A bee no longer appears instantly at full size. It pops in from nothing with a springy overshoot while a lavender dust puff, a flat ground glow and a few cyan sparkles burst at its feet. The effect is purely visual: the enemy exists for the gameplay rules from the first frame.

## 2. Why / issue fixed
- **Why:** improve the overall feeling of the enemy spawn.
- **Issue fixed:** the spawn was missing juice and polish.

## 3. Bottleneck
Not recorded (the human chose to skip it).

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `2 min` (10:48:49 to 10:50:35) |
| Implementation | `5 min` (10:50:35 to 10:55:46) |
| Tuning / testing | `9 min` (16:07:21 to 16:15:58) |
| Asset generation / integration | included above (no separate timestamp) |
| **Total** | `8 min` (human-corrected, see below) |

Real elapsed time from recorded timestamps: 1 min 46 s + 5 min 11 s + 8 min 37 s = 15 min 34 s, rounded to 16. The gap from 10:55:46 to 16:07:21 is excluded: the work was paused until the human fixed a compile error caused by a third-party asset imported in the same project, and the Unity connection was restored. About a minute spent diagnosing that error right before the pause has no timestamp and is not counted. Tuning / testing includes running the asset builder, the play-mode checks, the tuning pass and the clean-up. To be confirmed by the human.

**Correction:** the total was corrected by the human to 8 min, the effective working time. The phase rows above are the raw timestamp spans and include waiting time (shared editor, pauses), so they no longer add up to the total.

## 5. Files changed
- `Assets/Features/GameFeel/Scripts/View/SpawnFeedbackView.cs`: new. Scales the entity root from 0 to full size with an ease-out-back curve and asks the service for the puff.
- `Assets/Features/GameFeel/Scripts/GameFeelService.cs`: preloads the puff prefab (same pattern as the spark), new `PlaySpawnPuff(position)`. `Load` now warns and skips when a reference is unassigned.
- `Assets/Features/GameFeel/Scripts/Config/GameFeelConfig.cs`: new `spawnPuffPrefab` reference and the `spawnDuration` / `spawnOvershoot` values.
- `EnemyView.cs`: new `PlaySpawn()`. `PlayDeath` first snaps the pop to full size.
- `EnemiesContainerView.cs`: calls `enemyView.PlaySpawn()` right after placing the new enemy.
- `BeeNormal.prefab`: `SpawnFeedbackView` added and assigned on `EnemyView`.
- `Assets/Features/GameFeel/View/Local/SpawnPuff/`: `SpawnPuff.prefab`, `SpawnPuffMaterial.mat`, `SpawnPuffTexture.png`.
- `Assets/Resources/GameFeelConfig.asset`, `AddressableAssetsData/AssetGroups/VFX.asset`: reference and tuning values, new entry in the existing group.

Gameplay controllers and states were not touched.

## 6. Technical flow
1. At startup `GameFeelService.Initialize` loads `VFX/SpawnPuff` through Addressables and caches it.
2. `EnemiesController` spawns the enemy and raises `OnEnemySpawned`. `EnemiesContainerView` instantiates the view, places it and calls `EnemyView.PlaySpawn()`.
3. `SpawnFeedbackView.Play()` sets the root scale to 0 in the same frame (so the first rendered frame is never full size), starts timing, and calls `GameFeelService.PlaySpawnPuff` at the feet.
4. The service instantiates the cached puff prefab. It is three small particle systems: a ring of dust puffs thrown outward on the ground, one flat glow disc that widens and fades, and a few sparkles that pop up and fall back. The prefab destroys itself when the particles end.
5. Each frame `SpawnFeedbackView` sets the root scale to `baseScale * EaseOutBack(progress)`. When the progress reaches 1 it disables itself.
6. The hit squash (`HitFeedbackView`) scales the model child, so it multiplies with the root pop instead of fighting it.
7. If the enemy is killed during the pop, `PlayDeath` calls `Finish()` first, which snaps the root to full size, so the death launch measures the body at its real size.

## 7. Tuning values
In `GameFeelConfig` (`Resources/GameFeelConfig`):
- `spawnDuration` = `0.4` s: time to go from nothing to full size.
- `spawnOvershoot` = `2.5`: how far the pop goes past full size (1.7 = +10%, 2.5 = +19%, 3 = +25%, 0 = none).

In the puff prefab (designer-edited in the prefab, not in code): colors, particle counts, ring radius, sparkle size.

## 8. Addressables
- **Used?** Yes.
- **Assets / addresses / group:** `SpawnPuff.prefab` at `VFX/SpawnPuff`, in the existing `VFX` group (local build and load path variables). Its material and texture are implicit dependencies, bundled with it.
- **Load:** once, in `GameFeelService.Initialize`, awaited before gameplay (UniTask, no `WaitForCompletion`).
- **Owner:** `GameFeelService`, in the same handle list as the other VFX.
- **Lifetime / cache:** loaded for the whole session. Every spawn is an `Instantiate` of the cached prefab, nothing loads at spawn time.
- **Release:** `GameFeelService.Reset` releases all handles.
- **Failure:** an unassigned or failing reference logs a warning and skips the puff. The enemy still pops in. Startup in play mode produced no warnings.
- **Remote readiness:** moving the `VFX` group to a CDN is a group configuration change. The load is awaited at startup, so a remote group would need the download hidden behind a loading step. No code change.
- **Why Addressables here:** the puff is new runtime-loaded content, so it follows the project rule (dedicated group by content type, address, one owner, preload, release) and can be replaced or served remotely without touching code.

## 9. Generated assets
- **Used?** Yes.
- **`SpawnPuffTexture.png`:** 64×64 soft white disc (solid core, smooth edge), written with a short script. Why: the particles need a soft round shape, and the built-in radial gradient left a visible edge. Integrated as the base map of the puff material. Replacement cost: very low (swap the texture on the material).
- **`SpawnPuffMaterial.mat`:** URP Particles/Unlit, transparent alpha blend, uses the texture. Replacement cost: very low.
- **`SpawnPuff.prefab`:** dust ring + ground glow + sparkles, lavender and cyan to stand out from the dark floor and the yellow bees, and to differ from the yellow-orange hit spark. Integrated as Addressable `VFX/SpawnPuff`, referenced from `GameFeelConfig`. Replacement cost: low (replace the prefab behind the same address).
- Time is included in implementation and testing. The material and prefab were built with a one-off editor script that was deleted after use (no generation pipeline kept).

## 10. Edge cases and limits
- **Killed during the pop:** `Finish()` snaps to full size before the death launch. Written and reviewed, not exercised in play mode.
- **Hit during the pop:** the squash and the pop scale different transforms, so they multiply. Not exercised in play mode.
- **Restart:** `ClearAllEnemies` destroys the views instantly, which also stops the pop.
- **Gameplay unchanged:** the enemy is in the controller from the first frame, so it moves, can be hit and can attack while it pops in (0.4 s).
- **Missing service or prefab:** `SpawnFeedbackView` skips the puff if the service is absent, the service skips it if the prefab did not load. The pop still plays.
- **Spawn location vs camera:** enemies spawn 10 units from the hero. In some of my captures that spot was off screen in the portrait view, so part of the effect plays out of sight. The captures were taken while the camera setup was being changed by other work, so this was not measured with the final camera. If the effect is seen too rarely, `EnemiesConfig.SpawnRadius` is the lever (a gameplay value, left untouched).
- **Measured in play mode:** pop peak 1.189 at 0.21 s, back to 1.000 at 0.40 s, puff destroyed at 0.70 s. Frames at 0.06, 0.15, 0.3 and 0.5 s were checked on a test bee placed beside the hero (before the last tuning pass). The final sparkle size was not re-checked visually.
- **Only `BeeNormal`** has the component. `BeeSmall` and `BeeStrong` are never spawned (`SpawnEnemy` always uses `Enemies[0]`), so they were left alone.
- **Deliberately not built:** a spawn telegraph before the enemy exists (would delay a gameplay spawn), a white materialize flash, a spawn sound, puff pooling, facing the hero from the first frame (the existing rotation slerp turns the bee during the pop).

## 11. Base project observations
- `EnemiesController.SpawnEnemy` always uses `Enemies[0]`.
- `EnemyView` only learns the hero controller after its first frame (it subscribes in `Start`), so a view cannot orient itself or read the hero at spawn time without extra wiring.
- An unassigned `AssetReference` makes Addressables log an `InvalidKeyException` as an error before any caller's `try/catch` runs, so a defensive `RuntimeKeyIsValid()` check is needed to keep the console clean.

## 12. Likely review questions
- **Why scale the entity root and not the model?** The model child already gets squashed by the hit feedback and moved by the death launch. Scaling the root composes with both through the transform hierarchy and needs no coordination. The health bar is not parented to the entity, so it is not affected.
- **Why code and not an Animator clip?** The pop is one curve with two designer values. A clip would need a new state, a trigger and transitions in the shared bee Animator controller, for a single scale curve. The code version is one small component with two designer values.
- **Why `ease-out-back`?** It is one line, it needs one parameter, and it gives the classic cartoon pop: it overshoots then settles. `spawnOvershoot` = 0 turns it into a plain ease-out.
- **Why not telegraph the spawn before the enemy appears?** That would delay the gameplay spawn, which is a game-rule change. This feature stays presentation-only: views react to the existing event.
- **Why Addressables and why not pool the puff?** The puff is new runtime content, so it follows the Addressables rules. At one puff every 2 seconds, instantiating the cached prefab and letting it destroy itself is simpler than a pool and has no visible cost.

## 13. 30-second explanation
Bees used to appear instantly at full size with nothing to mark the moment. Now the entity root scales from nothing with an ease-out-back overshoot, and a lavender dust puff with a ground glow and sparkles bursts at its feet. A small view component on the bee does the scaling, and the particle prefab is preloaded from the `VFX` Addressable group by the existing game feel service. Gameplay is untouched: the enemy is live from the first frame. The trade-off is that the effect is only visual, so it cannot warn the player before the enemy exists, and with a spawn radius of 10 part of it may play off screen.

## 14. README entry
Copied into `README.md` under "Features".

### Enemy spawn — 8 min
- **Implemented:** bees now pop in from nothing with a springy overshoot while a lavender dust puff, a ground glow and a few sparkles burst at their feet. Details in [the feature note](Documentation/FeatureNotes/08_EnemySpawn.md).
- **Time:** 8 min
- **Bottleneck:** not recorded.
- **Addressables:** the puff prefab is preloaded from the `VFX` group.
