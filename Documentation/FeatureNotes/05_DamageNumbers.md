# FEATURE: Damage Numbers

## 1. Summary
Hitting an enemy pops a floating damage number above it, using the third-party package asset. The number is chunky and outlined, with a yellow-to-orange gradient. It slams in, drifts up and sideways with a random tilt, then shrinks and fades. Heavier weapons print bigger numbers, and the hit that kills prints a bigger, red-orange one.

## 2. Why / issue fixed
- **Why:** add juice at the moment of impact, with a damage text that design features can build on later to highlight it more (critical damage, scaling damage).
- **Issue fixed:** the game was missing juice at impact.

> Deduced by the agent from the human's one-sentence statement ("missing juice at impact", "can later be used with design features such as critical damage and scaling damage"). To confirm or refine before the review.

## 3. Bottleneck
Getting a number on screen was quick (implementation took less time than checking). Most of the time went into validating it in play mode: the Unity editor was shared with other sessions, and their recompiles interrupted the test runs. Before anything could run, the imported third-party package also had to be made to compile on this Unity and TMP version.

> The human did not state a bottleneck. This one is taken from the recorded work, not from their words. To confirm or replace before the review.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `5 min` (10:56:58 to 11:02:00) |
| Implementation | `8 min` (11:02:00 to 11:10:28) |
| Tuning / testing | `13 min` (11:10:28 to 11:23:26) |
| Asset generation / integration (if any) | included above (no generated art) |
| **Total** | `9 min` (human-corrected, see below) |

Real elapsed time from recorded timestamps (10:56:58 to 11:23:26 = 26 min 28 s). Analysis includes the compile fix of the imported asset. Tuning / testing includes waiting for the shared Unity editor, so it is an upper bound.

**Correction:** the total was corrected by the human to 9 min, the effective working time. The phase rows above are the raw timestamp spans and include waiting time (shared editor, pauses), so they no longer add up to the total. No revised phase breakdown was supplied.

## 5. Files changed
- `Assets/Features/GameFeel/Scripts/GameFeelService.cs`: loads the damage number prefab (Addressables), prewarms its pool, `PlayDamageNumber(hitPoint, damage, isKillingBlow)` spawns one.
- `Assets/Features/GameFeel/Scripts/Config/GameFeelConfig.cs`: prefab reference and the "Damage numbers" tuning values.
- `Assets/Features/GameFeel/Scripts/View/HitFeedbackView.cs`: `Play(damage, isKillingBlow)` also asks for a number when a damage value is given.
- `Assets/Features/GameFeel/Scripts/Game.GameFeel.asmdef`: references `ThirdPartyPackage`.
- `Assets/Features/GameFeel/View/Local/DamageNumber/`: `DamageNumber.prefab` and `DamageNumberMaterial.mat`.
- `Assets/Resources/GameFeelConfig.asset`: new references and values.
- `EnemiesController.cs`: `OnEnemyDamaged` and `OnEnemyKilled` now also carry the damage dealt (`Action<int, int>`). No damage rule changed.
- `EnemiesContainerView.cs`, `EnemyView.cs`: pass the damage through to `PlayHit` and `PlayDeath`.
- `Assets/3rdParty/ThirdPartyPackage/Scripts/Internal/Editor/EditorInternal.cs`: two lines patched, see section 11.

## 6. Technical flow
1. At startup `GameFeelService.Initialize` loads the `VFX/DamageNumber` prefab through Addressables, reads its `DamageNumber` component and calls `PrewarmPool()` (12 numbers).
2. The hero hits an enemy: `EnemiesController.AttackEnemy` raises `OnEnemyDamaged(id, damage)`, or `OnEnemyKilled(id, damage)` for the hit that kills.
3. `EnemiesContainerView` forwards the damage to `EnemyView.PlayHit` / `PlayDeath`, which call `HitFeedbackView.Play(damage)` (`isKillingBlow: true` on death).
4. `HitFeedbackView` already plays the flash, squash and spark. With a damage above 0 it also calls `GameFeelService.PlayDamageNumber` at the body center.
5. The service spawns one number from the pool, a little above the hit point, then sets the gradient (hit or kill colors) and the size on every spawn, because pooled numbers keep the look of their previous use.
6. The prefab's own settings run the motion: slam-in, drift, fade-out. Its scale-by-number setting makes heavier weapons print bigger. The number draws after the health bars and through scenery.
7. The number returns to the pool when it has faded out. The hero's own hits call `Play()` with no damage, so no number.

## 7. Tuning values
In `GameFeelConfig` (Resources), "Damage numbers":
- `damageNumberHeight` = `0.5`: world units above the body center where the number starts.
- `hitNumberScale` = `1.8`: size of a regular number.
- `killNumberScale` = `1.5`: size of the killing-blow number relative to a regular one.
- `hitNumberTopColor` / `hitNumberBottomColor` = yellow / orange: gradient of a regular number.
- `killNumberTopColor` / `killNumberBottomColor` = orange-red / deep red: gradient of the killing blow.

On `DamageNumber.prefab` (third-party package inspector):
- `lifetime` = `0.8`: seconds before the fade-out starts.
- Fade in `0.2` s from 1.8x size, with alternating digit stretch (1 x 1.35): the slam-in.
- Fade out `0.25` s down to 0.55x size.
- Lerp: speed `6`, sideways `0.2` to `0.55` (random side), up `0.9` to `1.25`: the drift.
- Start rotation `5` to `12` degrees, random side.
- Scale by number: damage `15` gives 1x, damage `50` gives 1.35x.
- Pool size `12`.

On `DamageNumberMaterial.mat`: face dilate `0.2`, outline width `0.3`, near-black outline, soft black drop shadow, render queue `3200`.

## 8. Addressables
- **Used?** Yes
- **Assets / addresses / group:** `DamageNumber.prefab`, address `VFX/DamageNumber`, group `VFX` (existing, uses the profile build and load path variables). Its material, the third-party package font asset and scripts come with it as dependencies.
- **Load:** once, in `GameFeelService.Initialize`, before any hit can happen.
- **Owner:** `GameFeelService` holds the handle, in its existing handle list.
- **Lifetime / cache:** the prefab is loaded for the whole session. third-party package instantiates and pools 12 copies from it at startup, and every hit reuses one. No loads and no instantiates on a hit in normal play.
- **Release:** `GameFeelService.Reset` (called by `ServicesLocator.OnDestroy`) clears the pooled copies first, then releases the handle.
- **Failure:** a missing reference or a failed load logs a warning and disables only the numbers. A prefab without a `DamageNumber` component does the same. `PlayDamageNumber` then returns at once, and gameplay is not affected.
- **Remote readiness:** the group already uses the profile path variables, so moving it to a CDN is a group configuration change. The service awaits the load at startup like the other feedback assets, so a remote group would need the download hidden behind a loading step. The third-party package font asset is not in a group of its own, so it is pulled into the same bundle as an implicit dependency.
- **Why Addressables here:** the number is new runtime content, so it follows the project rule: a dedicated group, an address, one owner, preload, pool and release.

## 9. Generated assets
- **Used?** No generated art.
- Two small assets were made in the editor: `DamageNumber.prefab` (a copy of the third-party package "Comic" demo prefab, with its settings changed and the Liberation Sans outline font) and `DamageNumberMaterial.mat` (a copy of that font's material with a thicker outline, a drop shadow and a higher render queue). Only the prefab is an Addressable entry (group `VFX`), the material comes with it as a dependency. Replacement cost: low, the look is the prefab and one material. Time is included in implementation and tuning.

## 10. Edge cases and limits
- Prefab missing, failed to load, or without the component: no numbers, one warning, game runs.
- A killing blow shows the full damage dealt, even when the enemy had less health left.
- Numbers are independent of the enemy, so they stay and finish their animation when the enemy dies or is destroyed.
- The hero never shows a number for its own hits (`Play()` without damage).
- Numbers reuse pooled copies, so `SetGradientColor` and `SetScale` run on every spawn.
- Checked in play mode from captured frames of a running game. The editor ran below real time while unfocused, so the slam-in timing was not seen live. To confirm in the Game view.
- Not built: critical hits and damage variation (the game has none, a weapon's damage is constant), numbers on the hero when hit, combining or stacking numbers (hits are far enough apart), a color flash on spawn.

## 11. Base project observations
- `OnEnemyDamaged` and `OnEnemyKilled` only carried the enemy id, so a view could not know the damage. They now carry it. Reading the weapon's damage in the view would break as soon as damage is not the weapon's value (armor, critical hits).
- A weapon's damage is constant (15, 30 or 50), so there is nothing to scale within one weapon yet. The damage numbers already scale between weapons.
- The imported third-party package targets a newer TMP (3.2, Unity 6) and did not compile on Unity 2022.3 with TMP 3.0.9: `TextWrappingModes` and `textWrappingMode` do not exist. This blocked play mode for the whole project. Two editor-only lines in `EditorInternal.cs` were changed to `enableWordWrapping = false`. The patch is lost if the asset is reimported or updated.
- third-party package keeps non-serializable state (a `HashSet` in its updater), so a script recompile during play mode breaks it until play mode restarts. This only matters in the editor.

## 12. Likely review questions
1. **Why a third-party asset and not a small number component?** third-party package gives pooling, camera facing, fades, scale by number and combination for free, and it is the asset the project provides. The trade-off is a dependency that needed a two-line patch and that the team must keep compiling.
2. **Why pass the damage through the events and not read the weapon in the view?** The damage dealt is a gameplay fact. A view that reads the weapon would show the wrong value once armor or critical hits exist. The event is the one place that knows the real value.
3. **Why spawn from `GameFeelService` and not from a component on each enemy?** One preloaded, pooled template for all hits. Enemies hold no VFX and nothing loads on a hit. It follows the existing spark and spawn-puff pattern.
4. **How does the number draw above the health bars?** The bars draw at queue `Transparent + 100`. The number uses a copy of the font material at queue `3200`, and third-party package's "render through walls" keeps it from sinking into the scenery.
5. **Why set the color and size on every spawn?** Pooled numbers keep what the previous use left on them, so a regular number could come out in kill colors.

## 13. 30-second explanation
Hits had no juice at the moment of impact. Now every hit on an enemy pops a damage number: chunky, outlined, with a color gradient. It slams in, drifts, and fades, bigger for heavier weapons and red-orange and bigger for the hit that kills. The enemy events now carry the damage, `HitFeedbackView` asks `GameFeelService` for a number, and the service spawns it from a pool of third-party package prefabs preloaded through Addressables. Motion and fades are tuned on the prefab, colors and sizes in `GameFeelConfig`, so critical hits or scaling damage can reuse it later. The trade-off is a third-party dependency that needed a compile patch.

## 14. README entry
Copied into `README.md` under "Features".

### Damage numbers — 9 min
- **Implemented:** every hit on an enemy pops a floating damage number: chunky, outlined, with a color gradient. It slams in, drifts and fades. It is bigger for heavier weapons, and the hit that kills prints a bigger, red-orange number. Built on third-party package, so critical hits or scaling damage can reuse it later. Details in [the feature note](Documentation/FeatureNotes/05_DamageNumbers.md).
- **Time:** 9 min
- **Bottleneck:** validating in play mode on a Unity editor shared with other sessions, after making the imported third-party package compile.
- **Addressables:** the damage number prefab is preloaded and pooled from the `VFX` group.
