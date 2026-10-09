# FEATURE: Combo counter

## 1. Summary
A "COMBO xN" counter on the right of the screen climbs with every kill in a row. A time bar drains after each kill, and the combo breaks when it runs out or when the hero is hit. Every kill punches the number with a glow, a ring, sparks and a rising bell. Five tiers (5, 10, 20, 35, 50 kills) change the colors, grow the number and slam a title ("NICE!" to "LEGENDARY!"). A break greys the number, shakes it and drops it off screen.

## 2. Why / issue fixed
- **Why (from the human):** to increase again the feeling of killing enemies, because the game is going toward a Vampire Survivors-like. It had to be juicy.
- **Issue fixed (deduced from that statement):** in a survivor-like the player kills constantly, and each kill disappeared without adding up to anything. A running streak gives the kills a cumulative payoff and a reason to keep killing without taking a hit.

> The issue wording is deduced from the human's statement, to be confirmed.

## 3. Bottleneck
Not given by the human. Deduced from the work, not from their statement: the HUD placement on a portrait screen. The first version sat at the hero's height and overlapped the action, then the tier title collided with the wave badge and the wave banner. It took three position passes, and two of them were the same bug: the animation wrote an anchored position and overwrote the offset the builder had set.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `2 min 29 s` (11:31:23 → 11:33:52) |
| Implementation | `2 min 22 s` (11:33:52 → 11:36:14) |
| Tuning / testing | `4 min 15 s` (11:36:14 → 11:40:29) |
| Asset generation / integration | included in the phases above (no separate timestamp) |
| **Total** | `9 min` (sum of the timestamps, 9 min 06 s) |

Compilation, the sound and texture generation, the HUD build and three play-mode probe runs are inside "Tuning / testing". Waiting for the editor is counted, waiting for the human is not.

## 5. Files changed
- `ComboTracker.cs`: new, plain C#. Count, time left, `OnIncreased` and `OnBroken` events. Knows nothing about Unity.
- `ComboCounterView.cs`: new, in `Game.UI`. Feeds the tracker, listens to kills and hero hits, and runs every animation.
- `GameFeelService.cs`: loads three clips, `PlayComboHit(step)` (pentatonic pitch), `PlayComboMilestone`, `PlayComboBreak`. Releases them in `Reset`.
- `GameFeelConfig.cs` and `GameFeelConfig.asset`: three clip references, three volumes, max pitch.
- `MainScene.unity`: new `UI/ComboCounter` object (panel, glow, ring, number, label, bar, title, 16 sparks).
- `ComboHit.wav`, `ComboMilestone.wav`, `ComboBreak.wav`, `ComboGlow.png`, `ComboRing.png`.
- `Audio.asset`: three new Addressable entries.

Gameplay controllers and game rules were not touched. `EnemiesController.OnEnemyKilled` and `HeroController.OnDamaged` / `OnDied` already existed.

## 6. Technical flow
1. `ComboCounterView` subscribes to `OnEnemyKilled`, `OnDamaged`, `OnDied` and the game state once the services are ready.
2. A kill calls `ComboTracker.RegisterKill(window)`: count + 1, time bar refilled, `OnIncreased` raised.
3. From `minShownCount` (2) on, the view reacts. The first time it slides the panel in from the right. Every time it sets the number and the tier colors, punches the scale with a back-ease, kicks and wobbles the rotation, shakes it, flares the glow, grows a ring, emits sparks, and asks `GameFeelService` for the bell. The bell climbs one pentatonic note per kill, capped.
4. If the kill crosses a tier threshold, the punch is stronger, the burst is bigger, the title slams in (scale down, hold, fade up) and the sparkle plays.
5. Each frame the view ticks the tracker with scaled time (a hit-stop freeze does not eat the window) and animates on unscaled time. The bar drains, and under 30% it blinks red and the number trembles.
6. The window runs out, or the hero is hit: `OnBroken`. The label becomes "TIME'S UP" or "BROKEN!", the number greys and shakes (harder on a hit, with a red glow), then the panel falls, tumbles and fades. The falling sound plays.
7. Back on the start screen the tracker resets silently and the panel hides at once.

## 7. Tuning values
On `ComboCounterView`:
- `comboWindow` = `3` s: time to get the next kill.
- `minShownCount` = `2`: kills before the counter appears.
- `tiers`: threshold, title and two colors per tier (defaults 0, 5, 10, 20, 35, 50).
- `punchScale` = `1.55`, `tierPunchMultiplier` = `1.8`, `punchDuration` = `0.4`, `kickAngle` = `9`, `shakeAmount` = `7`, `tierScaleStep` = `0.08`: the kill punch.
- `lowTimeThreshold` = `0.3`, `lowTimeShake`, `lowTimeBlinkRate`: the "about to break" warning.
- `breakDuration` = `0.7`, `breakFallDistance`, `breakSpin`, `breakShakeAmount`: the break.
- Title timings and spark count, speed, gravity and life.

In `GameFeelConfig`: `comboHitVolume` = `0.5`, `comboMilestoneVolume` = `0.8`, `comboBreakVolume` = `0.7`, `comboMaxSemitones` = `14`.

## 8. Addressables
- **Used?** Yes, for the sounds. The HUD itself and its two textures are scene content, not runtime-loaded.
- **Assets / addresses / group:** `Audio/ComboHit`, `Audio/ComboMilestone`, `Audio/ComboBreak` in `Audio`.
- **Load:** at `GameFeelService.Initialize`, awaited before gameplay (UniTask).
- **Owner:** `GameFeelService`, in its handle list.
- **Lifetime / cache:** loaded for the whole session. Every play is a `PlayOneShot` of the cached clip.
- **Release:** `GameFeelService.Reset` releases the handles and clears the references.
- **Failure:** an unassigned or failing reference logs a warning and only that sound is skipped. The counter still works silently.
- **Remote readiness:** the group uses the profile path variables, so moving it to a CDN is a configuration change. The loads are awaited at startup, so a remote group would need the download hidden behind a loading step.
- **Why Addressables here:** new runtime-loaded content, same pattern as the other feedback sounds, replaceable or remotely served without code changes.

## 9. Generated assets
- **Used?** Yes.
- **`ComboHit.wav`, `ComboMilestone.wav`, `ComboBreak.wav`:** a short glassy bell, a five-bell rising sparkle and a falling "womp" with a crackle, synthesized with sine partials and envelopes by a one-off editor script (deleted). Why generated: the project had no combo sounds. Integrated as `Audio` Addressables. Replacement cost: low, replace the files and keep the addresses. The bell is pitched at runtime, so keep it a single clean note.
- **`ComboGlow.png`, `ComboRing.png`** (128 px): a soft radial glow, also used for the sparks, and a thin ring. Generated by the same script. Referenced by the scene. Replacement cost: low, swap the textures.
- Time is included in the phases above.

## 10. Edge cases and limits
- **Several kills in one frame:** each one counts, the view reacts to each, and only one bell plays per frame.
- **Kamikaze bees:** an explosion is not a kill, it hits the hero and breaks the combo through the hero hit event.
- **Hero hit with no combo shown:** nothing plays, a combo of 1 is not announced.
- **New combo during a break animation:** the break is cut and the new combo starts fresh.
- **Hit-stop:** the window uses scaled time, so a freeze frame does not eat it. The animations use unscaled time, so they keep moving.
- **Restart:** the tracker and the visuals reset on the start screen.
- **Verified in play mode:** three probe runs on a shared editor, frames checked visually: appear, punch, tier titles and colors, ring, glow, sparks, low-time bar, break by hit. No console errors.
- **Not verified:** the break by timeout, because the hero's own kills kept the combo alive during the probe. The sounds were not heard, only wired. Only one screen aspect ratio was seen.
- **Deliberately not built:** a score or reward from the combo, a best-combo display, a window that shrinks with the tier, pooling of the HUD (it is one fixed set of objects).

## 11. Base project observations
- The `Game.GameFeel` assembly cannot reference gameplay, so a HUD that reacts to kills has to live in `Game.UI`, which can. The seam works, but the UI assembly now knows `EntitiesService`.
- The UI is one scene canvas with the views added by hand. A HUD part is a scene edit, not a prefab, so it conflicts with other sessions editing `MainScene`.

## 12. Likely review questions
- **Why a plain `ComboTracker` and not logic in the view?** The count, the window and the break are rules. Separating them keeps the view about presentation, and the tracker can later move to a gameplay service if the combo ever gives a bonus.
- **Why does the window use scaled time and the animation unscaled?** A hit-stop freeze should not cost the player combo time, but the HUD must not freeze with it.
- **Why a pentatonic scale for the bell?** Each kill raises the pitch, and any run of notes of that scale sounds in tune, so a fast streak sounds like a melody and not a siren.
- **Why a time bar and not just a number?** The player needs to see the combo about to break. The bar, the red blink and the trembling number are the warning.
- **Why not Addressables for the HUD?** It is in the scene, like the other HUD parts, and always on screen. Only the runtime-loaded sounds go through Addressables.

## 13. 30-second explanation
Kills in a survivor-like disappeared without adding up. Now kills in a row build a combo with a time bar. Each kill punches the number, flares a glow and a ring, throws sparks and plays a bell that climbs a scale. Tiers add colors and a slammed title. A hit or the timeout breaks it with a shaking fall. A small plain tracker holds the rules and a view listens to the existing kill and hero-hit events, so no gameplay code changed. The trade-off is that it is feedback only: no reward, no best score.

## 14. README entry

### Combo counter — 9 min
- **Implemented:** to increase again the feeling of killing enemies in a survivor-like, kills in a row now build a combo counter with a time bar. Each kill punches the number, flares a glow and sparks and plays a rising bell, and tiers add colors and a slammed title. The combo breaks with a shaking fall when the time runs out or the hero is hit. Feedback only: it gives no reward yet. Details in [the feature note](Documentation/FeatureNotes/24_ComboCounter.md).
- **Time:** 9 min
- **Bottleneck:** not confirmed.
- **Addressables:** three generated sounds preloaded from the `Audio` group.
