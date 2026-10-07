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
| Damage numbers | 26 min |
| Start screen and game flow | 31 min |
| Hit feedback, ragdoll death and screen shake | 25 min |
| Enemy spawn | 16 min |
| Smooth hero rotation | 4 min |
| Hero health value | 5 min |
| Hero ground ring | 8 min |
| Footstep puffs | 10 min |
| Archero-style visual pass | 13 min |
| Great Sword texture | 3 min |
| **Total** | 184 min |

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

### Damage numbers — 26 min
- **Implemented:** every hit on an enemy pops a floating damage number: chunky, outlined, with a color gradient. It slams in, drifts and fades. It is bigger for heavier weapons, and the hit that kills prints a bigger, red-orange number. Built on third-party package, so critical hits or scaling damage can reuse it later. Details in [the feature note](Documentation/FeatureNotes/05_DamageNumbers.md).
- **Time:** 26 min
- **Bottleneck:** validating in play mode on a Unity editor shared with other sessions, after making the imported third-party package compile.
- **Addressables:** the damage number prefab is preloaded and pooled from the `VFX` group.

### Start screen and game flow — 31 min
- **Implemented:** the game now waits on a "main menu" start screen: the camera frames the hero close up, facing the player, with a "TAP TO START" prompt, and nothing moves or spawns until the tap. A small game manager (Start, Playing, GameOver) drives it, so Restart comes back to the same screen. Details in [the feature note](Documentation/FeatureNotes/06_StartScreen.md).
- **Time:** 31 min

### Hit feedback, ragdoll death and screen shake — 25 min
- **Implemented:** every hit on the hero or a bee flashes the body white, squashes it and spits a small spark burst. Killed bees and the dead hero are knocked away in a flipping, ragdoll-style hop, and the camera gets small shakes when the hero attacks, is hit and dies. Details in [the feature note](Documentation/FeatureNotes/07_HitFeedbackRagdollShake.md).
- **Time:** 25 min
- **Addressables:** the spark prefab and the flash material are preloaded from the `VFX` group.

### Enemy spawn — 16 min
- **Implemented:** bees now pop in from nothing with a springy overshoot while a lavender dust puff, a ground glow and a few sparkles burst at their feet. Details in [the feature note](Documentation/FeatureNotes/08_EnemySpawn.md).
- **Time:** 16 min
- **Bottleneck:** not recorded.
- **Addressables:** the puff prefab is preloaded from the `VFX` group.

### Smooth hero rotation — 4 min
- **Implemented:** the hero no longer snaps to face his target when he auto-attacks. He eases toward it for a moment, then back to his movement direction, and every hero turn now shares the same easing. Details in [the feature note](Documentation/FeatureNotes/09_SmoothHeroRotation.md).
- **Time:** 4 min
- **Bottleneck:** none.

### Hero health value � 5 min
- **Implemented:** the hero's current health number now floats above his health bar, so the player reads exactly how much health is left. Enemies keep the bar only. Details in [the feature note](Documentation/FeatureNotes/10_HeroHealthValue.md).
- **Time:** 5 min
- **Bottleneck:** not recorded.

### Hero ground ring — 8 min
- **Implemented:** a thin, semi-transparent white ring now sits on the ground under the hero, so the player always knows where he is. Details in [the feature note](Documentation/FeatureNotes/11_HeroRing.md).
- **Time:** 8 min
- **Bottleneck:** not recorded.
- **Addressables:** the ring prefab is preloaded from the `VFX` group.

### Footstep puffs — 10 min
- **Implemented:** a small dust puff pops at the hero's feet every few steps while he walks, to highlight the feeling of walking. Details in [the feature note](Documentation/FeatureNotes/12_FootstepPuffs.md).
- **Time:** 10 min
- **Bottleneck:** no real bottleneck.
- **Addressables:** the puff prefab is preloaded from the `VFX` group.

### Archero-style visual pass — 13 min
- **Implemented:** the prototype visuals were not helping the player understand the game: flat grey floor, hard black shadows, a default sky and no post-processing. The scene now has a warm key light with soft violet shadows, a gradient sky with fog that melts the floor into the horizon, a teal floor so the warm characters stand out, and a light post-processing grade (bloom, tonemapping, saturation, split toning, vignette). The look lives in the Dungeon biome prefab. HDR, 4x MSAA and soft shadows are enabled in the URP asset. The Great Sword also got a steel material, because its texture was missing from the project.
- **Time:** 13 min (the sword material fix was not timed separately)
- **Bottleneck:** not recorded.

### Great Sword texture — 3 min
- **Implemented:** the sword now has a texture: gold ornamented crossguard with a gem, leather-wrapped grip, steel blade with gold trim. The model's UVs are a palette-style atlas, so the texture is baked from the mesh instead of hand painted, and can be replaced by a real one without touching the material.
- **Time:** 3 min
- **Bottleneck:** not recorded.

## Feedback on the base project

TO FILL

## Comments

TO FILL
