# FEATURE: Joystick UI Rework (smaller, kept in a thumb zone)

## 1. Summary
The virtual joystick is smaller, and its base is kept in a bottom "thumb zone" instead of spawning under the finger anywhere on screen. The ring size and the finger travel come from one config value, so the knob always stays under the finger.

## 2. Why / issue fixed
The joystick UI was too big and on top of the gameplay actions. It hid what the player needed to see. This fix makes it smaller and keeps it from covering the gameplay.

## 3. Bottleneck
The joystick UI was too big and on top of the gameplay actions.

> Human's answer, derived from a single statement: no separate bottleneck was given. To confirm or refine before the review.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis + implementation | `3 min` (10:43:05 → 10:45:52) |
| Compile fix + testing | `6 min` (10:45:52 → 10:52:14) |
| Asset generation / integration | `0 min` |
| **Total** | `9 min` |

Real elapsed time from recorded timestamps. No timestamp was taken between analysis and implementation, so they are reported as one block rather than estimated. The testing block includes waiting on an unrelated compile error caused by other work in progress in the same editor.

## 5. Files changed
- `JoystickInputConfig.cs`: `_maxRadius` (pixels) replaced by `_maxRadiusScreenRatio` (fraction of the screen width) and a new `_thumbZoneHeightRatio`. `MaxRadius` stays a property and is now computed in pixels.
- `JoystickInputService.cs`: new `ClampToThumbZone`, applied to the joystick base on press. The movement maths is untouched.
- `JoystickView.cs`: sizes the ring from the config when it appears, and places the ring and knob in screen pixels. The old `_containerRadius` is removed.
- `Joystick.prefab`: ring authored at 194 and knob at 80 canvas units (was 300 and 150). The ring is re-sized at runtime anyway.
- `JoystickInputConfig.asset`: new field values.
- `Game.UI.asmdef`: added a reference to `Core.ScriptableObjectSingleton`, because the view now reads the config.

## 6. Technical flow
1. Press: `JoystickInputService.HandleInput` reads the mouse or touch position and calls `ClampToThumbZone`. The base is kept inside `x` from `radius` to `width - radius` and `y` from `radius` to the top of the thumb zone. The new `JoystickState` is active with a zero movement vector.
2. Drag: every frame the movement vector is `(finger - base) / radius`, clamped to length 1. Unchanged logic, but the base can now be away from the finger, so a press above the zone gives a full tilt toward the finger.
3. The service raises `OnStateChanged`. `HeroController` only reads the movement vector, so gameplay is unchanged.
4. `JoystickView` reacts. When the ring appears it is sized to `2 × MaxRadius / canvas.scaleFactor`. On every update the ring is placed at the base and the knob at `base + movement × MaxRadius`. Both are in screen pixels, which is what `RectTransform.position` uses on an Overlay canvas.
5. Release or hero death: the joystick is hidden.

## 7. Tuning values
Both are on `JoystickInputConfig`.
- `MaxRadiusScreenRatio` = `0.09` (range `0.04` to `0.2`): joystick radius as a fraction of the screen width. About 97 canvas units on the 1080-wide reference screen, so the ring is about 194 across. It sets the ring size and the finger travel together.
- `ThumbZoneHeightRatio` = `0.3` (range `0.1` to `0.6`): height of the bottom band where the joystick base can sit.
- Knob size = `80` canvas units: authored on the prefab, not driven by the config.

## 8. Addressables
- **Used?** No.
- **Why not:** nothing new is loaded at runtime. The joystick is part of the scene UI and the settings stay in the existing `Resources` config, like the other configs. A direct reference was the reliable choice here.

## 9. Generated assets
- **Used?** No. The existing circle sprite is reused.

## 10. Edge cases and limits
- **Press above the thumb zone:** the base stays at the top edge of the zone and the stick points toward the finger at full tilt, so the hero moves right away. No touch is ignored, so a mouse click in the middle of the editor Game view still steers.
- **Press near the screen edges:** the base is clamped so the ring is never cut off. The knob is then under the finger and the initial tilt may be above zero.
- **Thumb zone smaller than the ring:** its top is never below one radius (`Mathf.Max`).
- **Screen size changes (editor Game view, rotation):** the radius is read from the live `Screen.width` on press and the ring is re-sized each time it appears.
- **Validated:** in Play mode, by forcing a press at the middle of the screen. The base landed at 30% of the screen height, the ring was twice the input radius, and the knob was exactly one radius above the base. The screenshot showed the hero and the area around it unobstructed.
- **Not validated:** a real mouse or touch drag, a device, and the in-zone and near-edge screenshots. They use the same clamp.
- **Deliberately not built:** an idle hint of where the joystick appears, a fade in and out, the device safe area (home indicator), a left or right-handed layout, multi-touch.

## 11. Base project observations
- The input range was in screen pixels (`100`) while the drawn ring and the knob travel were in canvas units (`150`). The knob moved faster than the finger, and the feel changed with the resolution.
- The ring was 300 canvas units (about 28% of the screen width) and spawned exactly under the finger.
- Only the first touch is read, and a press over a UI button also starts the joystick.

## 12. Likely review questions
- **Why a fraction of the screen width and not pixels?** Pixels change meaning with the resolution. The canvas is set to match the width, so a width ratio keeps the ring and the finger travel the same on any screen.
- **Why clamp the base instead of ignoring touches outside the zone?** No input is lost. A player or a reviewer pressing anywhere still steers, and the joystick never leaves the bottom band.
- **Why is the clamp in the service and not in the view?** The base is part of the joystick state, and the movement vector is computed from it. If only the view moved the ring, the picture and the real input would disagree.
- **Why `RectTransform.position` and not `anchoredPosition`?** On an Overlay canvas the position is already in screen pixels, like the input. That avoids converting between pixels and canvas units.
- **Why is the ring sized when it appears and not in `Awake`?** The canvas scale is only reliable once the canvas scaler has run, and the window can be resized. Sizing on appearance is cheap and always correct.

## 13. 30-second explanation
The joystick was too big and appeared under the finger, so it covered the gameplay. I made it smaller and kept its base in a bottom band of the screen, so the hero and enemies stay visible. One config value, a fraction of the screen width, drives both the ring size and the input range, so the knob stays under the finger on any resolution. The trade-off is that a press high on the screen steers at full tilt from the bottom band instead of from the finger.

## 14. README entry

### Joystick UI rework — 9 min
- **Implemented:** the joystick is smaller and its base stays in a bottom thumb zone, so it no longer covers the gameplay. The ring size and the finger travel now come from one value.
- **Time:** 9 min
- **Bottleneck:** the joystick UI was too big and on top of the gameplay actions.
