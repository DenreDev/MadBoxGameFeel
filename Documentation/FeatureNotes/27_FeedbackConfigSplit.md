# FEATURE: Feedback config split (refactor)

## 1. Summary
The single `GameFeelConfig` (95 fields, 11 unrelated feedbacks) and the `GameFeelService` that did everything are gone. Each feedback is now its own small ScriptableObject, plugged into the gameplay content it belongs to (`EnemyConfig`, `HeroConfig`, `WeaponConfig`). A few small services each own one shared runtime concern. No player-facing change: every value was migrated as is.

## 2. Why / issue fixed
From the human's request, polished:
- **Why:** `GameFeelConfig` had become a god object holding the feedback of every game system. That does not match how I build things. Each feedback should stand on its own, by composition, and be connected through ScriptableObjects to the gameplay feature it serves.
- **Issue fixed:** a scalable product needs robust code that the whole team can work in. With one shared config and one shared service, every new feedback edited the same two files, hero and enemies could not be tuned apart, and every weapon felt the same.

## 3. Bottleneck
> Not given yet. To be confirmed by the human.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis (audit and design question to the human) | `5 min 28 s` (11:14:00 → 11:19:28) |
| Implementation | `10 min 52 s` (11:19:28 → 11:30:20) |
| Tuning / testing (compile and static checks) | `3 min 00 s` (11:30:20 → 11:33:20) |
| Sync with `main` (merge, port the wave, combo and crowd zoom feedback, re-validate) | `5 min 02 s` (18:33:28 → 18:38:30) |
| Asset generation / integration | none |
| **Total** | `24 min 22 s` |

Real elapsed time from recorded timestamps. Writing this note and the README is not counted. The refactor was first built on `530da97`. `main` then gained waves, combos, crowd zoom and enemy pooling, which also wrote to `GameFeelConfig`, so the branch was merged with `main` and those additions were moved into the split too.

## 5. Files changed
- **Removed:** `GameFeelConfig.cs`, `GameFeelConfig.asset`, `GameFeelService.cs`.
- **New feedback configs** (`GameFeel/Scripts/Config/`, menu *Create > Feedback*): `HitFeedbackConfig`, `DamageNumberConfig`, `SpawnFeedbackConfig`, `AttackTelegraphConfig`, `FootstepFeedbackConfig`, `CurrencyDropConfig`, `WeaponSoundsConfig`, `HitStopConfig`, `AttackRangeConfig`, `CameraShakeConfig`, `StartCameraConfig`, `CrowdZoomConfig`, `WaveSoundsConfig`, `ComboSoundsConfig`.
- **New assets** (`GameFeel/Config/`): `HeroHitFeedback`, `HeroFootsteps`, `HeroComboSounds`, `BeeHitFeedback`, `BeeDamageNumbers`, `BeeSpawnFeedback`, `BeeAttackTelegraph`, `BeeCurrencyDrop`, `WaveSounds`, `SwordSounds`, `SwordHitStop`, `SwordAttackRange`, `CameraShake`, `StartCamera`, `CrowdZoom`.
- **New services:** `FeedbackAssetsService` (the Addressables owner), `FeedbackAudioService` (2D one-shots), `HitStopService` (time scale), `CurrencyFeedbackService` (orb root, target, collect chain), `FeedbackPreloadService` (walks the catalogs, in `Game.Entities`).
- **New view:** `WeaponFeedbackView` on the hero prefab: swing whoosh, impact sounds and freeze frame of the current weapon.
- **Content configs:** `EnemyConfig`, `EnemiesConfig` (wave stinger), `HeroConfig` (with the combo sounds) and `WeaponConfig` gained a *Feedback* header with their feedback references. All 3 bees and all 3 swords use the same assets, so nothing changes in game.
- **Views:** `HitFeedbackView`, `SpawnFeedbackView`, `AttackTelegraphView` and `FootstepFeedbackView` get their config through `Bind(...)` from the owner view. `AttackRangeView` reads the current weapon's look. `CameraShakeView`, `StartCameraView` and `CrowdZoomView` have a serialized config on the World prefab. `WaveCounterView` and `ComboCounterView` read their sounds from `EnemiesConfig` and `HeroConfig`. The pooled `EnemyView` binds its feedback in `Appear()`, on every spawn. `CurrencyOrbView` receives its drop config at launch.
- **asmdefs:** `Game.Weapons` references `Game.GameFeel`. `Game.Entities` references `Unity.Addressables`.

### Where each old `GameFeelConfig` group went

| Old group | New home | Plugged into |
|---|---|---|
| Hit feedback, enemy recoil, crit spark and impact | `HitFeedbackConfig` | `HeroConfig`, `EnemyConfig` |
| Damage numbers, crit number look | `DamageNumberConfig` | `EnemyConfig` |
| Spawn feedback | `SpawnFeedbackConfig` | `EnemyConfig` |
| Attack telegraph | `AttackTelegraphConfig` | `EnemyConfig` |
| Currency orbs (content, drop, attract, sound) | `CurrencyDropConfig` | `EnemyConfig` |
| Footsteps | `FootstepFeedbackConfig` | `HeroConfig` |
| Hero ring prefab | `GroundRingPrefab` field | `HeroConfig` |
| Attack sounds | `WeaponSoundsConfig` | `WeaponConfig` |
| Hit stop, crit freeze | `HitStopConfig` | `WeaponConfig` |
| Attack range look | `AttackRangeConfig` | `WeaponConfig` |
| Camera shake, crit shake multiplier | `CameraShakeConfig` | `CameraShakeView` (World prefab) |
| Start screen camera | `StartCameraConfig` | `StartCameraView` (World prefab) |
| Crowd zoom | `CrowdZoomConfig` | `CrowdZoomView` (World prefab) |
| Wave counter content (stinger) | `WaveSoundsConfig` | `EnemiesConfig` (the waves' catalog) |
| Combo counter content (bell, tier, break, pentatonic climb) | `ComboSoundsConfig` | `HeroConfig` (the combo is the hero's kill streak) |

Older feature notes (05 to 25) still name `GameFeelConfig` and `GameFeelService`. This table is the mapping.

## 6. Technical flow
1. **Startup.** `ServicesLocator` initializes the services. `FeedbackPreloadService` walks `HeroConfig`, `EnemiesConfig.Enemies` and `WeaponsConfig.Weapons`. For each feedback config plugged in, it calls `PreloadAssets(assets)`, and the config passes its own `AssetReference`s to `FeedbackAssetsService.Preload`.
2. **Spawn.** `EnemiesContainerView` takes the enemy from its pool and calls `EnemyView.Appear(enemyConfig, ...)`. That hands each feedback component its config (`hitFeedback.Bind(config.HitFeedback, config.DamageNumbers)`, and so on). `HeroView` does the same with `HeroConfig` once the services are ready.
3. **Play.** Feedback views read their tuning from the bound config and their assets synchronously with `assets.Get(...)` or `assets.Spawn(...)`. Those calls are already loaded, never loading.
4. **Weapon blows.** `WeaponFeedbackView` listens to `OnAttackStarted`, `OnEnemyDamaged` and `OnEnemyKilled`. It reads `WeaponsService.CurrentWeapon.Sounds` and `.HitStop`, counts the kill streak, asks `HitStopService.Freeze(duration, timeScale)` and plays the sounds through `FeedbackAudioService`.
5. **Weapon swap.** `AttackRangeView` already rebuilds on `OnWeaponChanged`. It now also takes the new weapon's `AttackRangeConfig`. Sounds and hit stop are read on each blow, so they follow the swap with no extra code.
6. **Shutdown.** Services reset in reverse order. `FeedbackAssetsService` releases every handle, `HitStopService` restores the time scale, and the audio host and orb root are destroyed.

Rule of thumb for the team: **a feedback = one config SO + one view component**. It is plugged into the content config it belongs to, and its assets are listed in its own `PreloadAssets`.

## 7. Tuning values
Unchanged, only moved (see the table above). New capabilities that come from the split:
- Each enemy type can have its own hit reaction, numbers, spawn, telegraph and drop. For example, `BeeStrong` could drop more orbs or flinch less.
- Each weapon can have its own sounds, freeze and cone look.
- Any feedback can be removed from one piece of content by leaving its slot empty.

## 8. Addressables
- **Used?** Yes (same assets, new ownership).
- **Assets / addresses / group:** unchanged: `VFX/*` in `VFX`, `Audio/*` in `Audio`.
- **Load:** at startup, from `FeedbackPreloadService`, before any view subscribes (`OnAllServicesInitialized`).
- **Owner:** `FeedbackAssetsService`, the only class that calls `Addressables.LoadAssetAsync` for game feel.
- **Lifetime / cache:** handles are cached by runtime key, so an asset shared by several configs is loaded once (the hero and the bees share `VFX/HitSpark`). Lookups are synchronous dictionary reads.
- **Release:** `FeedbackAssetsService.Reset`, after clearing the third-party package pool built from the number prefab.
- **Failure:** unassigned or failed references log a warning that names the config, and `Get` returns null. Every caller skips its feedback on null. Gameplay is never blocked.
- **Remote readiness:** unchanged. Switching `VFX` or `Audio` to remote paths is a profile change. Preloading from the catalogs means new content brings its own assets into the preload without code.
- **Why Addressables here:** same reasons as the original features. This refactor makes the preload follow the content: an enemy type that is not in the catalog loads nothing.

## 9. Generated assets
- **Used?** No.

## 10. Edge cases and limits
- **Kamikaze explosion:** `EnemyView.PlayExplode` reuses the enemy's own spawn puff and hit spark (from its `SpawnFeedbackConfig` and `HitFeedbackConfig`) instead of having a config of its own.
- **Null config:** every feedback slot is optional. An empty slot disables that feedback for that content only.
- **Kill streak without a `HitStopConfig`:** only kills in the same instant chain (window 0), so the kill sound does not climb.
- **Small behavior change:** `FeedbackAudioService` plays a given clip at most once per frame, for every clip. Before, the currency drop pop had no such guard, so two enemies dying in the same frame now give one pop instead of two stacked ones.
- **Not validated in play mode by the agent.** This worktree has no Unity Library and the open editor belongs to another checkout. Validated by compiling the 5 changed assemblies (GameFeel, Weapons, World, Entities, UI) with Unity 2022.3's Roslyn against the project's compiled dependencies, plus a script check: every GUID resolves, every Addressable reference is in a group, and every serialized key matches a C# field. **A play test in the editor is still needed.**
- **Deliberately not built:** no feedback base class or interface (the preloader lists each slot explicitly), no per-weapon camera shake (the shake stays a camera setting), and no editor tooling.

## 11. Base project observations
- The base project's `ServicesLocator` discovers services by reflection, so splitting one service into four costs no registration code.
- `BeeSmall` and `BeeStrong` are prefab variants of `BeeNormal`, so they inherit the feedback components, but their `EnemyConfig`s had no way to differ in feel until now.

## 12. Likely review questions
- **Why do the feedback configs live in `GameFeel` and not next to each feature?** They are generic: `HitFeedbackConfig` serves both the hero and the enemies. What is feature-specific is *which* asset is plugged in, and that lives in the feature's content config.
- **Why a separate preload service instead of letting each view load its assets?** The views of a bee only exist once it spawns, which is too late: the first spawn puff would be missed. The catalogs are known at startup. `FeedbackPreloadService` lives in `Game.Entities` because it is the assembly that sees all three catalogs (`Game.GameFeel` cannot reference it without a cycle).
- **Why do configs have a `PreloadAssets` method instead of the preloader reading their fields?** A config's list of assets sits next to the fields that declare them. Adding an asset to a feedback is a one-file change.
- **Why `HitStopService` if `WeaponFeedbackView` decides the freeze?** `Time.timeScale` is global. One owner guarantees overlapping freezes merge (the longest wins) and the time scale is always restored, whoever asks.
- **Why not one `WeaponFeedbackConfig` holding sounds, hit stop and the cone?** Those are three feedbacks with different consumers. Separate assets let a new weapon reuse the sword's sounds but get its own freeze.

## 13. 30-second explanation
`GameFeelConfig` and `GameFeelService` handled the feedback of every system, so every feature edited the same two files and nothing could be tuned per enemy or per weapon. I split it by feedback: one ScriptableObject per feedback, plugged into the content it belongs to. Enemies hold their hit, numbers, spawn, telegraph and drop. The hero holds his hit, footsteps and ring. Weapons hold their sounds, freeze and cone. One service owns the Addressables handles, preloaded at startup by walking the content catalogs. The trade-off is more small files, but each one has a single reason to change.

## 14. README entry

### Feedback config split (refactor) — 24 min
- **Implemented:** the all-in-one `GameFeelConfig` and `GameFeelService` were split by composition: each feedback is its own ScriptableObject, plugged into the enemy, hero or weapon config it belongs to, so each enemy type and weapon can feel different. No player-facing change. Details in [the feature note](Documentation/FeatureNotes/27_FeedbackConfigSplit.md).
- **Time:** 24 min (19 min refactor, 5 min to merge it with the latest main)
- **Bottleneck:** not confirmed.
- **Addressables:** one service owns every game feel handle, preloaded at startup from the content catalogs.
