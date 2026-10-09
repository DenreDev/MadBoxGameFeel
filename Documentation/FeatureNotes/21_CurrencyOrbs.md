# FEATURE: Currency orbs

## 1. Summary
When an enemy dies it bursts into 3 to 5 tiny blue orbs. They hop out in a ring, bounce, rest for under a second, then recoil, curve into the hero and are collected with a sparkle and a rising blip. Orbs carry no value yet: it is feedback only.

## 2. Why / issue fixed
- **Why (from the human):** to increase the feeling of killing an enemy.
- **Issue fixed (deduced from that statement):** a kill ended with the enemy flying away and nothing else, so it lacked a satisfying payoff. A drop that is then pulled to the hero gives the kill a follow-up and a small reward moment (drop, attract, gather).

> The issue wording is deduced from the human's statement, to be confirmed.

## 3. Bottleneck
Not given by the human. Deduced from the work, not from their statement: getting the look right from the gameplay camera. The first orbs were too small to read, then too big, green (read as health) and too present. It took three size and colour passes.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `1 min 58 s` (10:28:41 → 10:30:39) |
| Implementation | `4 min 00 s` (10:30:39 → 10:34:39) |
| Tuning / testing | `5 min 13 s` (10:34:39 → 10:38:52, then 10:40:45 → 10:41:45) |
| Asset generation / integration | included in implementation (no separate timestamp) |
| **Total** | `8 min` (human-stated) |

The total is the value stated by the human. The phase rows are the recorded timestamps, which sum to `11 min 11 s` and do not add up to the total: the stated total is the one used in the README. The last size pass (smaller, fainter orbs) has no end timestamp and is not counted. Waiting for the human's feedback between passes is not counted. Testing is two play-mode probe runs on a shared editor, with another session's play test waited out first.

## 5. Files changed
- `CurrencyOrbView.cs`: new. One orb: phases Burst, Rest, Windup, Magnet, Vanish. Billboard, glow, ground shadow, trail.
- `GameFeelService.cs`: loads the orb, sparkle and two sounds. `PlayCurrencyDrop`, `PlayCurrencyCollected` (sparkle, chained pitch), `ClearCurrency`, `CurrencyTarget`. Owns the `CurrencyOrbs` root object. Releases everything in `Reset`.
- `GameFeelConfig.cs` and `GameFeelConfig.asset`: four content references and about 25 tuning values.
- `EnemyView.cs`: `PlayDeath` asks the service for the drop.
- `HeroView.cs`: registers the hero as the orbs' target, clears the orbs when he dies.
- `CurrencyOrb.prefab`, `CurrencyCollect.prefab`, materials, texture, two WAV files under `Features/GameFeel/View/Local/`.
- `VFX.asset`, `Audio.asset`: four new Addressable entries.

Gameplay controllers and game rules were not touched.

## 6. Technical flow
1. At startup `GameFeelService.Initialize` loads the orb prefab, the collect sparkle and two clips, and keeps the orb prefab's `CurrencyOrbView`.
2. `HeroView` sets `CurrencyTarget` to the hero's transform.
3. An enemy is killed: `EnemyView.PlayDeath` calls `PlayCurrencyDrop(position)`. The service spawns 3 to 5 orbs under a `CurrencyOrbs` root, spread evenly in a ring with jitter, each with its own burst velocity, and plays a soft pop.
4. Each orb hops under its own gravity, squashes and bounces on the floor until the bounce is too small, then rests with a slow bob and a pulsing glow. Its shadow shrinks with height.
5. After `AttractDelay` plus a random jitter it winds up: squash, glow swell and a short recoil away from the hero.
6. It then flies to a point on the hero's chest. Speed ramps up, the velocity is steered toward the hero, and the sideways kick of the start makes the path curve. The orb grows briefly, leaves a trail and shrinks as it arrives.
7. Within `CollectRadius` it is collected: `PlayCurrencyCollected` spawns the sparkle and plays the blip. The pitch rises with each orb collected within `CurrencyChainWindow`, capped. One blip per frame.
8. The hero dies: `ClearCurrency` makes every orb shrink away. Orbs never wait for a dead hero or leak into the next round.

## 7. Tuning values
In `GameFeelConfig`:
- `currencyOrbsPerKill` = `3 to 5`: orbs per kill. Lower this first if the screen feels busy.
- `currencyDropHeight` = `0.6`, `currencyBurstSpeed` = `2.5 to 4.5`, `currencyBurstUpSpeed` = `6`, `currencyGravity` = `25`, `currencyBounciness` = `0.45`, `currencyRestHeight` = `0.2`: the shape of the drop.
- `currencyAttractDelay` = `0.8` and `currencyAttractDelayJitter` = `0.35`: rest time and stagger.
- `currencyWindupDuration` = `0.18`, `currencyWindupDistance` = `0.4`: the recoil before leaving.
- `currencyAttractAcceleration` = `45`, `currencyMaxSpeed` = `20`, `currencyAttractCurve` = `3`: the flight. Keep the top speed above the hero's move speed (5).
- `currencyTargetHeight` = `1`, `currencyCollectRadius` = `0.5`: where and when an orb counts as collected.
- `currencyDropVolume` = `0.5`, `currencyCollectVolume` = `0.6`, `currencyChainWindow` = `0.4`, `currencyChainPitchStep` = `0.05`, `currencyMaxPitch` = `1.6`: sound.

In the prefabs: orb size (`Body` quad, 0.36), glow, shadow, trail, and the sparkle's count, size and colours.

## 8. Addressables
- **Used?** Yes.
- **Assets / addresses / group:** `VFX/CurrencyOrb`, `VFX/CurrencyCollect` in `VFX`. `Audio/CurrencyDrop`, `Audio/CurrencyCollect` in `Audio`. The materials and the texture are implicit dependencies bundled with the prefabs.
- **Load:** at `GameFeelService.Initialize`, awaited before gameplay (UniTask).
- **Owner:** `GameFeelService`, in its handle list.
- **Lifetime / cache:** loaded for the whole session. Every orb and sparkle is an `Instantiate` of the cached prefab, nothing loads on a kill.
- **Release:** `GameFeelService.Reset` releases the handles and destroys the `CurrencyOrbs` root.
- **Failure:** an unassigned or failing reference logs a warning. No orb prefab means no drop, a missing sparkle or clip only drops that part. A prefab without `CurrencyOrbView` logs a warning and disables the orbs. Gameplay goes on.
- **Remote readiness:** the groups use the profile path variables, so moving them to a CDN is a configuration change. The loads are awaited at startup, so a remote group would need the download hidden behind a loading step.
- **Why Addressables here:** new runtime-loaded content, same pattern as the other feedback assets, replaceable or remotely served without code changes.

## 9. Generated assets
- **Used?** Yes.
- **Orb sprite** (`CurrencyOrbTexture.png`, 128 px): a cartoon orb with a navy outline, cyan-to-blue fill and white highlights. Generated with a short script (no image library) because the project had no such sprite. Replacement cost: low, replace the texture, the material keeps working.
- **`CurrencyDrop.wav`, `CurrencyCollect.wav`** (about 14 and 19 KB): a bubbly pop and a bright two-note blip, synthesized with sine sweeps and envelopes. Placeholders, replace the files and keep the addresses.
- **Prefabs and materials:** built with a one-off editor script, deleted after use. The orb is three billboard quads (shadow, glow, body) and a trail. The sparkle is two particle systems (ten dots and a soft flash).
- Time is included in implementation.

## 10. Edge cases and limits
- **Hero dead:** orbs shrink away, none wait for him. Drops are skipped when there is no target.
- **Freeze frames:** orbs move with `Time.deltaTime`, so they freeze with the hit stop, which looks right.
- **Orb count:** no cap and no pool. Orbs are instantiated and destroyed. Fine at the tested rate (up to about 10 alive), but a much bigger crowd would grow it.
- **No value or counter:** orbs are feedback only. Nothing is added up.
- **Verified in play mode:** the orbs spawn, bounce, fly with a trail and are collected (21 to 25 collected in 14 s runs), no console errors. Frames were checked visually after each size pass.
- **Not verified:** the sounds were not heard, only their playback was wired. The collect sparkle and the hero's contact point were not seen in a captured frame.
- **Colour contrast:** blue on the teal floor relies on the glow and the white highlight to stand out.
- **Deliberately not built:** a currency counter, a hero pulse on collect, orb pooling, a magnet radius, orb value.

## 11. Base project observations
- The services are used through `ServicesLocator` but the `Game.GameFeel` assembly cannot reference gameplay (`Game.Entities` references it), so anything feedback-side that needs the hero has to be handed a transform. The hero view registers itself on the service for that.
- Several sessions edit the same shared files (`GameFeelService`, `GameFeelConfig`, the config asset, the Addressable groups) in one working tree, which makes small isolated commits hard.

## 12. Likely review questions
- **Why is it not a gameplay system?** There is no currency to spend and no rule attached, so a controller and events would be code without a use. The seam is clear: `PlayCurrencyDrop` is the one entry point, and a controller could raise a `Collected` event later.
- **Why does `HeroView` set `CurrencyTarget` on the service?** The feedback assembly cannot reference the entities assembly, and polling the controller would need that reference. A transform is the smallest dependency.
- **Why a velocity-steered flight and not a tween?** It keeps working when the hero moves, the sideways kick gives a curve for free, and acceleration is two numbers.
- **Why one `Update` per orb?** At a few dozen orbs it is negligible, and each orb stays a self-contained state machine. A manager loop would only pay off at hundreds.
- **Why a billboard sprite and not a 3D sphere?** An unlit sphere has no shading and reads as a flat disc. A sprite carries the outline and highlights, and fits the cartoon style.

## 13. 30-second explanation
A kill ended with nothing to collect. Now the dead enemy bursts into a few tiny blue orbs that hop, rest, then wind up and curve into the hero, with a sparkle and a rising blip on each pickup. One small view runs each orb's phases, and `GameFeelService` spawns them, owns the Addressable content and the sounds, and clears them when the hero dies. The trade-off is that it is feedback only: no value, no counter, no pooling.

## 14. README entry

### Currency orbs — 8 min
- **Implemented:** to increase the feeling of killing an enemy, dead enemies now drop a few tiny blue orbs that bounce, rest a moment, then recoil and curve into the hero, with a sparkle and a pitch-rising blip on each pickup. Feedback only: orbs have no value yet. Details in [the feature note](Documentation/FeatureNotes/21_CurrencyOrbs.md).
- **Time:** 8 min
- **Bottleneck:** not confirmed.
- **Addressables:** the orb and sparkle prefabs in `VFX`, two generated sounds in `Audio`, all preloaded.
