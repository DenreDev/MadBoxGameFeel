# FEATURE: Attack cone and range display

## 1. Summary
The sword now only hits enemies inside a cone in front of the hero (120° by default), instead of everything around him within range. The cone is drawn on the ground as a faint fan that pulses once when the slash lands.

## 2. Why / issue fixed
The player attack was missing key information on what was going to happen. The hero hit enemies behind him and nothing on screen showed the reach or the direction of the attack, so the player could not tell what would be hit, or why something was hit.

> Deduced from the human's statement ("the player attack was missing key information on what was going to happen"). Not confirmed word for word.

## 3. Bottleneck
Not given by the human. Deduced from the work: the Unity MCP bridge was down, so the new material, the Addressables entry, the config reference and the prefab component were written by hand as asset files. The result compiled and imported, but it was not seen in play mode.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | not timestamped |
| Implementation | `1 min 47 s` (10:28:49 → 10:30:36, first to last file written) |
| Tuning / testing | `34 s` (10:30:36 → 10:31:10, compile and import check only) |
| Asset generation / integration | included in implementation |
| **Total** | `2 min 21 s` (from 10:28:49) |

Real elapsed time from file timestamps and the editor log. The analysis (finding the hit rule, reading the hero, config and Addressables code) was not timestamped, so it is not counted. There was no play-mode test, so there was no tuning.

## 5. Files changed
- `WeaponConfig.cs`: new `ArcAngle` (default 120°), the width of the hit cone per weapon.
- `HeroController.cs`: keeps the aim direction toward the swing target, refreshes it when the slash lands, and only hits enemies inside the cone.
- `AttackRangeView.cs` (new): builds the fan mesh from the weapon's range and angle, shows and hides it, pulses it on the slash.
- `GameFeelConfig.cs`, `GameFeelService.cs`: the Addressable material reference and its loading, plus the display tuning values.
- `Alice.prefab`: the new component on the hero.
- `AttackRange.mat`: unlit transparent material, tint only.

## 6. Technical flow
1. At startup `GameFeelService` loads the material `VFX/AttackRange`.
2. `AttackRangeView` creates a child object under the hero and builds the fan from the current weapon. It is rebuilt when the weapon changes.
3. The hero picks the closest enemy as before and starts the swing: the aim direction is set toward it.
4. When the slash lands, the aim is refreshed from the target's current position (a target that died keeps the last aim).
5. Enemies are hit when they are closer than the range and the angle between the aim and the enemy is at most half the arc. An enemy on top of the hero is always hit. The max targets rule is unchanged.
6. `OnAttacked` makes the cone pulse once, hit or miss.
7. The cone is a child of the hero, so it turns with him. It is hidden on the start screen and when he is dead.

## 7. Tuning values
- `arcAngle` (WeaponConfig) = `120`: width of the cone in degrees, per weapon. 360 gives the old behavior.
- `attackRangeFillAlpha` = `0.14`: fill strength at the far end, fading to nothing at the hero's feet.
- `attackRangeEdgeAlpha` = `0.45`: strength of the thin arc at the exact reach.
- `attackRangeEdgeWidth` = `0.1`: thickness of that arc.
- `attackRangePulseDuration` = `0.25`: seconds of the pulse (0 disables it).
- `attackRangePulseBoost` = `3`: how much stronger the cone is at the start of the pulse.
- `attackRangePulsePop` = `0.06`: how much bigger it is at the start of the pulse.
- The tint is the material's Base Color.

## 8. Addressables
- **Used?** Yes.
- **Assets / addresses / group:** `VFX/AttackRange` (material), in the existing `VFX` group.
- **Load:** at `GameFeelService.Initialize`, before gameplay.
- **Owner:** `GameFeelService` (handle in its list).
- **Lifetime / cache:** loaded once. The view reads the tint once and shares the material, the pulse uses a property block.
- **Release:** in `GameFeelService.Reset`.
- **Failure:** a missing or failed load logs a warning and the cone is not shown. The hit rule does not depend on it.
- **Remote readiness:** the `VFX` group uses the profile path variables, so moving it to a remote profile is a configuration change.
- **Why Addressables here:** same content type and group as the other ground effects, loaded the same way.

## 9. Generated assets
- **Used?** No new generated file besides the material. The cone shape is a mesh built in code from the weapon values, so there is no texture to replace.

## 10. Edge cases and limits
- Auto-aim is still 360° and picks the closest enemy. The hero turns to it, so "in front" means toward that target. Restricting who he can target would be a different change.
- The rule uses the enemy's center, like the old range check. The drawn edge is the exact limit, an enemy half over the edge is not hit.
- The cone follows the hero's visible facing, the rule follows the exact aim. They can differ by a few degrees at the instant the hero is still turning.
- Only the rule and the Addressable setup were checked by compiling and importing. Not verified in play mode: how the cone looks, whether it lines up with the model's front, and whether the strengths are discreet enough.
- Not built: highlighting the enemies that are inside the cone before the slash.

## 11. Base project observations
- The hit rule had no notion of direction, only distance.
- `Alice.prefab` still holds a serialized `attackFacingDuration` that no script field uses anymore.

## 12. Likely review questions
- **Why is the cone centered on the target and not on the hero's transform?** The hero turns to the target during the swing, so both agree when the slash lands. The controller is plain C# and has no transform.
- **Why does the rule live in the controller and the drawing in a view?** Presentation never drives gameplay. The view only reads the same weapon values.
- **Why a mesh built in code and not a texture?** The range and angle are per weapon. A mesh stays exact for any value without a new asset.
- **Why a property block for the pulse?** The material is shared and Addressable, changing it would change it for everyone.
- **Why 120° by default?** A readable sweep in front of the hero that still leaves the flanks safe. It is a per-weapon value to tune.

## 13. 30-second explanation
The player could not see what the attack would hit, and it also hit enemies behind the hero. The sword now has a cone angle per weapon, and the hit rule only takes enemies inside it, centered on the enemy the hero aims at. A faint fan on the ground shows that cone and pulses when the slash lands. The rule is in the controller, the drawing in a view, and the material is Addressable. The trade-off is that the visuals were not checked in play mode.

## 14. README entry

### Attack cone and range display — 2 min
- **Implemented:** the sword now only hits enemies in a cone in front of the hero instead of all around him. The cone is drawn on the ground as a faint fan that pulses when the slash lands, so the player sees what will be hit. Details in [the feature note](Documentation/FeatureNotes/19_AttackConeAndRange.md).
- **Time:** 2 min
- **Bottleneck:** not confirmed.
- **Addressables:** the cone material is preloaded from the `VFX` group.
