# FEATURE: Health Bars

## 1. Summary
Every entity (hero and enemies) gets a world-space health bar floating above its head. The hero bar is green and bigger, enemy bars are red and only appear once the enemy is damaged. Damage is animated (sliding fill, white flash, size pop, lagging yellow trail) and the bar disappears when the entity dies.

## 2. Why / issue fixed
- **Why:** increase the player's understanding of the game, especially the hero state and the enemy state.
- **Issue fixed:** game feel and feedback were lacking.

## 3. Bottleneck
The player had no feedback on enemy health.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `3 min` (10:27:14 to 10:30:06) |
| Implementation | `2 min` (10:30:06 to 10:31:58) |
| Tuning / testing | `2 min` (10:31:58 to 10:34:25) |
| Feedback iterations | `10 min` (10:34:25 to 10:43:58) |
| Asset generation / integration (if any) | included above (no generated art) |
| **Total** | `9 min` (human-corrected, see below) |

Real elapsed time from recorded timestamps (10:27:14 to 10:43:58 = 16 min 44 s). The feedback iterations row covers three changes requested after the first delivery (bigger and higher hero bar, hide enemy bars at full health, draw bars on top of meshes). It also includes the review time between rounds and about 1 min spent waiting for an unrelated config asset from other work, so it is an upper bound.

**Correction:** the total was corrected by the human to 9 min, the effective working time. The phase rows above preserve the original timestamp spans, including review and waiting time, and do not add up to the corrected total. No revised phase breakdown was supplied.

## 5. Files changed
- `Assets/Features/HealthBar/Scripts/HealthBarService.cs`: `IService` that preloads the Addressable prefab and creates bars.
- `Assets/Features/HealthBar/Scripts/View/HealthBarView.cs`: the bar itself (follow target, face camera, animate health).
- `Assets/Features/HealthBar/Scripts/Config/HealthBarConfig.cs`: ScriptableObject singleton with prefab reference, colors, sizes, timings.
- `Assets/Features/HealthBar/Scripts/Game.HealthBar.asmdef`: new assembly.
- `Assets/Features/HealthBar/View/Local/HealthBar/`: `HealthBar.prefab`, `HealthBarOverlay.shader`, `HealthBarOverlay.mat`.
- `Assets/Resources/HealthBarConfig.asset`: the config instance.
- `HeroView.cs`: creates the hero bar, forwards health from `OnStateChanged`, has a `healthBarHeight` field.
- `EnemyView.cs`: creates the enemy bar, updates it on hit, hides it on death, has a `healthBarHeight` field.
- `EnemiesContainerView.cs`: reads the enemy health from the controller and hands it to the view.
- `Game.Entities.asmdef`: references `Game.HealthBar`.

Gameplay controllers and states were not touched.

## 6. Technical flow
1. At startup `HealthBarService.Initialize` loads the `UI/HealthBar` prefab through Addressables and caches it. A failure is only a warning.
2. When the hero view or an enemy view appears, it asks the service for a bar (`Create(target, height, team)`). The service instantiates the cached prefab and binds the team color, scale and hide-when-full rule.
3. The views push health into the bar: `SetHealth(health, maxHealth)`. The hero view does it on every `OnStateChanged`, the enemy view on `PlayHit` (the container reads the new health from `EnemiesController.Enemies`).
4. First value, revive and heal snap straight to the value. A lower value starts the hit feedback: the fill slides down, flashes white, the bar pops in size, and a yellow trail stays at the old value for a moment then drains.
5. Each frame (`LateUpdate`) the bar follows its target, offset along the camera up axis, and copies the camera rotation. If the target no longer exists the bar destroys itself.
6. At health 0 the bar is hidden (hero via `SetHealth`, enemy via `PlayDeath`). A hero restart sends full health again and the bar comes back without animation.

## 7. Tuning values
In `HealthBarConfig` (Resources):
- `heroScale` = `1.5`: size of the hero bar, bigger so the player's health reads first.
- `enemyScale` = `1`: size of enemy bars.
- `hideEnemyBarWhenFull` = `true`: enemy bars only show after damage. The hero bar is always visible.
- `heroColor` = green, `enemyColor` = red: fill colors per team.
- `trailColor` = yellow: the lost-health trail.
- `flashColor` = white: hit flash color.
- `fillSmoothTime` = `0.12`: seconds for the fill to slide to the new value.
- `trailDelay` = `0.4`: seconds the trail waits before draining.
- `trailDrainSpeed` = `1.5`: trail drain speed in full bars per second.
- `flashDuration` = `0.12`: seconds of the white flash.
- `punchScale` = `0.3`: extra size at the instant of a hit.
- `punchDuration` = `0.25`: seconds to settle back.

On the views:
- `HeroView.healthBarHeight` = `2.1`, `EnemyView.healthBarHeight` = `1`: screen-space height above the pivot.

## 8. Addressables
- **Used?** Yes
- **Assets / addresses / group:** `HealthBar.prefab`, address `UI/HealthBar`, group `UI` (new, local build and load path variables). Its shader and material are dependencies of the prefab.
- **Load:** once, in `HealthBarService.Initialize`, before any view asks for a bar.
- **Owner:** `HealthBarService` holds the `AsyncOperationHandle`.
- **Lifetime / cache:** loaded for the whole session. Every bar is an `Instantiate` of the cached prefab, no per-hit loads.
- **Release:** `HealthBarService.Reset` (called by `ServicesLocator.OnDestroy`) releases the handle.
- **Failure:** warning logged, `Initialize` still returns true, `Create` returns null, views skip the bar. Gameplay is not affected.
- **Remote readiness:** the group uses the profile local path variables, so moving it to remote is a group configuration change. The service already loads asynchronously, but it awaits at startup, so a remote group would need the download hidden behind a loading step. The prefab also depends on the `WhiteSquare` sprite, which lives in `ShapesAtlas` (Default Local Group).
- **Why Addressables here:** the bar is new runtime content, so it follows the project rule: a dedicated group, an address, one owner, preload and release.

## 9. Generated assets
- **Used?** No generated art. The bar is built from the existing `WhiteSquare` sprite.
- Small code assets were added: `HealthBarOverlay.shader` and `HealthBarOverlay.mat` (unlit sprite shader with `ZTest Always`, so bars draw on top of meshes). Replacement cost: low, the material can be swapped on the prefab. Time is included in the feedback iterations.

## 10. Edge cases and limits
- Prefab missing or without `HealthBarView`: no bars, one warning, game runs.
- Target destroyed (enemy death delay, restart clearing enemies): the bar destroys itself.
- Revive or heal: snaps, never animates upward.
- Rapid hits: each hit restarts the trail delay, so the trail catches up once the fighting pauses.
- Hero death hides the bar immediately, so the final hit is not animated.
- Not built: floating damage numbers, bar color changes at low health, bar pooling (a bar is created and destroyed with its entity).
- The bar ignores depth on purpose, so it can draw over walls or props that sit between the camera and an entity.

## 11. Base project observations
- `OnEnemyDamaged` only carries the enemy id, so views have to read the new health from the controller.
- `HeroController.OnStateChanged` fires on every movement frame, not only on health changes, so listeners must filter.
- `EnemiesController.SpawnEnemy` always uses `Enemies[0]`, the other two enemy configs are never spawned.
- Enemy and hero prefabs are referenced directly from configs, only weapons and the shapes atlas are Addressable.

## 12. Likely review questions
1. **Why not a world-space Canvas?** Three sprite renderers are cheaper than a Canvas per entity (no canvas rebuilds) and scaling a left-anchored sprite is simple.
2. **Why is the bar not parented to the entity?** The bee prefabs have different scales and the entities rotate. Following a target and copying the camera rotation avoids scale and rotation inheritance, and the bar destroys itself when the target is gone.
3. **Why offset along the camera up axis?** The camera is steeply tilted, so a world-up offset put the bar on top of the head. Camera-up gives the same screen distance at any tilt.
4. **How do bars draw over meshes?** A small unlit shader with `ZTest Always` in the `Transparent+100` queue, and a `SortingGroup` so pieces of two bars do not interleave.
5. **Why Addressables for such a small prefab, and what if the load fails?** It is new runtime content, so it follows the rule: preload at startup, one owner, release on reset. On failure the game logs a warning and runs without bars.

## 13. 30-second explanation
The player could not tell how hurt the hero or the enemies were. Each entity now has a world-space bar: green and larger for the hero, red for enemies, enemies only after the first hit. A hit slides the fill down with a flash, a pop and a lagging yellow trail so the damage does not feel instant, and the bar disappears on death. Views push health into a small bar component, the prefab is preloaded through Addressables by a service, and gameplay code is untouched. The trade-off is that the bar ignores depth so it is always readable, even when something is in front of it.

## 14. README entry
Copied into `README.md` under "Features".

### Health bars — 9 min
- **Implemented:** every entity has a health bar above its head: green and bigger for the hero, red for enemies (shown once damaged). Hits are animated with a sliding fill, flash, pop and a lagging trail, and the bar disappears on death.
- **Time:** 9 min
- **Bottleneck:** the player had no feedback on enemy health.
- **Addressables:** the bar prefab is preloaded from a new `UI` group.
