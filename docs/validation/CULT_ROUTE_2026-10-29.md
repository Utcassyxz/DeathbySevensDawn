# Cult route / UI verification — 2026-10-29

- Unity 6000.6.7f1: runtime and editor scripts compile successfully in the original project and the isolated validation copy.
- Original project: VerifyCultRoute PASS, 26 checks. First-day boarding and dialogue gate, second-day note gate, refusal when prerequisites are missing, closeup and manual handover, unique saved item, bilingual item, SAN 50 black / SAN 49 red, live hammer impact triggering, inventory I / Esc, full reset. Original save restored.
- Original project: VerifyInvestigation PASS, 47 assertions; deliberate evidence collection, unsupported links, deductions, persistence, loops and complete reset. Board inspection was moved away from the door interaction area.
- Final isolated logic run: VerifyCultRoute PASS, 24 checks. Additionally strengthened the TV test to require crossing the complete underside, from x -4.5 to beyond -2.8 at y -3.15. Window-dependent keyboard/style checks are exercised in the original editor, not headless mode.
- Final isolated seven-day run PASS: time boundary, sleep, death, aging, knowledge rewards, journal, ending and save persistence. Language-dependent assertions use Chinese and restore the previous setting.
- Reviewed actual QHD screenshots: door eye closeup, foil handover, inventory and low-SAN journal. Reviewed fixed-resolution giant / Earth render. New human-eye motion includes gradual exposure and a moving pupil.
- An isolated Editor Search database startup exception was identified as an editor indexing fault; game exceptions remain test failures.
- Headphones/speaker listening and other display aspect ratios were not validated in this pass. Sound files are actual imported WAVs routed to ApartmentMix, with repeated playback triggered during runtime checks.
- The full cult ending is not implemented here; this change establishes the requested entry conditions and key item.