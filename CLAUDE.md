# Madbox Technical Assignment

This is a 4-hour Game Feel home assignment (see `Home Assignment - Game Developer.pdf`).

Always read these files before modifying the project:

@Documentation/TECHNICAL_ASSIGNMENT_RULES.md
@Documentation/FEATURE_CHEAT_SHEET_TEMPLATE.md

They are the source of truth for three areas the human may update at any time:

1. **Technical** (architecture, Addressables): rules section 3
2. **Asset generation** (art direction, limits): rules section 4
3. **Feature record** (time, questions, notes, README): rules section 5

## Workflow summary

1. The human gives the player issue and how they want it fixed.
2. Inspect the existing code, then **implement directly** (no option menus). Ask only if genuinely ambiguous.
3. Keep it simple, readable, localized. Follow existing project conventions.
4. New runtime assets use **Addressables**, designed as if the project were large and served from a CDN, without over-engineering.
5. Validate, using real timestamps for phase timing. Never invent time.
6. Ask the human: why was it created, what issue did it fix, what was the bottleneck. Polish their answers.
7. Write the feature note in `Documentation/FeatureNotes/` and update `README.md` (implemented, time, bottleneck).
8. Proactively suggest relevant Game Feel features when useful. Build only what the human approves.

Do not refactor unrelated code. Note base-project issues for the README feedback section.
Everything must stay explainable by the human in a senior code review.
