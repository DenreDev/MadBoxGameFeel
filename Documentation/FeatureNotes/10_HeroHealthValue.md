# FEATURE: Hero Health Value

## 1. Summary
The hero's current health number (for example `85`) now floats just above his health bar. Enemies keep a bar only, no number. The number is part of the existing health bar prefab and follows its pop and flash on hit.

## 2. Why / issue fixed
- **Why:** help the player understand their own health. A bar shows a proportion, the number shows exactly how much is left.
- **Issue fixed:** not recorded.

## 3. Bottleneck
Not recorded.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `1 min` (16:30:54 to 16:31:33) |
| Implementation | `1 min` (16:31:33 to 16:32:44) |
| Tuning / testing | `3 min` (16:32:44 to 16:36:04) |
| Asset generation / integration (if any) | included above (no generated art) |
| **Total** | `5 min` |

Real elapsed time from recorded timestamps (16:30:54 to 16:36:04 = 5 min 10 s). The phase boundaries are the first tool-call timestamps of each phase. The testing phase was spent on attempts to play-test in a Unity editor shared with other sessions, which did not produce a usable frame. The final check was done by the human in the editor ("seems ok"), and that review time is not included. To be confirmed by the human.

## 5. Files changed
- `Assets/Features/HealthBar/Scripts/View/HealthBarView.cs`: new `healthText` field, `Bind` takes `showHealthValue`, `UpdateHealthText` rewrites the number only when it changes.
- `Assets/Features/HealthBar/Scripts/HealthBarService.cs`: passes `isHero && config.ShowHeroHealthValue` when binding a bar.
- `Assets/Features/HealthBar/Scripts/Config/HealthBarConfig.cs`: new `showHeroHealthValue` toggle.
- `Assets/Features/HealthBar/Scripts/Game.HealthBar.asmdef`: references `Unity.TextMeshPro`.
- `Assets/Features/HealthBar/View/Local/HealthBar/HealthBar.prefab`: new `HealthText` child under `Visual`.
- `Assets/Features/HealthBar/View/Local/HealthBar/HealthTextOverlay.mat`: TextMeshPro overlay material with a dark outline.

Hero and enemy views, and the gameplay controllers, were not touched.

## 6. Technical flow
1. `HealthBarService.Create` binds the bar. For the hero (and when `showHeroHealthValue` is on) the `HealthText` object is enabled, for enemies it stays disabled.
2. `HeroView` already pushes health into the bar on every state change through `SetHealth(health, maxHealth)`.
3. `SetHealth` calls `UpdateHealthText`. If the value differs from the last one shown, it writes it with `SetText("{0}", health)`, which does not allocate a string.
4. The text lives under the bar's `Visual` object, so it hides with the bar (start screen, death) and scales with the hit pop.
5. The text uses the overlay material (`ZTest Always`, `Overlay` queue), so it is drawn on top of meshes like the bar.

## 7. Tuning values
- `showHeroHealthValue` = `true` (in `HealthBarConfig`): shows or hides the hero's health number.
- Font size, color, outline and position above the bar are set on the `HealthText` child in the prefab (font size `3`, bold, white, outline `0.25`, local height `0.36`).

## 8. Addressables
- **Used?** No new asset. The text is part of the health bar prefab, which is already preloaded from the `UI` group (address `UI/HealthBar`) by `HealthBarService`.
- **Load / owner / lifetime / release / failure:** unchanged, see [the health bar note](02_HealthBars.md). If the prefab fails to load there are no bars and no number.
- **Remote readiness:** the material and the TMP font atlas it uses are referenced by the prefab, so they ship in the same bundle. The font asset lives in `Assets/TextMesh Pro`, not in an Addressable group, so Addressables pulls it into the bundle as an implicit dependency.
- **Why Addressables here:** no new runtime-loaded asset was added, so nothing to change.

## 9. Generated assets
- **Used?** No. The overlay material was created in the editor from the standard TMP overlay shader and the default `LiberationSans SDF` font. Replacement cost: a few minutes (swap the font or material on the child).

## 10. Edge cases and limits
- Dead hero: `SetHealth` returns before updating the text and the bar is hidden. On restart the full health is a new value, so the number is rewritten.
- Missing text reference: `healthText` is null-checked, the bar still works without the number.
- Not built: no `current/max` format, no number on enemies, no number animation (count down, color flash). Possible follow-ups.
- Not validated in play mode by the agent: the shared editor was locked by another session's play transition. The human checked it in the editor.

## 11. Base project observations
- The editor is shared by several sessions, so play-mode validation by script is unreliable. Any domain reload from another session stops a running play test.

## 12. Likely review questions
- **Why a world-space TextMeshPro and not a Canvas?** The bar is already world space and faces the camera. A child text follows for free: same position, rotation, scale and pop, with no canvas to keep in sync.
- **Why a separate overlay material?** The default TMP shader is depth tested, so the number would hide behind the dungeon geometry while the bar is drawn on top. The overlay shader uses `ZTest Always`, like `HealthBarOverlay`.
- **Why pass the flag in `Bind` and not a separate prefab?** One prefab, one loader, one handle. The team rule (hero or enemy) stays where the bar is created, and the view does not know about teams.
- **How do you avoid garbage every hit?** The text is only rewritten when the value changes, using `SetText` with a format, which writes the number without building a string.
- **How would you add the number to enemies?** Pass `true` for them in `HealthBarService.Create`, or add a second config toggle.

## 13. 30-second explanation
A bar shows a proportion, not the exact health. I added the hero's current health number above his bar, as a world-space TextMeshPro child of the existing bar prefab, so it follows the bar and its hit pop. It only updates when the value changes, uses an overlay material so it draws above meshes, and is switched on for the hero only through the existing `Bind` call, with a config toggle. Trade-off: no `current/max` format and no animation, to keep it small.

## 14. README entry

### Hero health value — 5 min
- **Implemented:** the hero's current health number now floats above his health bar, so the player reads exactly how much health is left. Enemies keep the bar only. Details in [the feature note](Documentation/FeatureNotes/10_HeroHealthValue.md).
- **Time:** 5 min
- **Bottleneck:** not recorded.
