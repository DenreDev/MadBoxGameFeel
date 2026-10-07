# FEATURE: Attack sounds

## 1. Summary
The hero's attack now has sound: a whoosh when the swing starts, a thump when the sword hits an enemy, and a heavier crunch when it kills, whose pitch rises with the kill streak. The three sounds are small generated placeholders loaded from a new `Audio` Addressables group.

## 2. Why / issue fixed
The attack still did not feel impactful enough, and the game had no sound at all, so a sword hit made no noise. Sound adds weight to a hit without touching the look or the camera that were already tuned.

> Deduced from the human's request ("try to improve the attack impact again"). Not confirmed word for word.

## 3. Bottleneck
Not given by the human. Deduced from the work: nothing could be heard at first because the base scene has no `AudioListener`. The play-mode probe showed the sources loaded but the console warned about it.

> To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | not timestamped |
| Implementation | `1 min 03 s` (21:58:26 → 21:59:29) |
| Tuning / testing | `6 min 01 s` (21:59:29 → 22:05:30) |
| Asset generation / integration | `18 s` (21:58:08 → 21:58:26) |
| **Total** | `7 min 22 s` (from 21:58:08) |

Real elapsed time from recorded timestamps. The analysis (reading the hit code and the current state left by other sessions) was not timestamped separately, so it is not counted. Testing includes five play-mode probe runs.

## 5. Files changed
- `GameFeelService.cs`: loads the three clips, owns three `AudioSource`s on one hidden object, `PlaySwordWhoosh`, `PlayHitSound`. Adds an `AudioListener` on that object when the scene has none. Releases everything in `Reset`.
- `GameFeelConfig.cs`: three clip references, three volumes, kill pitch step and cap.
- `HitFeedbackView.cs`: calls `PlayHitSound` next to the hit stop.
- `HeroView.cs`: calls `PlaySwordWhoosh` when the swing starts.

## 6. Technical flow
1. At startup `GameFeelService.Initialize` loads the clips and creates the sources.
2. The swing starts: `HeroView.OnHeroAttackStarted` plays the whoosh. Its loudest moment is about 0.28 s in, just before the slash lands at 0.33 s.
3. The slash damages enemies: each enemy's `HitFeedbackView` asks for the hit sound.
4. The service plays one thump and one crunch per frame at most, so a swing hitting three enemies is one blow. The kill sound pitch is `1 + (streak - 1) * step`, capped.
5. The kill streak is the one already counted for the freeze frame.

## 7. Tuning values
- `swordWhooshVolume` = `0.5`, `hitImpactVolume` = `0.8`, `killImpactVolume` = `1`: loudness of each sound.
- `killStreakPitchStep` = `0.06`: pitch gained per chained kill.
- `maxKillPitch` = `1.5`: highest kill pitch.
- The whoosh and the thump get a random pitch of 0.95 to 1.05 so repeated swings do not sound identical.

## 8. Addressables
- **Used?** Yes.
- **Assets / addresses / group:** `Audio/SwordWhoosh`, `Audio/HitImpact`, `Audio/KillImpact` in a new group `Audio` (same build and load paths as `VFX`).
- **Load:** at `GameFeelService.Initialize`, before gameplay.
- **Owner:** `GameFeelService` (handles in its list).
- **Lifetime / cache:** loaded once, the same clip is played every time.
- **Release:** in `GameFeelService.Reset`, together with the audio object.
- **Failure:** a missing or failed clip logs a warning and that sound stays silent. Gameplay goes on.
- **Remote readiness:** the group uses the profile path variables, so moving it to a remote profile is a configuration change.
- **Why Addressables here:** audio is the typical content that grows large and is worth serving remotely.

## 9. Generated assets
- **Used?** Yes, three 16-bit mono 44.1 kHz WAV files (20 to 40 KB each) in `Features/GameFeel/View/Local/SwordSounds`.
- **What:** a noise whoosh opening up, a low thump with a short click, a heavier thump with a crunch and a metallic ring.
- **Why generated:** the project had no audio. Made with a short Python script (noise, sine sweeps, envelopes).
- **How integrated:** Addressable entries above, referenced from `GameFeelConfig`.
- **Replacement cost:** low. Replace the WAV files, keep the addresses.
- **Time:** 18 s.

## 10. Edge cases and limits
- No `AudioListener` in the base scene: the service adds one on its own object when none exists. It is 2D audio, so its position does not matter.
- Several enemies hit in one frame: one thump and one crunch.
- Sounds are not slowed by the freeze frame (audio keeps its own time).
- Verified in play mode: the clips load, the whoosh starts with each swing, the thump and crunch start on hits and kills, the kill pitch rose with the streak (1.00, 1.06, 1.12).
- Not verified: how the sounds actually sound. The probe only sees that a source is playing, so volumes and the whoosh timing need a listen.
- The first enemy of a probe run had no hit feedback. Cause found: the probe started the game before all services were ready, so that enemy spawned before the enemies container subscribed and never got a view. It is a test artifact, not a bug in normal play, but it shows the spawn loop does not wait for the views.

## 11. Base project observations
- The scene has no `AudioListener` and the project has no audio at all.
- `EnemiesController` can spawn an enemy before `EnemiesContainerView` is subscribed, which leaves a hittable enemy without a view. In normal play the start screen delays this.

## 12. Likely review questions
- **Why three `AudioSource`s?** A pitch change on a shared source would bend sounds that are already playing. One source per sound keeps the kill pitch streak clean.
- **Why in `GameFeelService`?** It already owns the Addressable handles of the feedback content and the kill streak. Sound is another feedback of the same hit.
- **Why add the listener in code?** The base scene has none and other sessions edit the scene. The fallback keeps the feature self-contained, and it does nothing when a listener exists.
- **Why not `AudioSource.PlayClipAtPoint`?** It creates and destroys an object per call and cannot set the pitch.

## 13. 30-second explanation
The game had no sound, so sword hits had no weight. I generated three small placeholder sounds and load them from a new Addressables `Audio` group. The swing plays a whoosh, a hit plays a thump, a kill plays a heavier crunch that rises in pitch with the kill streak. The service owns the clips and the sources, the views only ask it to play. The trade-off is that the placeholders are synthetic and the sound was only verified by playback state, not by ear.

## 14. README entry

### Attack sounds — 7 min
- **Implemented:** the attack now has sound: a swing whoosh, a hit thump and a heavier kill crunch that rises in pitch with the kill streak. Details in [the feature note](Documentation/FeatureNotes/18_AttackSounds.md).
- **Time:** 7 min
- **Bottleneck:** not confirmed.
- **Addressables:** three generated sounds preloaded from a new `Audio` group.
