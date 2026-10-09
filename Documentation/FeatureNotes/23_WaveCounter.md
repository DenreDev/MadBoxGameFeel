# FEATURE: Wave counter

## 1. Summary
A "WAVE n" badge at the top of the screen, and a big banner that slams in whenever a new wave starts, with a short fanfare. The number punches and a glow pulses off the badge on every change. It only listens to the wave spawner: gameplay is untouched.

## 2. Why / issue fixed
- **Why (from the human):** to explain to the player what is going on.
- **Issue fixed (deduced from that statement):** waves were invisible. Enemies simply got more numerous and tougher, and the player could not tell that the game was escalating in steps. Showing the wave number and announcing each new one makes the pressure readable.

> The issue wording is deduced from the human's statement, to be confirmed.

## 3. Bottleneck
Not given by the human. Deduced from the work, not from their statement: checking the animation in play mode. The editor was unfocused and rendered slowly, so timed screenshots came out stale, and a first test tapped Start before the services finished initializing. A fixed capture framerate made the frames deterministic.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis and code writing (not separated) | `2 min 23 s` (11:27:34 → 11:29:57) |
| Implementation: compile fix, sound, HUD builder | `2 min 02 s` (11:29:57 → 11:31:59) |
| Tuning / testing | `2 min 56 s` (11:31:59 → 11:34:55) |
| Asset generation / integration | included in implementation (no separate timestamp) |
| **Total** | `7 min` (7 min 21 s from the recorded timestamps) |

Analysis and the first code were not timed separately. Waiting for the human is not counted.

## 5. Files changed
- `WaveCounterView.cs`: new, in `Game.UI`. Listens to the wave event, drives the badge and the banner animations.
- `EnemyWaveSpawner.cs`: new `OnWaveStarted(waveNumber)` event, raised as a wave opens. Nothing else changed (another session's file).
- `EntitiesService.cs`: exposes the spawner as `WaveSpawner`.
- `GameFeelService.cs`: loads the stinger, `PlayWaveStart()`, releases it in `Reset`.
- `GameFeelConfig.cs` and `GameFeelConfig.asset`: `waveStartClip` reference and `waveStartVolume`.
- `Game.UI.asmdef`: references `Game.GameFeel` and `Unity.TextMeshPro`.
- `MainScene.unity`: new `WaveCounter` object under the `UI` canvas, first sibling so the start screen and game over overlay cover it. Saving re-serialized much of the scene, so its diff is large and mostly reordering.
- `WaveStart.wav` under `Features/GameFeel/View/Local/WaveSounds/`, and the `Audio.asset` entry.

## 6. Technical flow
1. At startup `GameFeelService` preloads the stinger. `WaveCounterView` subscribes to `EnemyWaveSpawner.OnWaveStarted` and the game flow state.
2. A round starts: the spawner waits `RoundStartDelay`, then opens wave 1 and raises `OnWaveStarted(1)`.
3. The view sets the number and the banner text, fades the badge in, restarts both animations and asks the service for the stinger.
4. Number: it starts at `punchScale`, white, and springs back with an overshoot to its gold color. A glow behind the badge grows and fades.
5. Banner: the text drops in from `bannerStartScale` with an overshoot while the ribbon opens, holds, then drifts up and fades while the ribbon closes.
6. Each next wave repeats step 3 to 5. The last configured wave never ends, so the counter stops there.
7. The flow returns to Start (after a game over): the view resets, hidden until wave 1 of the next round.

## 7. Tuning values
In `WaveCounterView` (inspector):
- `punchScale` = `1.7`: how big the number pops on a change.
- `punchDuration` = `0.5`: seconds to settle back.
- `punchColor` = white: the flash color of the number.
- `pulseScale` = `1.6`, `pulseAlpha` = `0.7`: size and opacity of the glow.
- `badgeFadeDuration` = `0.3`: fade of the badge in and out.
- `bannerSlamDuration` = `0.28`, `bannerStartScale` = `2.6`: how the banner text lands.
- `bannerHoldDuration` = `0.9`, `bannerFadeDuration` = `0.4`: how long it stays and leaves.
- `bannerRiseDistance` = `70`: pixels the text drifts up while leaving.

In `GameFeelConfig`: `waveStartVolume` = `0.7`.

Colors, sizes and layout are the scene objects' own (gold `1, 0.8, 0.2`, badge 320 x 120, banner at 68% of the screen height).

## 8. Addressables
- **Used?** Yes, for the sound only. The HUD lives in the scene, like the start and game over screens.
- **Assets / addresses / group:** `Audio/WaveStart` in the `Audio` group.
- **Load:** at `GameFeelService.Initialize`, awaited before gameplay (UniTask).
- **Owner:** `GameFeelService`, in its handle list.
- **Lifetime / cache:** loaded for the whole session, played from one cached `AudioSource`. Nothing loads when a wave starts.
- **Release:** `GameFeelService.Reset` releases the handle and destroys the audio host.
- **Failure:** an unassigned or failing reference logs a warning and the wave start is silent. The HUD still works.
- **Remote readiness:** the `Audio` group uses the profile path variables, so moving it to a CDN is a configuration change. It is awaited at startup, so a remote group would need the download hidden behind a loading step.
- **Why Addressables here:** new runtime-loaded content, same pattern as the other sounds.

## 9. Generated assets
- **Used?** Yes.
- **`WaveStart.wav`** (about 1.3 s, 16 bit mono): a rising two-note brass fanfare ending on a chord, with a low boom under it. Synthesized with a short editor script (harmonics, a slight detune and envelopes), deleted after use. Generated because the project had no wave sound. Integrated as the `Audio/WaveStart` Addressable and played by `GameFeelService.PlayWaveStart`. Replacement cost: low, replace the file and keep the address. Placeholder: not heard by the author, only its playback was verified as wired.
- The HUD itself uses Unity's built-in rounded UI sprite and the project's TextMeshPro font and outline material, no generated image. It was built with a one-off editor script, deleted after use.
- Time is included in implementation.

## 10. Edge cases and limits
- **Start screen and restart:** the HUD resets and hides when the flow returns to Start, so the next round begins at wave 1.
- **Hit stop:** the animations use unscaled time, so a freeze frame does not stall them.
- **Wave change during the banner:** the animations restart from the beginning, they never stack.
- **Last wave:** it repeats until the hero dies, so the counter keeps its number and no new banner appears.
- **Missing sound or services:** silent, nothing breaks.
- **Verified in play mode:** the banner slam, the ribbon, the badge fade-in and the number punch (wave 2 was sent through the view's handler, because the hero died before wave 2 in the test). No console warnings or errors.
- **Not verified:** the sound was not heard. Layout was checked on one portrait Game view, not on other aspect ratios or with a notch.
- **Deliberately not built:** a countdown to the next wave, a "final wave" banner, camera shake on a new wave, a pooled or prefab HUD.

## 11. Base project observations
- `GameFlowService.Initialize` resets the state to Start. A call to `StartGame` made before all services are initialized is silently lost.
- The wave spawner (another session's work) had no event, so anything reacting to waves needed one added. Several sessions edit the same shared files (`GameFeelService`, `GameFeelConfig`, the config asset, the scene) in one working tree.

## 12. Likely review questions
- **Why does the view read the spawner and not the gameplay state?** A wave is the spawner's concept. One event with the wave number is the smallest link, and the spawner knows nothing about the UI.
- **Why is the sound played by the view and not by the spawner?** Presentation never drives gameplay. The view reacts to the event and asks the feedback service for the sound.
- **Why hand-written easing and no tween library?** Two small functions and one timer per animation cover it. No new dependency, no allocations, and a new wave simply resets the timers.
- **Why unscaled time?** Hit stop drops the time scale for a few frames. A scaled HUD would stutter exactly when the game feels heaviest.
- **Why is the HUD not an Addressable prefab?** It is a permanent screen like the start screen and the game over overlay, so it follows their convention. The sound is the only runtime-loaded content.

## 13. 30-second explanation
Waves were invisible to the player, so the game's escalation was hard to read. A small badge now shows the wave number and a big banner with a fanfare announces each new one, with a punch and a glow on every change. One view listens to a new `OnWaveStarted` event of the spawner and drives two timer-based animations on unscaled time. The trade-off is that it only announces: no countdown to the next wave and no distinct final wave.

## 14. README entry

### Wave counter — 7 min
- **Implemented:** to explain to the player what is going on, a "WAVE n" badge now sits at the top of the screen and a big banner with a short fanfare announces each new wave. The number punches and a glow pulses on every change. Details in [the feature note](Documentation/FeatureNotes/23_WaveCounter.md).
- **Time:** 7 min
- **Bottleneck:** not confirmed.
- **Addressables:** the generated stinger is preloaded from the `Audio` group.
