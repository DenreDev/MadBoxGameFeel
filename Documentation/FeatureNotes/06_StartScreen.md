# FEATURE: Start Screen and Game Flow

## 1. Summary
The game now launches into a non-gameplay Start state: a "main menu" framing with the camera low and close in front of the hero, and a pulsing "TAP TO START" prompt. Nothing moves or spawns until the player taps. A small game manager (`GameFlowService`) owns the phases `Start → Playing → GameOver → Start`, so Restart goes back to the same start screen.

## 2. Why / issue fixed
The game started automatically and did not wait for the player. This feature gives the game a proper launch flow, and it increases how much the player likes the hero, who is shown up close at the start.

> Human's statement, polished. It covers both the why and the issue fixed.

## 3. Bottleneck
Not documented. The human chose not to give one for this feature.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis (services, controllers, camera prefab, UI scene) | `4 min` (10:44:22 → 10:48:19) |
| Implementation, first pass (flow service, loop gating, camera view, start-screen UI) | `5 min` (10:48:19 → 10:50:31, 11:01:05 → 11:03:59) |
| Tuning / testing, first pass (play-mode probe on a shared editor) | `14 min` (11:03:59 → 11:18:08) |
| Restart + game-flow architecture + hero facing the camera (implementation and testing, not split) | `3 min` (11:29:59 → 11:32:57) |
| Menu-style camera framing + hero health bar hidden on Start (compile, play-mode checks) | `5 min` (first capture 16:01:33 → cleanup 16:05:10, plus the compile just before) |
| Asset generation / integration | `0 min` |
| **Total** | `31 min` |

Real elapsed time from recorded timestamps. Excluded: waiting on the human to free the shared editor (about 10:50:31 → 11:00:56, bounds taken from Unity's own state timestamps), the idle gaps between the three requests (11:18 → 11:29, 11:33 → about 16:00), and the time spent writing this note. To confirm with the human: the code edits of the last pass (camera pitch and config values) happened before the Unity connection dropped and were not timestamped, so they are not in the 5 min.

## 5. Files changed
New:
- `GameFlowService.cs` (+ `Game.GameFlow.asmdef`): the game manager. `GameState` enum, three transitions, `OnStateChanged`.
- `StartCameraView.cs` (World prefab): close-up "menu" camera in Start, eases to the gameplay camera on Playing.
- `StartScreenView.cs` (new `StartScreen` object in `MainScene`): full-screen tap layer and pulsing prompt, fades with the state.

Changed:
- `EntitiesService.cs`: the only bridge between gameplay and flow. Hero died → `EndGame()`. Flow back to Start → clear enemies and revive the hero.
- `HeroController.cs`, `EnemiesController.cs`: loops tick only while Playing.
- `GameOverOverlayView.cs`: shows on GameOver. The Restart button now only calls `GameFlowService.Restart()` (it used to reset enemies and the hero itself).
- `HeroView.cs`: turns to the camera in Start, hides its health bar in Start.
- `GameFeelConfig.cs` and `GameFeelConfig.asset`: three start-screen values.
- `Game.Entities`, `Game.UI`, `Game.World` asmdefs: references to `Game.GameFlow` (and `Game.GameFeel` for World).
- `World.prefab`, `MainScene.unity`: `StartCameraView` component, `StartScreen` object.

## 6. Technical flow
1. `ServicesLocator` finds `GameFlowService` by reflection like every service. It starts in `Start`.
2. `StartCameraView.Awake` (on the World prefab) reads the gameplay distance and pitch from the prefab, then snaps the camera to the menu framing. No tilt-in at launch.
3. Views read the state: `StartScreenView` fades in and blocks raycasts, `HeroView` turns to the camera and hides its health bar, `GameOverOverlayView` stays hidden. The hero and enemy loops see `IsPlaying == false` and do nothing.
4. The player presses the screen. `StartScreenView.OnPointerDown` calls `StartGame()` (press, not release, so the response is instant).
5. `Start → Playing`: the screen fades out, the camera pulls back (distance through the transposer's Z Damping, pitch through `SmoothDampAngle`), the loops run and the first bee spawns on the next frame.
6. The hero dies: `HeroController.OnDied` → `EntitiesService` → `EndGame()` → `GameOver`. The overlay shows, the loops stop.
7. Restart button: `Restart()` → `Start`. `EntitiesService` is subscribed first, so it clears the bees and revives the hero before the views react. The start screen fades back in and the camera eases back to the menu framing.

## 7. Tuning values
In `GameFeelConfig`:
- `startCameraDistance` = `5`: how close the camera is on the start screen. Lower is a tighter portrait.
- `startCameraPitch` = `12`: camera angle in degrees on the start screen. Low means in front of the hero at face level, 90 is straight down.
- `cameraPitchSmoothTime` = `0.6` s: how long the tilt between start and gameplay angle takes.

On `StartScreenView`: `pulseSpeed` `0.8` per second, `pulseScale` `0.08`, `fadeDuration` `0.3` s.

Elsewhere, unchanged and still the source of truth: gameplay distance and pitch on the World prefab (see `04_HigherCamera.md`), Framing Transposer Z Damping `1` (the pull-back ease), `HeroView.rotationSpeed` `10` (also used to turn to the camera).

## 8. Addressables
- **Used?** No.
- **Why not:** nothing new is loaded at runtime. The start screen is scene UI, the camera is an existing prefab and the config is a Resources ScriptableObject like the others. A direct reference was the reliable choice.

## 9. Generated assets
- **Used?** No. The prompt pill uses Unity's built-in UI sprite and the project's existing TextMeshPro font.

## 10. Edge cases and limits
- **Wrong-state calls are ignored:** a double tap or a late `EndGame` cannot skip a phase (`Transition` checks the current state).
- **Missing virtual camera, transposer or config:** `StartCameraView` logs a warning and the start screen keeps the gameplay framing.
- **Teardown:** the new views check that `ServicesLocator.Instance` still exists in `OnDestroy`.
- **Subscription order matters:** `EntitiesService` subscribes to the flow in `Initialize`, before any view, so the round is reset before the views react to `Start`. It works, but it is an implicit dependency.
- **The starting touch also drives the joystick:** the press that starts the game also activates the joystick, so the player can keep dragging and move straight away.
- **Found in testing:** the hero's health bar floated over his face in the menu framing. It is now hidden in Start.
- **Validation:** play-mode runs with a throwaway script (deleted) that simulated a pointer-down. Checked: no spawn and no movement for 4.5 s in Start, hero facing the camera, UI raycast hits the start layer at the centre and corners, the first bee appears right after the tap, the loops freeze on GameOver, Restart gives 0 enemies, full health and the menu camera again, and the second tap starts gameplay again. Not tested: a real finger on a device.
- **Deliberately not built:** a title or logo, sound or extra juice on the tap, a hero intro animation, a second Cinemachine camera with Brain blending.

## 11. Base project observations
- Every view unsubscribes in `OnDestroy` with `ServicesLocator.Instance....`, but the locator is destroyed first, so each view throws a `NullReferenceException` when leaving play mode. The two new views guard it. The others were left alone.
- `EntitiesService.Reset()` did nothing, and the controllers' own `Reset()` is never called, so their loops are not cancelled at teardown.
- `GameOverOverlayView` used to reset gameplay itself (clear enemies, revive the hero). A view was driving gameplay rules, which is now the flow's job.
- `ServicesLocator.GetService` throws before the services are created, so anything that runs early has to wait for `OnAllServicesInitialized`.
- The hero model faces the root's -Z, so "look at X" is written as `LookRotation(-direction)` everywhere.

## 12. Likely review questions
- **Why a service for the flow and not a bool on the hero or a scene object?** Three views, two controllers and the camera all need the same answer. One service with three states and one event is a single source of truth. It knows nothing about heroes or views, so it stays easy to extend (pause, win, tutorial).
- **Why does `EntitiesService` bridge hero death to the flow?** `EntitiesService` already depends on the flow. If the flow subscribed to the hero instead, the two would depend on each other. The flow stays ignorant of gameplay.
- **Why do the loops poll the state instead of awaiting it?** The state now goes back and forth. Polling each frame lets the loops pause and resume on every restart, and the first bee appears on the very first frame of Playing.
- **Why rotate the existing virtual camera instead of a second one with Brain blending?** Only pitch and distance differ, and the hero is the follow target of the single prefab camera. The code is about 30 lines and the gameplay framing stays tunable on the prefab. A second camera is the Cinemachine-native alternative, and it would be the next step if the menu needed its own composition.
- **Why start on pointer down?** The response is immediate and the same touch carries on as joystick input.

## 13. 30-second explanation
The game used to start by itself, with no moment for the player to get ready. I added a small game manager with three states, Start, Playing and GameOver, and made it the one place that says whether the game is running. In Start nothing moves, the camera frames the hero close up at face level, and a prompt waits for the tap. The tap starts gameplay and the camera pulls back to the normal view. Dying leads to GameOver, and Restart returns to the same start screen. The main trade-off is that the camera is one virtual camera with its angle animated in code, which is simple but less flexible than a second Cinemachine camera.

## 14. README entry

### Start screen and game flow — 31 min
- **Implemented:** the game now waits on a "main menu" start screen: the camera frames the hero close up, facing the player, with a "TAP TO START" prompt, and nothing moves or spawns until the tap. A small game manager (Start, Playing, GameOver) drives it, so Restart comes back to the same screen. Details in [the feature note](Documentation/FeatureNotes/06_StartScreen.md).
- **Time:** 31 min
