# Twine logic redesign (stage 1)

Goal: give the existing Twine games the logic and flow habits that worked in the Capstone team's Zombie Chase, with **no visible UI change**. See `capstone-twine-design-report.md` for where those habits come from.

Stage 1 covers the four games that are rebuilt from source by `tool/build_twine.sh`: Salt Sludge, DASH Diet, Pill Path and the post-play survey. The other six games (Dog Quest, Bingo Bash, Quiet Landscape, Vascular Village, Quiet Minute, Daily Check-In) are not touched yet. Their sources are behind their shipped HTML, so they need to be caught up first (see the notes at the top of `tool/build_twine.sh`).

## The shared kit

`assets/game-src/shared/ccq_game_kit.tw` is one script passage that the build script compiles into every game. It adds `setup.ccq` with:

| Helper | What it is for |
|---|---|
| `call(method, ...)` | The one safe way to reach Flutter. Never throws, does nothing without a bridge |
| `once(key, fn)` / `submitOnce(key, answers, opts)` | Run-once guards that are **not** reset by Back/Forward. `submitOnce` leaves the key free if the bridge is missing |
| `lock(key, ms)` | Ignore a second tap that lands right after the first |
| `after()` / `every()` / `clearTimers()` | Timers that are cancelled on every passage change |
| `slimVars(vars)` | Telemetry snapshot with only simple values |
| `band(value, table)` | One threshold table instead of repeated if/else chains |
| `clamp(n, min, max)` | Number safety |

Unit tests: `node tool/test_game_kit.js` (13 tests).

Why guards live outside `$variables`: SugarCube restores `$variables` when the player goes Back or Forward, so a flag kept there is reset by history navigation. The Android hardware Back button goes through the same history.

## Changes per game

| Game | Problem (reproduced on the old build) | Fix |
|---|---|---|
| Salt Sludge | A double-tap on a food applied it twice (score 20 instead of 10) and skipped a day | The pick is applied once, in the tap action, behind a tap guard |
| Salt Sludge | The day number was moved forward while the result screen drew, so Back then Forward showed the wrong day ("Day 2 of 5" after one answer) | `$day` moves only when the food is picked; the result screen reads `$resultDay` |
| Salt Sludge | Going Back from the final screen and returning submitted the play twice | `setup.salt.submitFinal()` uses a once-guard keyed by play id |
| Salt Sludge | The 30/60 verdict limits were written in two places | One `VERDICTS` table; the number of days is one constant |
| DASH Diet | Round label drifted whenever the result screen redrew; resuming mid-day lost the score and wrong counts | Round is read from the foods already shown; score and wrong are saved with the day and restored |
| DASH Diet | Meal Result could be submitted twice via Back | One submit per participant per day, outside history |
| Pill Path | A finished 7-day path never advanced: no stone for today and nothing worked after day 7 | `settlePath()` starts the next path automatically |
| Pill Path | Undo did not stick: the next screen put today's pill straight back from the shared habit record | Undo also clears today's shared record |
| Pill Path | Showing the "Mark Taken" screen again logged a duplicate event | An unchanged answer logs nothing |
| Pill Path | No protection against paying the 25 points twice for one day | An `awarded` record per day, kept even after undo |
| Pill Path | Game left open past midnight kept yesterday's date; Bingo's shared record skipped the backfill | The date is re-read on every action; backfill runs first |
| Post-play survey | A missing bridge used up the one-shot guard so the answers were never sent | `submitOnce` only counts once the bridge really got the response |
| Post-play survey | An unanswered usability question was stored as the number 0, which looks like a real rating | Sent as `null` |
| All four | Each telemetry event carried every story variable (including whole data tables and typed text) | Only simple values; free text removed |

## Verified how

- Each game was compiled from source with tweego and SugarCube 2.30.0 (the same engine as before) and `tool/build_twine.sh --check` reports no drift.
- Each fix was run in a real browser against the compiled game with a recording stub for the bridge, and the main bugs were first reproduced on the old build.
- The app was rebuilt for Android with the new HTML.

## Known issue found, not changed

`ccq_bridge.js` `submitResponse()` only reads `surveyId` and `countAsCompletion` from its options. The `pointsEarned` that Salt Sludge, DASH Diet and the survey pass is **ignored**. If points are meant to be awarded from those submits, that needs a decision in the bridge or the host.

## Next stages

1. Catch the sources of the six held-back games up to their shipped HTML, then move them onto the kit.
2. Apply the delta-polling and cleanup patterns to any game that reads a sensor.
3. Add the Capstone games (Zombie Chase, While You Wait) to the build script with the same logging.
