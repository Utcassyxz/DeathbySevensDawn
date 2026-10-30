# 2026-10-29 — Investigation, bilingual narrative and pixel opening

## Play instructions

- Main menu and pause menu: English / 中文. The choice persists between launches.
- Bedroom: collect the observation book first; revisit from Day 2 to find the missing pages.
- Living room, Days 1–2: inspect the tank cabinet label. From Day 3, speak to the occupant; R deliberately records that day's observation.
- Day 5 records the old waterline; Day 6 records 001's testimony. The follow-up about the book changes if missing pages were collected.
- Corridor: inspect 403 near its northern door.
- Evidence board: six new records, three new deductions; total 16 evidence / 9 deductions. Tentative links and conclusions retain their existing behavior.
- Start a new game: an 11-second pixel opening plays, then the opening dream begins. Esc skips. Starting a new game retains the existing design of clearing the previous journey.

## Presentation

Deep space is drawn into a 480×270 point-filtered texture. Earth occupies a small part of the frame. The opening slowly enlarges Earth, shifts its palette, grows curling tendrils, adds amber fire and dark smoke, and fades to black. Effects are procedural prototype art. UI text still uses the installed readable font; a licensed bilingual pixel font remains an art task.

Dream, broadcast, chapter and 001 dialogue now use short physical observations and interrupted exchanges. New evidence records separate observations from hypotheses.

## Language implementation

Chinese remains StoryText.json; English is StoryTextEnglish.json. Both use the same schema and stable evidence IDs. StoryText owns the persisted language choice. Investigation invalidates its cached catalogue on language changes. Existing collected evidence and deduction text resolves by ID for display; saved records are not overwritten. Relation keys remain Chinese internally for compatibility with existing connections.

Localized: narrative resources, new follow-up, main/pause/ending, evidence board, principal journal controls, furniture and window prompts. Developer tools and some old status metadata still contain Chinese. This is a playable bilingual pass, not yet a complete translation of every debug/status label.

## Verification

VerifyExpansion checks resource parity, 16/9 catalogue counts, unique IDs, all recipe references, language refresh, stable relation keys, fixed pixel resolution, changing corruption frames and final black frame.

ExpansionRunner tests desk revisit, reachable early tank label, Day 3 dialogue without auto collection, Day 5/6 observations, all three added deductions, localized collected titles, graph persistence and the opening transition. Tests restore the main save, backup and temporary save after execution. Do not modify scripts while a runtime test is running.

Original handover work (centralized text, dream placeholders, aquarium forms and extra door board) is preserved in this batch.
