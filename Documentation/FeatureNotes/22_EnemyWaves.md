# FEATURE: Enemy waves and the three bee types

## 1. Summary
Enemies now come in timed waves that start one second after the tap and ramp up for about 85 seconds. They spawn just outside the screen, more often from the top and bottom than from the sides. The three bees in the project each have their own behaviour: the small one is a kamikaze, the normal one attacks faster and lunges, and the strong one is a slow, huge tank. The camera zooms out when the crowd grows, and enemy views are pooled.

## 2. Why / issue fixed
- **Why (from the human):** the game felt empty and boring, and enemies were too easy to dodge.
- **Issue fixed (deduced from that statement):** one bee type trickled in at a fixed rate, so the first seconds had nothing to do and the pressure never rose. The normal bee committed to a long windup while standing still, so the hero walked out of its range without trying. The spawn system was also part of the game feel: enemies appeared on a circle around the hero, ignoring the 9:16 screen, so some popped in from the close left and right edges with no time to react.

> The issue wording is deduced from the human's statement, to be confirmed.

## 3. Bottleneck
Deduced from the human's statement, not given directly: making the spawns follow what the player actually sees. The camera is tilted, so the screen shows more ground above the hero than below, and the sides of a portrait screen are very close to him. The first spawns of a round also happen while the start-screen camera is still pulling back. Reading spawn points from the real camera, and waiting for the camera before the first wave, solved both.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `3 min 21 s` (11:02:21 → 11:05:42) |
| Implementation | `~8 min` (11:05:42 → ~11:14) |
| Tuning / testing | `~7 min` (~11:14 → 11:20:38) |
| Asset generation / integration | none |
| **Total** | `15 min` (human-stated) |

The total is the value stated by the human and is the one used in the README. The phase rows are the recorded timestamps (11:02:21 → 11:20:38, 18 min 17 s) and do not add up to it. No timestamp was taken when implementation ended: the split uses the time stamped on the first play-test screenshot (11:14), so those two rows are approximate. Testing is three 100-second automated play-test rounds on the shared editor, with tuning between them.

## 5. Files changed
- `EnemyWaveSpawner.cs`: new. Wave timeline, weighted enemy pick, spawn point outside the screen.
- `EnemyWave.cs`: new. Serializable wave definition (`EnemyWave`, `WaveEnemy`).
- `EnemyAssetsService.cs`: new `IService`. Loads the enemy prefabs through Addressables and owns the handles.
- `CrowdZoomView.cs`: new. Widens the camera's field of view with the number of enemies alive.
- `EnemiesController.cs`: the spawn loop moved out. New `SpawnEnemy`, lunge during the strike, kamikaze `Explode` and the `OnEnemyExploded` event.
- `EnemyState.cs`: `LungeTarget` and `WithStrike`.
- `EnemyConfig.cs`: `lungeDistance`, `explodesOnContact`, and the prefab is now an `AssetReferenceGameObject`.
- `EnemiesConfig.cs` and `EnemiesConfig.asset`: waves, round start delay, edge weights and margin. `spawnInterval` and `maxEnemies` were removed (each wave has its own).
- `EntitiesService.cs`: creates, restarts and resets the spawner. Depends on `EnemyAssetsService`.
- `EnemiesContainerView.cs`: one `ObjectPool<EnemyView>` per enemy type, and the explode reaction.
- `EnemyView.cs`: `Appear()` resets a pooled view, `PlayExplode()`, no more `Destroy` on death.
- `HitFeedbackView.cs`: `Stop()`, so a reused body loses any flash, squash or recoil from its last life.
- `GameFeelConfig.cs`: four crowd zoom values.
- `BeeNormal.asset`, `BeeSmall.asset`, `BeeStrong.asset`: new stats. `BeeSmall.prefab` and `BeeStrong.prefab`: scale, and health bar height for the strong one.
- `World.prefab`: `CrowdZoomView` added. `Game.Entities.asmdef`: Addressables references.
- `Enemies` Addressables group (new).

## 6. Technical flow
1. Startup: `EnemyAssetsService` loads the three enemy prefabs in parallel and keeps each one's `EnemyView`.
2. `EntitiesService` creates the `EnemyWaveSpawner`. Its loop only counts time while the game is `Playing` and the hero is alive, using `Time.deltaTime`, so the start screen, game over and freeze frames pause it.
3. Tap: after `roundStartDelay` (1 s, the camera pull-back), wave 1 starts with its burst.
4. Each wave: the burst is spawned in groups of `groupSize`. After that, one group spawns every `spawnInterval`, as long as fewer than `maxAlive` enemies are alive. After `duration` the next wave starts. The last wave never ends.
5. Each enemy is a weighted random pick among the wave's entries whose prefab is loaded.
6. Each group gets one spawn point. The spawner picks an edge by weight (top 5, bottom 3, sides 2), then a random point along it in screen coordinates. It casts a ray from the camera through that point onto the ground, then pushes the point `offscreenMargin` further out from the hero. Group members are spread around that point by `groupSpread`. With no camera, it falls back to a circle around the hero.
7. `EnemiesController.SpawnEnemy` adds the enemy and raises `OnEnemySpawned`. `EnemiesContainerView` takes a view from that type's pool and calls `Appear()`: it resets the view, faces the hero, updates the health bar and plays the spawn pop.
8. Behaviours (`EnemiesController.UpdateEnemy`):
   - Chase until within `attackRange`.
   - Kamikaze: hit the hero immediately, raise `OnEnemyAttacked` and `OnEnemyExploded`, and remove the enemy. There is no `OnEnemyKilled`, so no orbs, no damage number and no kill freeze.
   - Others: windup (red disc sized to range plus lunge), then strike. At the start of the strike the enemy locks a lunge target where the hero stands, stopping 0.8 m short and dashing at most `lungeDistance`. The dash takes the first half of the strike. At the end of the strike the hero is hit if he is within `attackRange`.
9. Death: the view plays its death and goes back to the pool after `deathDuration`. Explosion: puff and spark, back to the pool at once. A restart clears the alive enemies (back to the pool) and calls `Restart()` on the spawner.
10. `CrowdZoomView` eases the virtual camera's field of view between 60° and 72° depending on the number of enemies alive. The spawner reads the real camera, so the spawn points move out with the zoom.

## 7. Tuning values
`EnemiesConfig`:
- `waves`: five waves. Times are from the tap, including the 1 s start delay.

| Wave | Starts | Burst | Interval | Group | Max alive | Enemies (weight) |
|---|---|---|---|---|---|---|
| 1 | 1 s | 3 | 1.2 s | 1 | 6 | normal |
| 2 | 13 s | 4 | 1.0 s | 2 | 10 | normal 3, small 1 |
| 3 | 31 s | 5 | 0.9 s | 2 | 14 | normal 3, small 2, strong 0.5 |
| 4 | 56 s | 6 | 0.8 s | 3 | 20 | normal 3, small 2, strong 0.6 |
| 5 | 86 s, endless | 6 | 0.6 s | 3 | 28 | normal 3, small 3, strong 0.8 |

- `roundStartDelay` = `1`: seconds between the tap and wave 1, so the camera has pulled back to the gameplay view.
- `offscreenMargin` = `2`: meters beyond the screen edge. Below about 1.5, group members can appear at the corners.
- `topEdgeWeight` = `5`, `bottomEdgeWeight` = `3`, `sideEdgeWeight` = `2`: where enemies come from.
- `groupSpread` = `1.2`: spacing inside a group.
- `spawnRadius` = `10`: fallback circle when there is no camera.

Bee configs:

| | Normal | Small | Strong |
|---|---|---|---|
| Health | 60 | 25 | 180 |
| Speed (hero 7) | 4 | 9 | 2.2 |
| Damage | 3 | 6 | 20 |
| Attack range | 2.3 | 1 | 2.8 |
| Windup | 0.35 s | none | 0.9 s |
| Lunge | 1 m | none | none |
| Cooldown | 0.8 s | none | 1.5 s |
| Explodes on contact | no | yes | no |
| Prefab scale | 1 | 0.65 | 1.9 |

`GameFeelConfig` crowd zoom:
- `crowdZoomStartCount` = `12`, `crowdZoomFullCount` = `24`: the crowd size where the zoom starts and is complete.
- `crowdZoomFovIncrease` = `12`: degrees added at full zoom (0 disables it).
- `crowdZoomSmoothTime` = `1.5`: slow, so the camera breathes instead of pumping with each kill.

## 8. Addressables
- **Used?** Yes.
- **Assets / addresses / group:** `Enemies/BeeNormal`, `Enemies/BeeSmall`, `Enemies/BeeStrong` in a new `Enemies` group (pack together, `LocalBuildPath` / `LocalLoadPath`). `EnemyConfig.prefab` is an `AssetReferenceGameObject`.
- **Load:** in `EnemyAssetsService.Initialize`, at startup, all in parallel with `UniTask.WhenAll`. Services finish initializing before the start screen accepts a tap, so nothing loads during gameplay.
- **Owner:** `EnemyAssetsService` (an `IService`, found by `ServicesLocator`). `EntitiesService` depends on it.
- **Lifetime / cache:** loaded once and kept for the session. Enemies spawn all game long. The prefab is the template for the view pools, so instances are reused, not reloaded or re-instantiated.
- **Release:** `EnemyAssetsService.Reset` releases every handle.
- **Failure:** a missing reference, a failed load or a prefab without `EnemyView` logs a warning and that type is left out. The spawner only picks loaded types, so a wave keeps running with what is available. If nothing loads, a warning says no enemy will spawn and the game still runs.
- **Remote readiness:** switching the group's paths to `RemoteBuildPath` / `RemoteLoadPath` in the profile is enough. No code changes, and the parallel startup load already hides download time. The base project's `Local/` and `Remote/` folders suggest the small and strong bees were meant to be downloaded later. They were kept in one group on purpose: all three share the BeeCommon mesh, animations and texture, and splitting them would duplicate those into two bundles, unless BeeCommon got its own group first.
- **Why Addressables here:** enemy content is exactly what a live game adds and updates from a CDN. Before, `EnemiesConfig` sat in `Resources` with direct prefab links, which forced every bee into the build.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- **Start camera:** the start screen camera is close and low. Spawning right at the tap would project the screen edges near the hero, so `roundStartDelay` waits for the pull-back (measured: gameplay framing by 0.8 s).
- **An edge ray misses the ground** (the camera looking at the horizon): falls back to the circle.
- **Time:** waves count gameplay time, so the start screen, game over and hit stop pause them. A restart goes back to before wave 1.
- **Pooling:** a dying view is out of the active list but not in the pool until its death animation ends, so it can never be reused mid-death. The Animator resets itself when the object is deactivated. `Appear()` resets the death launch, hit feedback and telegraph. The health bar is kept with its view and refilled.
- **Kamikaze hit that kills the hero:** `TakeHit` ends the game, then the bee is removed as usual.
- **Not built:** enemies don't push each other apart (strong bees overlap around the hero), there is no wave announcement UI, no pool prewarm (pools grow on demand, about 20 views at most), and no despawn of enemies left far behind.

## 11. Base project observations
- Enemies have no separation, so crowds and the big strong bee stack on the hero.
- `EntitiesService.Reset` doesn't call `Reset` on its controllers, so their update loops are never cancelled.
- The original spawn loop always spawned `Enemies[0]`. The small and strong bees were configured but never used.
- Enemy prefabs were direct references from a `Resources` config, despite the `Local/` and `Remote/` folder split.

## 12. Likely review questions
- **Why does gameplay code read `Camera.main`?** Spawn placement is about what the player sees, so the camera is the right source. The spawner only reads it and never moves it, and there's a circle fallback, so the game still works without a camera.
- **Why a separate spawner instead of keeping the loop in `EnemiesController`?** Two responsibilities: the controller runs the enemies that exist, the spawner decides when, what and where. Changing the pacing never touches combat code.
- **Why Unity's `ObjectPool` and not a custom pool?** It's built in, well known and enough. The only real work is resetting the view, which `EnemyView.Appear()` does in one place.
- **Why one Addressables group when the folders say Local / Remote?** The three bees share dependencies. Two groups would duplicate the common mesh and textures in both bundles. The group can still go remote with a profile change.
- **How does the normal bee get harder to dodge but stay fair?** A shorter windup and a lunge toward where the hero stood when the strike began. Standing still gets you hit; stepping away when the red disc shows still dodges.

## 13. 30-second explanation
The game felt empty and enemies were easy to dodge. Now waves start one second after the tap and ramp up for about a minute and a half, mixing a fast kamikaze bee, a normal bee that commits and lunges, and a slow, huge tank. A small spawner class owns the pacing and places spawns just outside what the camera shows, mostly from the top and bottom, because portrait sides are too close to react. The camera zooms out when the crowd grows, views are pooled, and the bee prefabs come from an `Enemies` Addressables group preloaded at startup. The main trade-off: spawn placement reads the real camera, which keeps it simple and zoom-aware but needs a short delay while the start camera pulls back.

## 14. README entry

### Enemy waves and bee types — 15 min
- **Implemented:** the game felt empty and enemies were too easy to dodge. Enemies now come in waves that start one second after the tap and ramp up, from just outside the screen and rarely from the close portrait sides. Small bees are fast kamikazes, normal bees attack sooner and lunge so they actually land hits, and strong bees are huge, slow tanks. The camera zooms out when the crowd grows, and enemy views are pooled. Details in [the feature note](Documentation/FeatureNotes/22_EnemyWaves.md).
- **Time:** 15 min
- **Bottleneck:** placing the spawns around what the player actually sees on a tilted portrait camera (to be confirmed).
- **Addressables:** the three bee prefabs are preloaded from a new `Enemies` group.
