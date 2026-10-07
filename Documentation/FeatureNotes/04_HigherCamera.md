# FEATURE: Higher Camera Angle

## 1. Summary
The gameplay camera is now higher and steeper: pitch 57.7° → 65° and Cinemachine camera distance 14 → 16. The player sees more of the arena around the hero, so enemies show up earlier. No code, only two values on the World prefab.

## 2. Why / issue fixed
The original camera was too focused on the player, so the player was surprised by enemy spawns. A higher camera shows more of the arena around the hero. It should also help the enemy spawn feeling that will be added later.

## 3. Bottleneck
The camera was too focused on the player, so enemy spawns came as a surprise.

> Deduced from the human's statement on the issue, at the human's request.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis + implementation (first try 70° / 18, then the in-between 65° / 16) | `2 min` (10:47:11 → 10:49:10, 11:25:50 → 11:26:05) |
| Tuning / testing (geometry check, play-mode screenshots) | `6 min` (10:49:10 → 10:52:49, 11:26:05 → 11:28:12) |
| Asset generation / integration | `0 min` |
| **Total** | `8 min` |

Real elapsed time from recorded timestamps. 10:47:11 is the session creation time. The idle gap between the two passes (waiting on the human, 10:52 → 11:25) is excluded, and so is the time spent writing this note. Analysis and implementation have no separate timestamp between them, so they are reported as one block rather than estimated. The second pass starts at the first timestamp recorded after the request.

## 5. Files changed
- `World.prefab`:
  - `VirtualCamera` rotation X `57.7°` → `65°` (yaw stays `180°`). Its stored position is synced to the new pose for the editor. The Framing Transposer overrides it at runtime.
  - `VirtualCamera/cm` Framing Transposer `Camera Distance` `14` → `16`.

## 6. Technical flow
1. The `VirtualCamera` on the World prefab is the camera the `CinemachineBrain` on the scene's Main Camera follows. `HeroContainerView` sets its `Follow` target to the hero.
2. There is no Aim component, so the camera rotation is the `VirtualCamera` transform rotation (pitch 65°, yaw 180°). The Framing Transposer only moves the camera.
3. The Framing Transposer places the camera `Camera Distance` (16) away from the tracked point (hero + `1.3` up), opposite to where it looks. Height is `1.3 + 16 × sin 65° ≈ 15.8` and it sits `16 × cos 65° ≈ 6.8` behind the hero.
4. At game start, `StartCameraView` (start screen feature) shows a close-up, then pulls back to the distance set on the prefab's Framing Transposer. The value above stays the single source of truth.
5. Health bars copy the camera rotation every frame, so they follow the new angle with no change.

## 7. Tuning values
- `VirtualCamera` rotation X (pitch) = `65°`: higher is more top-down and balances what is seen ahead and behind the hero. Lower is flatter and sees farther ahead.
- Framing Transposer `Camera Distance` = `16`: higher shows more of the arena on every side and makes the hero smaller.
- Framing Transposer `Tracked Object Offset` Y = `1.3`: unchanged.

Distance from the hero at which an enemy first appears on screen. Computed from the camera geometry (portrait 505×897, vertical FOV 60), not measured in game:

| | Original (57.7°, 14) | **Now (65°, 16)** | First try (70°, 18) |
|---|---|---|---|
| From the side | 4.9 | **5.5** | 6.2 |
| From behind | 6.9 | **8.1** | 9.3 |
| From ahead | 17.5 | **15.8** | 15.5 |
| Spawn ring (radius 10) visible at spawn | 21% | **23%** | 31% |
| Hero size on screen | 100% | **88%** | 79% |

## 8. Addressables
- **Used?** No.
- **Why not:** nothing is loaded at runtime. Only two serialized values on an existing prefab changed.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- **Portrait view:** the game is portrait, so the horizontal view is narrow and the sides are the limit. Pitch mostly balances ahead against behind. Only a larger distance widens the sides.
- **Arena edge:** the floor is a plane scaled to 6000 units, so the higher view cannot show an edge.
- **Hero size:** the hero is about 12% smaller than in the original camera. This is the accepted trade-off.
- **Scene Main Camera:** its transform in `MainScene` still has the old pose. The Cinemachine brain overrides it from the first frame, so it was left alone.
- **Start screen:** `StartCameraView` reads the prefab distance as the gameplay distance, so this change and the start screen do not conflict.
- **Validation:** play-mode run after pressing start: the hero is readable, bees come in from the sides and behind, health bar and damage numbers are placed correctly, no console errors or warnings.
- **Deliberately not built:** a camera look-ahead toward the movement direction and any dynamic zoom. Proposed, not approved.

## 11. Base project observations
- The original camera is very asymmetric: it sees 17.5 units ahead but only 6.9 behind and 4.9 to the sides.
- The game is portrait with a vertical FOV of 60, so the view is narrow, while enemies spawn on a 10-unit radius around the hero (`EnemiesConfig.spawnRadius`). Most spawns start off screen.
- The camera angle is stored twice: on the World prefab's `VirtualCamera` and on the scene's Main Camera. Only the first one has any effect at runtime.
- A Cinemachine Impulse Listener sits on the same `VirtualCamera`, so the camera shake keeps working with the new angle.

## 12. Likely review questions
- **Why change both pitch and distance?** The view is portrait, so the sides are the weak spot and only distance widens them. Pitch balances ahead against behind. Changing only one would fix half of the problem.
- **Why 65° / 16 and not 70° / 18?** 70° / 18 gave more warning (31% of the spawn ring visible instead of 23%) but shrank the hero to 79%. The in-between keeps the hero at 88% and still sees more than the original in every direction except straight ahead.
- **Why no config value or code?** Both values are already tunable in the inspector on the prefab, and `StartCameraView` reads that distance. A second config would duplicate the source of truth.
- **How do you know it is better?** The visibility distances per direction were computed from the camera geometry, then checked in a real play-mode run with enemies on screen.

## 13. 30-second explanation
The original camera was too close to the hero, so the player was surprised by enemy spawns. I raised the pitch from 57.7° to 65° and the distance from 14 to 16 on the existing virtual camera. Enemies now appear 12 to 17% earlier from the sides and behind. The trade-off is a hero about 12% smaller, and a first, higher camera was pulled back for that reason. No code was needed.

## 14. README entry

### Higher camera angle — 8 min
- **Implemented:** the gameplay camera is higher and steeper (65° instead of 57.7°, distance 16 instead of 14), so enemies coming from the sides and behind show up earlier while the hero stays readable. Details in [the feature note](Documentation/FeatureNotes/04_HigherCamera.md).
- **Time:** 8 min
- **Bottleneck:** the camera was too focused on the player, so enemy spawns came as a surprise.
