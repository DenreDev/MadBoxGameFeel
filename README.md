# MadBoxGameFeel

## Approach

I first considered how to use AI effectively within the four-hour limit. I spent some initial time defining focused instructions for architecture, asset generation, and documentation, with the aim of accelerating implementation and leaving room for testing and polish. I remained responsible for the design direction and for understanding and evaluating the generated code.

My first gameplay priority was combat readability. Animations were not connected to gameplay actions, making it difficult to understand what was happening. I started by making those states visible and adding health and damage feedback.

I then focused on making combat more satisfying through hit reactions, effects, sound, and camera feedback. Finally, I refined the presentation and explored additions at the boundary between feel and gameplay design where they could improve clarity and polish. Throughout, the goal was to balance implementation with playtesting, tuning, fixes, and understanding the architecture.

## Features and time

| Feature | Approximate time |
|---|---|
| Entity animation state machines | 9 min |
| Health bars | 9 min |
| Joystick UI rework | 9 min |
| Higher camera angle | 8 min |
| Damage numbers | 9 min |
| Start screen and game flow | 10 min |
| Hit feedback, ragdoll death and screen shake | 12 min |
| Enemy spawn | 8 min |
| Smooth hero rotation | 4 min |
| Hero health value | 5 min |
| Hero ground ring | 8 min |
| Footstep puffs | 7 min |
| Archero-style visual pass | 13 min |
| Great Sword texture | 3 min |
| Multi-hit Great Sword | 2 min |
| Run animation speed | 2 min |
| Enemy attack telegraph | 7 min |
| Delayed swing and aim | 8 min |
| Attack impact pass | 3 min |
| Attack sounds | 7 min |
| Attack cone and range display | 2 min |
| Critical strikes | 11 min |
| Currency orbs | 8 min |
| Enemy waves and bee types | 15 min |
| Wave counter, including view refactor | 10 min |
| Combo counter | 9 min |
| Boot, start and game over screens | 14 min |
| Attack range charge fill | 6 min |
| Feedback config split (refactor) | 24 min |
| **Total** | **About 4 h** |

All times are approximate, rounded from recorded work sessions.

### Entity animation state machines

- **Implemented:** hero and bees now play Idle, Move, Attack, Damage and Death at the right moment, driven by events from the gameplay controllers.

### Health bars

- **Implemented:** every entity has a health bar above its head: green and bigger for the hero, red for enemies (shown once damaged). Hits are animated with a sliding fill, flash, pop and a lagging trail, and the bar disappears on death. The bars bring comprehension: the player sees at once who is being hit, how much damage it did and how close an enemy is to dying.
- **Addressables:** the bar prefab is preloaded from a new `UI` group.

### Joystick UI rework

- **Implemented:** the joystick is smaller and its base stays in a bottom thumb zone, so it no longer covers the gameplay. The ring size and the finger travel now come from one value.

### Higher camera angle

- **Implemented:** the gameplay camera is higher and steeper (65° instead of 57.7°, distance 16 instead of 14), so enemies coming from the sides and behind show up earlier while the hero stays readable.

### Damage numbers

- **Implemented:** every hit on an enemy pops a floating damage number: chunky, outlined, with a color gradient. It slams in, drifts and fades. It is bigger for heavier weapons, and the hit that kills prints a bigger, red-orange number. Built as one small TextMeshPro component, so critical hits or scaling damage can reuse it later.
- **Addressables:** the damage number prefab is preloaded from the `VFX` group.

### Start screen and game flow

- **Implemented:** the game now waits on a "main menu" start screen: the camera frames the hero close up, facing the player, with a "TAP TO START" prompt, and nothing moves or spawns until the tap. A small game manager (Start, Playing, GameOver) drives it, so Restart comes back to the same screen.

### Hit feedback, ragdoll death and screen shake

- **Implemented:** every hit on the hero or a bee flashes the body white, squashes it and spits a small spark burst. Killed bees and the dead hero are knocked away in a flipping, ragdoll-style hop, and the camera gets small shakes when the hero attacks, is hit and dies.
- **Addressables:** the spark prefab and the flash material are preloaded from the `VFX` group.

### Enemy spawn

- **Implemented:** bees now pop in from nothing with a springy overshoot while a lavender dust puff, a ground glow and a few sparkles burst at their feet.
- **Addressables:** the puff prefab is preloaded from the `VFX` group.

### Smooth hero rotation

- **Implemented:** the hero no longer snaps to face his target when he auto-attacks. He eases toward it for a moment, then back to his movement direction, and every hero turn now shares the same easing.

### Hero health value

- **Implemented:** the hero's current health number now floats above his health bar, so the player reads exactly how much health is left. Enemies keep the bar only.

### Hero ground ring

- **Implemented:** a thin, semi-transparent white ring now sits on the ground under the hero, so the player always knows where he is.
- **Addressables:** the ring prefab is preloaded from the `VFX` group.

### Footstep puffs

- **Implemented:** a small dust puff pops at the hero's feet every few steps while he walks, to highlight the feeling of walking.
- **Addressables:** the puff prefab is preloaded from the `VFX` group.

### Archero-style visual pass

- **Implemented:** the prototype visuals were not helping the player understand the game: flat grey floor, hard black shadows, a default sky and no post-processing. The scene now has a warm key light with soft violet shadows, a gradient sky with fog that melts the floor into the horizon, a teal floor so the warm characters stand out, and a light post-processing grade (bloom, tonemapping, saturation, split toning, vignette). The look lives in the Dungeon biome prefab. HDR, 4x MSAA and soft shadows are enabled in the URP asset. The Great Sword also got a steel material, because its texture was missing from the project.

### Great Sword texture

- **Implemented:** the sword now has a texture: gold ornamented crossguard with a gem, leather-wrapped grip, steel blade with gold trim. The model's UVs are a palette-style atlas, so the texture is baked from the mesh instead of hand painted, and can be replaced by a real one without touching the material.

### Multi-hit Great Sword

- **Implemented:** the Great Sword now hits up to 3 enemies with one swing to give the attack more impact. The slash effect was removed and the screen shake on hits is lower.

### Run animation speed

- **Implemented:** the hero's run animation now plays at a speed based on his real move speed, so the legs match how fast he moves.

### Enemy attack telegraph

- **Implemented:** enemies now stop and warn before attacking: the body blinks red and a red disc grows on the ground to the attack range. Then the attack animation plays, and the hero is hit at its end only if he is still in range, so he can dodge.
- **Addressables:** the red disc prefab and the tint material are preloaded from the `VFX` group.

### Delayed swing and aim

- **Implemented:** the hero's attack now waits for the slash high point to deal damage, keeps facing its target for the whole swing and always plays its animation completely.
- **Addressables:** a slash effect was added in the `VFX` group, then removed from the game.

### Attack impact pass

- **Implemented:** sword hits shove the enemy back, chained kills freeze the game for a few frames, and the camera kicks along the slash.

### Attack sounds

- **Implemented:** the attack now has sound: a swing whoosh, a hit thump and a heavier kill crunch that rises in pitch with the kill streak.
- **Addressables:** three generated sounds preloaded from a new `Audio` group.

### Attack cone and range display

- **Implemented:** the sword now only hits enemies in a cone in front of the hero instead of all around him. The cone is drawn on the ground as a faint fan that pulses when the slash lands, so the player sees what will be hit.
- **Addressables:** the cone material is preloaded from the `VFX` group.

### Critical strikes

- **Implemented:** sword hits can now critical strike (15% chance, x1.5 damage). A crit gets a bigger pink damage number with a "!", a bigger spark, a stronger squash and shove, a freeze frame, a harder camera kick and the heavy crunch sound.

### Currency orbs

- **Implemented:** to increase the feeling of killing an enemy, dead enemies now drop a few tiny blue orbs that bounce, rest a moment, then recoil and curve into the hero, with a sparkle and a pitch-rising blip on each pickup. Feedback only: orbs have no value yet.
- **Addressables:** the orb and sparkle prefabs in `VFX`, two generated sounds in `Audio`, all preloaded.

### Enemy waves and bee types

- **Implemented:** the game felt empty and enemies were too easy to dodge. Enemies now come in waves that start one second after the tap and ramp up, from just outside the screen and rarely from the close portrait sides. Small bees are fast kamikazes, normal bees attack sooner and lunge so they actually land hits, and strong bees are huge, slow tanks. The camera zooms out when the crowd grows, and enemy views are pooled.
- **Addressables:** the three bee prefabs are preloaded from a new `Enemies` group.

### Wave counter

- **Implemented:** to explain to the player what is going on, a "WAVE n" badge now sits at the top of the screen and a big banner with a short fanfare announces each new wave. The number punches and a glow pulses on every change. A WaveCounterView entry point drives separate badge and banner scripts on their respective objects.
- **Addressables:** the generated stinger is preloaded from the `Audio` group.

### Combo counter

- **Implemented:** to increase again the feeling of killing enemies in a survivor-like, kills in a row now build a combo counter with a time bar. Each kill punches the number, flares a glow and sparks and plays a rising bell, and tiers add colors and a slammed title. The combo breaks with a shaking fall when the time runs out or the hero is hit. Feedback only: it gives no reward yet.
- **Addressables:** three generated sounds preloaded from the `Audio` group.

### Boot, start and game over screens

- **Implemented:** to improve the overall juice of the game from the very first frame, a title cover now hides the loading instead of an empty sky and a dead "TAP TO START". The start screen drops its title in, pops a chunky button and teaches the controls. Game over now plays a short sequence: dim, title slam, round stats, then a "TRY AGAIN" button that only accepts taps once it is visible. Buttons squash under the finger.

### Attack range charge fill

- **Implemented:** to highlight the attack, a fill now grows inside the attack cone during the swing and reaches its edge exactly when the hit lands, so the player knows when it hits.

### Feedback config split (refactor)

- **Implemented:** the all-in-one `GameFeelConfig` and `GameFeelService` were split by composition: each feedback is its own ScriptableObject, plugged into the enemy, hero or weapon config it belongs to, so each enemy type and weapon can feel different. No player-facing change.
- **Addressables:** one service owns every game feel handle, preloaded at startup from the content catalogs.

## Feedback on the base project

**What I noticed as a player**

- Hits had no weight: no flash, flinch, knockback, sparks, sound or camera reaction, and the damage and death animations were never played.
- It was hard to understand what was happening: the animators had Damage, Death and attack states that nothing triggered, bees never left Idle, and there was no health or damage feedback.
- Damage was applied the same frame as the swing or the bee's arrival, so animation and hit could never line up.
- Enemy attacks came with no warning, so the player could not read them or dodge.
- The hero's hit rule only used distance, with no direction and nothing drawn on screen, so it was unclear what a swing would hit.

**What I noticed in the code**

- The structure was easy to extend: plain C# controllers raising events, views reacting to them, services found by reflection through `ServicesLocator`, and ScriptableObject configs. Adding a feedback rarely meant touching gameplay code.
- Views unsubscribe in `OnDestroy` through `ServicesLocator.Instance`, which is already destroyed, so leaving play mode throws `NullReferenceException`s.
- `EntitiesService.Reset()` does not reset its controllers, so their update loops are never cancelled.
- `ServicesLocator` has no `try/catch` around `Initialize`, `GetService` throws before the services exist, and `ResetServices` throws on a duplicate instance.
- `HeroController.OnStateChanged` fires every movement frame, and enemy events carried only an id (no damage), so views had to re-read state.
- Only weapons were Addressable: enemy and hero prefabs were direct references from a `Resources` config, despite the `Local/` and `Remote/` folders.
- The camera angle is stored twice (World prefab `VirtualCamera` and the scene Main Camera), and only the first one has an effect.

## Comments

- I appreciated the one-week window to complete a four-hour assignment. It made the test easier to fit around my job.
- I particularly enjoyed the focus on Game Feel, since technical tests often emphasize designing and implementing new features.
- The scope felt well suited to the four-hour limit, especially with AI assistance. Supervising an agent's implementation gave me more time to focus on Game Feel decisions.
- I also appreciated seeing Addressables used in the base project.
