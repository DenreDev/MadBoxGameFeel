# FEATURE: Entity Animation State Machines (hero and bees)

## 1. Summary
The hero and the bees now play their existing Idle, Move, Attack, Damage and Death animations at the right moment. Each entity has an Animator state machine driven by events from the gameplay controllers.

## 2. Why / issue fixed
Entities were lacking feedback on their state. The player could not understand that the hero had attacked, for example. This fix lets the player understand the different hero and enemy states.

## 3. Bottleneck
Entities were lacking feedback on their state.

> Human's answer, kept as given. To confirm or refine before the review.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis + first implementation (hero Idle / Move / Attack) | `2 min` (10:12:47 → 10:14:36) |
| Implementation + testing of all entities (hero Damage / Death, bees) | `7 min` (10:21:21 → 10:28:27) |
| Asset generation / integration | `0 min` |
| **Total** | `9 min` |

Real elapsed time from recorded timestamps. The idle gap between the two sessions (waiting on the human, 10:14 → 10:21) is excluded. The second session has no separate timestamp between implementation and testing, so it is reported as one block rather than estimated.

## 5. Files changed
- `HeroAnimationController.controller`: added `Attack` and `Hit` triggers and an `IsDead` bool. Any State → Attack, Damage and Death. Attack and Damage are interrupted by movement (→ Run). Death → Idle on revive. The orphan `Weak` state is renamed `Attack`.
- `BeeAnimationController.controller`: was a skeleton with no parameters. Added `Speed`, `Attack`, `Hit` and `Die`. Idle ↔ Move on `Speed`. Any State → Attack, Damage and Die.
- `HeroController.cs`: new events `OnAttacked(Vector3 target)` and `OnDamaged`. Damage rules untouched.
- `HeroView.cs`: sets the animator parameters from those events and from `HeroState.IsDead`. Turns the hero toward its target on attack.
- `EnemiesController.cs`: new events `OnEnemyAttacked`, `OnEnemyDamaged` and `OnEnemyKilled` (raised just before `OnEnemyRemoved`).
- `EnemiesContainerView.cs`: forwards those events to the matching `EnemyView`.
- `EnemyView.cs`: `PlayAttack`, `PlayHit` and `PlayDeath`. Derives the animator `Speed` from per-frame displacement.
- `BeeNormal.prefab`: `Animator` assigned on `EnemyView`.

## 6. Technical flow
1. A gameplay controller changes state and raises a C# event. Controllers know nothing about animation.
2. A view reacts and sets the Animator parameter.
   - Hero attacks: `OnAttacked` → `HeroView` turns toward the target and sets the `Attack` trigger.
   - Hero is hit: `OnDamaged` → `Hit` trigger. Hero dies: `OnStateChanged` → `IsDead` = true, which wins over everything else in the Animator.
   - Restart: `IsDead` = false, so Death → Idle.
   - Enemy attacks: `OnEnemyAttacked` → `EnemiesContainerView` → `EnemyView.PlayAttack`. Hit works the same way.
   - Enemy killed: `OnEnemyKilled` → the container removes the view from its dictionary and calls `PlayDeath`. `Die` plays and the object destroys itself after `deathDuration`. The `OnEnemyRemoved` that follows finds nothing and does nothing.
   - Restart (`ClearAllEnemies`) only raises `OnEnemyRemoved`, so the bees are destroyed instantly.
3. Movement is not an event. The hero's `Speed` comes from the joystick. A bee's `Speed` comes from its per-frame displacement, because the controller moves it.

## 7. Tuning values
- `EnemyView.deathDuration` = `1.5` s: how long the bee Die animation plays before the bee is destroyed. Tunable on the prefab.
- Animator transition durations (`0.05` to `0.15` s): blend time between states. Tunable in the controllers.
- Bee Idle ↔ Move `Speed` threshold = `0.1`: below it the bee idles.

## 8. Addressables
- **Used?** No.
- **Why not:** nothing new is loaded at runtime. The controllers and clips are referenced by the existing hero and bee prefabs, which the existing configs already reference. A direct reference was the reliable choice here.

## 9. Generated assets
- **Used?** No. Only the existing animation clips are used.

## 10. Edge cases and limits
- **Stale triggers on death:** a swing or hit queued in the same frame as the hero's death would replay as a phantom Attack or Damage after Restart. Found in play-mode testing. `HeroView` now resets both triggers when the hero dies.
- **Missing Animator:** every call is guarded with a null check, so a missing Animator skips the feedback instead of breaking gameplay.
- **Fast weapons:** Any State → Attack can transition to itself, so a 0.5 s cooldown restarts the 1.2 s swing instead of being ignored.
- **Moving while hit or swinging:** movement interrupts both, so the hero never slides while frozen in a flinch.
- **Deliberately not built:** damage is not delayed to match the visual impact of the swing. That is a gameplay change and was left out.

## 11. Base project observations
- The `HeroAnimationController` had Damage, Death and an attack state (`Weak`) with no way to trigger them.
- The `BeeAnimationController` had no parameters and no transitions, so bees never left Idle. It also has a duplicate `Run` state and a stray unused transition, which were left in place.
- `EnemyView` was subscribed to the services event correctly, but nothing drove enemy animation.
- The hero root faces the opposite way to its model: the child model is rotated 180° and the movement code compensates with `LookRotation(-movement)`.
- Damage is applied at the start of the attack, before the animation visually connects.
- The hero dies in about 9 seconds in the default setup, because bees stack up faster than the hero kills them (balance).

## 12. Likely review questions
- **Why an Animator state machine rather than a C# one?** The clips and the controllers already existed. The Animator is Unity's tool for blending and interrupting clips, and the C# side only sets parameters. No new concept was added.
- **Why events instead of the view polling state?** The project pattern is controllers that raise events and views that react. Attack, hit and kill are moments, not states, so they are events. Presentation never changes gameplay rules.
- **Why `IsDead` as a bool and the others as triggers?** Death is a state that must be reversible on Restart. Attack and hit are one-shot moments.
- **Why does the bee die through a separate `OnEnemyKilled` event?** `OnEnemyRemoved` also fires on Restart, where the bee should vanish instantly. A separate event lets the view play Die only for real kills.
- **What was the bug found while testing?** Triggers queued in the frame of death stayed set and fired on revive. Fixed by resetting them on death.

## 13. 30-second explanation
The hero and the bees were moving and fighting without any visible change of state, so the player could not read what was happening. Their animation clips already existed, but only Idle and Run were wired for the hero and nothing for the bees. I connected each Animator to small events raised by the gameplay controllers. Views set the Animator parameters, and the controllers know nothing about animation. The main trade-off is that damage still lands at the start of a swing, not at its visual impact.

## 14. README entry

### Entity animation state machines — 9 min
- **Implemented:** hero and bees now play Idle, Move, Attack, Damage and Death at the right moment, driven by events from the gameplay controllers.
- **Time:** 9 min
- **Bottleneck:** entities were lacking feedback on their state.
