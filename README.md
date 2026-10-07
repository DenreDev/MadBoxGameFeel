# MadBoxGameFeel

## Approach

TO FILL

## Features and time

| Feature | Time |
|---|---|
| Entity animation state machines | 9 min |
| Health bars | 17 min |
| **Total** | 26 min |

### Entity animation state machines — 9 min
- **Implemented:** hero and bees now play Idle, Move, Attack, Damage and Death at the right moment, driven by events from the gameplay controllers. Details in [the feature note](Documentation/FeatureNotes/01_EntityAnimations.md).
- **Time:** 9 min
- **Bottleneck:** entities were lacking feedback on their state.

### Health bars — 17 min
- **Implemented:** every entity has a health bar above its head: green and bigger for the hero, red for enemies (shown once damaged). Hits are animated with a sliding fill, flash, pop and a lagging trail, and the bar disappears on death. Details in [the feature note](Documentation/FeatureNotes/02_HealthBars.md).
- **Time:** 17 min
- **Bottleneck:** the player had no feedback on enemy health.
- **Addressables:** the bar prefab is preloaded from a new `UI` group.

## Feedback on the base project

TO FILL

## Comments

TO FILL
