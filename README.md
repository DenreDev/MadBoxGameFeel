# MadBoxGameFeel

## Approach

TO FILL

## Features and time

| Feature | Time |
|---|---|
| Entity animation state machines | 9 min |
| Health bars | 17 min |
| Joystick UI rework | 9 min |
| Higher camera angle | 8 min |
| **Total** | 43 min |

### Entity animation state machines — 9 min
- **Implemented:** hero and bees now play Idle, Move, Attack, Damage and Death at the right moment, driven by events from the gameplay controllers. Details in [the feature note](Documentation/FeatureNotes/01_EntityAnimations.md).
- **Time:** 9 min
- **Bottleneck:** entities were lacking feedback on their state.

### Health bars — 17 min
- **Implemented:** every entity has a health bar above its head: green and bigger for the hero, red for enemies (shown once damaged). Hits are animated with a sliding fill, flash, pop and a lagging trail, and the bar disappears on death. Details in [the feature note](Documentation/FeatureNotes/02_HealthBars.md).
- **Time:** 17 min
- **Bottleneck:** the player had no feedback on enemy health.
- **Addressables:** the bar prefab is preloaded from a new `UI` group.

### Joystick UI rework — 9 min
- **Implemented:** the joystick is smaller and its base stays in a bottom thumb zone, so it no longer covers the gameplay. The ring size and the finger travel now come from one value. Details in [the feature note](Documentation/FeatureNotes/03_JoystickUI.md).
- **Time:** 9 min
- **Bottleneck:** the joystick UI was too big and on top of the gameplay actions.

### Higher camera angle — 8 min
- **Implemented:** the gameplay camera is higher and steeper (65° instead of 57.7°, distance 16 instead of 14), so enemies coming from the sides and behind show up earlier while the hero stays readable. Details in [the feature note](Documentation/FeatureNotes/04_HigherCamera.md).
- **Time:** 8 min
- **Bottleneck:** the camera was too focused on the player, so enemy spawns came as a surprise.

## Feedback on the base project

TO FILL

## Comments

TO FILL
