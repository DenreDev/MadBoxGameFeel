# Madbox Technical Assignment Rules

Single source of truth for how the agent works on this repository.
The human developer may edit this file at any time; re-read it before each task.

---

## 1. Context

- Madbox **Game Developer home assignment** (senior position). Source: `Home Assignment - Game Developer.pdf` at the repository root.
- Goal: add **Game Feel** to an existing RPG prototype. No meta systems, no unrelated gameplay features.
- **Hard limit: 4 hours** of total work. Keep things small, simple, clean.
- AI is encouraged, but every line of code and every decision must be **fully understood and explainable** by the human during the review meeting.
- A second interview step is a **live exercise adding a feature with AI**, so the code base must stay easy to read and extend quickly.
- Delivery: source code on a repository plus a `README.md` containing:
  1. the approach: which feedbacks were added and why
  2. the time spent on each thing implemented
  3. feedback on the base project
  4. any other comment

The human owns design and prioritization. The agent is the implementation assistant.

---

## 2. Working mode

- The human describes the **player issue and how they want it fixed**. The agent inspects the relevant code, then **implements directly**. No menu of options.
- Ask a question only when something is genuinely ambiguous or a decision would change the player-facing result. Otherwise pick the simplest reasonable default and mention it.
- The agent **may propose Game Feel features** when relevant (short list: problem, player impact, rough cost). The human decides; nothing is built until approved.
- Do not expand scope silently. Do not refactor unrelated code. If the base project has problems, **note them for the README feedback section** instead of fixing them.
- Always inspect the existing code first and follow its conventions.

---

## 3. Technical rules (architecture)

Priorities, in order: player-facing impact, correctness, **readability**, compatibility with existing architecture, reasonable extensibility, sophistication.

Target: **simple today, extensible tomorrow**. Optimize for the smallest number of *concepts*, not the smallest number of files.

Prefer:
- small components with one clear responsibility
- composition over inheritance
- explicit dependencies
- inspector-tunable values for anything a designer would tweak (duration, intensity, curves, thresholds); never expose pure implementation details
- localized changes
- defensive handling of obvious failure cases
- existing project patterns:
  - gameplay logic in plain C# controllers/services raising C# events
  - MonoBehaviour **views** react to those events (presentation never drives gameplay rules)
  - services implement `IService` and are reached through `ServicesLocator`
  - config as ScriptableObject singletons
  - async with UniTask
- feedback code lives in views/feedback components, **separate from damage and game rules**

Avoid: generic frameworks, new event buses, extra interfaces, deep inheritance, premature abstractions, new dependencies for things Unity already does.

Explainability check: if a feature can't be explained clearly in about two minutes, simplify it.

### Addressables (assessed area, treat as a first-class goal)

The team said Addressables are central to their workflow, and the human is learning them. So:

- **Use Addressables for new runtime-loaded assets** (VFX prefabs, audio, projectile/impact prefabs, optional content), even when a direct reference would work.
- **Design as if the project were large and content were served remotely (CDN)**: addresses/labels, groups, async loading, explicit lifetimes. Do not build CDN infrastructure and do not over-engineer.
- Use it a little more than strictly needed, but never at the cost of reliability or readability.

Conventions:
- Put new assets in a **dedicated group by content type** (for example `VFX`, `Audio`) with a clear address naming scheme, not in `Default Local Group`. Use the profile's build/load path variables so switching a group to remote is a configuration change, not a code change.
- Reference assets by `AssetReferenceT<T>` in a config, or by address/label for preloaded sets.
- **One small loader owns the handles.** Do not scatter `Addressables.LoadAssetAsync` calls through gameplay code. If a service is the natural owner, follow the existing `IService` `Initialize` / `Reset` pattern (verify against how services are registered before adding one).
- Load asynchronously (UniTask), never with blocking `WaitForCompletion`, unless explicitly justified.
- **Preload before gameplay needs the asset** (for example during initialization). Never load on the first hit or every frame.
- Cache the loaded asset and reuse it. Instantiate from the cached asset; avoid per-hit loads.
- Release handles in `Reset` / `OnDestroy`. No leaked handles.
- **Failure must never break gameplay**: log a warning, skip the feedback, continue.

For every Addressable usage, document in the feature note: load timing, handle owner, lifetime, release, failure behavior, cache/reuse, and why Addressables here.

---

## 4. Asset generation rules

**Art direction:** fantasy cartoon, in the spirit of **Archero** and **Pocket Champs**: bright saturated colors, chunky readable shapes, soft rounded forms, clean outlines, punchy and cute rather than realistic.

Generated assets are allowed when they:
- are small and directly serve the requested feedback
- are quick to generate and quick to integrate
- are easy to replace later

Examples: hit sparks, impact bursts, smoke puffs, shadows, small UI elements, simple placeholder sounds.

Rules:
- Never generate large content sets. No asset-generation pipeline.
- Generated assets are Addressable by default (see section 3), in the matching group.
- Keep file sizes and texture dimensions modest (power-of-two, small).
- Every generated asset is recorded in the feature note: what it is, why generated, how integrated, where its Addressable address/group is, replacement cost, and time spent.
- Generated assets do not compensate for weak technical work.

---

## 5. Feature record rules

Every gameplay or Game Feel feature produces a record, in this order:

1. **Implement and validate** the feature.
2. **Time**: record real elapsed time per phase (analysis, implementation, tuning/testing) using timestamps (for example `Get-Date`) taken at phase boundaries. Never invent or estimate after the fact. If the real value is unknown, ask the human. Time spent on asset generation counts toward the feature total.
3. **Ask the human** (short, in chat):
   - Why was this feature created?
   - What issue did it fix?
   - What was the bottleneck? (what slowed it down or was hardest)
4. **Polish the human's words** into simple, concise English. Keep their meaning; do not add reasoning they did not give.
5. **Write the feature note** under `Documentation/FeatureNotes/` from the template (`NN_FeatureName.md`).
6. **Update `README.md`**: add the feature entry (what was implemented, for how long, bottleneck) and update the time summary table.

`README.md` is expected to be written to throughout the work. Do not invent missing information; flag anything needing human confirmation.

Final README sections (kept current as features land), matching exactly what the assignment asks for: approach, features and time spent, feedback on the base project, additional comments.

README must stay **short and human-readable**: one table for time, a few plain sentences per feature, a link to the feature note for details. No bottleneck write-ups, Addressables lifecycles or generated-asset inventories in the README; those live in the feature notes. Only mention Addressables or generated assets in the README as a short line inside a feature if relevant.
