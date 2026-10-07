# FEATURE: Hero Ground Ring

## 1. Summary
A thin, semi-transparent white ring sits on the ground under the hero and follows him. It is hidden on the start screen and while the hero is dead.

## 2. Why / issue fixed
To highlight the player's position more, so the player always knows where the hero is.

## 3. Bottleneck
Not given by the human. Agent observation, not the human's words: the first two play-mode probes were cut short by other sessions reloading the shared Unity editor, so the probe had to be rerun.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | not recorded |
| Implementation | not recorded |
| Tuning / testing | not recorded |
| Asset generation / integration | included in the total (texture, material, prefab) |
| **Total** | `7 min 58 s` (16:30:43 → 16:38:41, about 8 min) |

Real elapsed time from two `Get-Date` / `date` timestamps (first tool call and end of validation). Phase boundaries were not recorded, so no split is given. It excludes waiting for answers and writing this note.

## 5. Files changed
- `HeroRingView.cs` (new, on the Alice hero prefab): creates the ring under the hero once services are ready, shows or hides it from the game state and the hero state.
- `GameFeelService.cs`: loads the ring prefab at startup, new `CreateHeroRing(parent)`, releases it on `Reset`.
- `GameFeelConfig.cs` / `GameFeelConfig.asset`: new `heroRingPrefab` Addressable reference.
- `View/Local/HeroRing/`: `HeroRing.prefab`, `HeroRingMaterial.mat`, `HeroRingTexture.png`.
- `Alice.prefab`: added the `HeroRingView` component.

## 6. Technical flow
1. At startup `GameFeelService.Initialize` loads `VFX/HeroRing` through the config reference and caches it.
2. `HeroRingView.OnServicesInitialized` asks the service for a ring parented to the hero. No prefab means no ring, nothing else changes.
3. The ring is a flat quad lying just above the floor, so it follows the hero for free. A ring looks the same from every angle, so the hero's turns do not matter.
4. `HeroRingView` listens to `GameFlowService.OnStateChanged` and `HeroController.OnStateChanged`. The ring is active when the game is not on the start screen and the hero is alive.

## 7. Tuning values
- Ring size = the `HeroRing` prefab's scale (`2`, so about 1.7 units across the drawn ring): bigger ring around the hero.
- Ring color and alpha = `_BaseColor` of `HeroRingMaterial` (`white`, alpha `0.55`): lower alpha is more discreet, higher is more visible.
- Ring thickness and softness = baked into `HeroRingTexture.png`.

## 8. Addressables
- **Used?** Yes
- **Assets / addresses / group:** `VFX/HeroRing` (prefab), group `VFX`. The material and texture come with the prefab as dependencies.
- **Load:** during `GameFeelService.Initialize`, before gameplay.
- **Owner:** `GameFeelService` holds the handle.
- **Lifetime / cache:** loaded once, the hero gets one instance of the cached prefab.
- **Release:** `GameFeelService.Reset` releases the handle. The instance is a child of the hero and is destroyed with him.
- **Failure:** an unassigned or failed load logs a warning, there is no ring, gameplay is untouched.
- **Remote readiness:** the group uses the profile's load paths, so moving `VFX` to a CDN is a group setting. The ring is loaded before play, so a remote load would only delay startup.
- **Why Addressables here:** same loader and group as the other VFX, and the ring stays easy to replace or swap per hero.

## 9. Generated assets
- **Used?** Yes
- **HeroRingTexture.png:** 128x128 white ring with soft edges, generated with a short Python script (no extra libraries). It is a tintable shape, so the color lives in the material. Integrated through `HeroRingMaterial` and `VFX/HeroRing`. Replacement cost: drop in another white-on-transparent texture. Time: part of the feature total.
- **HeroRingMaterial.mat:** a copy of the spawn puff's transparent unlit material with the ring texture and a white alpha 0.55 color.

## 10. Edge cases and limits
- **Start screen:** the camera looks from the front there, the ring would be a thin line, so it is hidden.
- **Hero dead:** the ring is hidden, so it does not stay on the floor under the launched body. It comes back when the hero is alive again after a restart.
- **Missing asset or service:** no ring, no error.
- **Validated in play mode:** ring hidden on the start screen, visible and following the hero in gameplay, checked in a screenshot. The dead and restart cases were not run in play mode.
- **Deliberately not built:** pulsing or animated ring, ring color changes with health, ring under enemies.

## 11. Base project observations
- The floor is blue-teal, a cyan ring blended into it. White reads better on this biome.

## 12. Likely review questions
- **Why a quad and a texture, not a particle system or a projector?** It is one flat quad: cheap, nothing to simulate, easy to explain and replace.
- **Why Addressables for a ring?** To follow the project's content rules: same group, loader and lifetime as the other VFX.
- **Why is the ring a child of the hero?** It follows him with no per-frame code. The hero's root only moves and rotates around Y.
- **Why hide it on the start screen?** The camera is almost at eye level there, the ring would be an edge-on line.

## 13. 30-second explanation
The player could lose track of the hero among the enemies. A thin, semi-transparent white ring now sits under him. It is an Addressable prefab (`VFX/HeroRing`) loaded at startup by `GameFeelService`, instantiated under the hero by `HeroRingView`, which hides it on the start screen and when the hero is dead. Size and alpha are tuned on the prefab and its material.

## 14. README entry

### Hero ground ring — 8 min
- **Implemented:** a thin, semi-transparent white ring now sits on the ground under the hero, so the player always knows where he is. Details in [the feature note](Documentation/FeatureNotes/11_HeroRing.md).
- **Time:** 8 min
- **Bottleneck:** not recorded.
- **Addressables:** the ring prefab is preloaded from the `VFX` group.
