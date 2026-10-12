# Feature Cheat Sheet Template

> Copy into `Documentation/FeatureNotes/` as `NN_FeatureName.md` (e.g. `01_HitFeedback.md`).
> Keep notes and screenshots private in this Git-ignored directory. Never commit them or link to them from public documentation.
> Keep it short. It is review preparation: everything here must be explainable aloud.
> Sections 2 and 3 use the human's reasoning, polished, never invented.

# FEATURE: <name>

## 1. Summary
One or two sentences: what was implemented.

## 2. Why / issue fixed
Human's words, polished. The player-facing problem and why it mattered.

## 3. Bottleneck
Human's words, polished. What slowed this feature down or was hardest.

## 4. Time spent

| Phase | Time |
|---|---|
| Analysis | `XX min` |
| Implementation | `XX min` |
| Tuning / testing | `XX min` |
| Asset generation / integration (if any) | `XX min` |
| **Total** | `XX min` |

Real elapsed time only, from recorded timestamps. Never estimated after the fact.

## 5. Files changed
Only the important ones, with one line each.

## 6. Technical flow
Step-by-step runtime flow, understandable without reading the code.

## 7. Tuning values
Designer-facing values with default and effect.
- `ValueName` = `x`: what it changes

## 8. Addressables
- **Used?** Yes / No
- **Assets / addresses / group:**
- **Load:** when it is requested
- **Owner:** who holds the handle
- **Lifetime / cache:** how long it stays loaded, reused or reloaded
- **Release:** when and by whom
- **Failure:** what happens if loading fails
- **Remote readiness:** what would change if the group moved to a CDN
- **Why Addressables here:**

## 9. Generated assets
- **Used?** Yes / No
- For each: what it is, why generated, how integrated, Addressable address, replacement cost, time spent.

## 10. Edge cases and limits
Meaningful failure cases and how they are handled, plus what was deliberately not built.

## 11. Base project observations
Anything useful learned about the original project (candidate for the README feedback section).

## 12. Likely review questions
3 to 5 realistic questions from a senior Unity developer, each with a short answer.

## 13. 30-second explanation
What the player problem was, what was done, how it is structured, the main trade-off.

## 14. README entry
Copied into `README.md` under "Features". Include no bottleneck entry or link to the private note.

### <Feature name> — XX min
- **Implemented:** …
- **Time:** …
- **Addressables:** … (omit if none)
