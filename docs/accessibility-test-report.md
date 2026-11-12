# Accessibility test report: CardioCare Quest games

Date: 2026-11-09. Build: branch `integration/capstone-merge` (Capstone merge + Twine logic redesign).

## Fix status (updated after the first fix round)

Each fix below was re-checked in the browser against the compiled game.

| Finding | Status | What changed | Re-check result |
|---|---|---|---|
| B1 Screen changes not announced | **Fixed** | New `assets/game/ccq_a11y.js`, injected by both Flutter hosts into every game | Real Enter key: focus moves to the new screen's first line and it is announced |
| B2 Meters have no value | **Fixed** | Same script: `.meter-bar` becomes a progress bar | Reads "Sludge 0 percent", "Nutrients 80 percent" |
| B3 Stones have no meaning | **Fixed** | `pill_path.tw`: each stone has `role="img"` and a label | "Day 2: taken with help", "Day 3: missed" |
| B4 Unlabelled inputs | **Fixed** | Blood pressure boxes (Quiet Minute, Quiet Landscape), survey word boxes, 25 Bingo goal boxes | All labelled, e.g. "Top number (systolic), in mmHg" |
| B6 / M2 / L2 Survey ranking | **Fixed** | Real `<button>`s, 44×44, 8 px apart, named per game, disabled at the ends | Real Enter key moved an item, focus followed it, "Salt Sludge is now first" announced |
| B7 Header Home / Menu | **Fixed** | Same script: role, Tab stop, Enter/Space, 48×44 touch area; the icon stays in place | In the Tab order, working |
| B8 No page language | **Fixed** (lang) | Same script sets `lang="en"` | Headings: still open |
| L1 Path cut off when narrow | **Fixed** | Stones shrink from 36 px to as small as 14 px and stay round | No stone cut off at 375, 320, 280, 240 px, or at a real 200%-zoom viewport (before: 4 hidden) |
| L3 Clouds cover text | **Fixed** | Cloud text drawn above every cloud shape; the clouds look the same | 0 of 15 sample points covered on each cloud (screenshot checked) |
| M1 Tiny targets | **Fixed** | "I'm ready" 50×14 → 86×44; "I'm done for now" 121×21 → 157×44 | Measured |
| C1 Colour-only "with help" | **Partly** | Screen readers now hear it (B3). A visible non-colour mark is a design choice, so it is not done | Open |
| L4, L5, B5, M3, M4, M5, K1–K5, D1 | Open | Medium/low items, not in this round | See below |

Correction to the original finding M1: "Mark walk complete" only appears in the browser preview, not in the app, so it was not a player-facing issue.

Files changed in this round:
- **Shared and app wiring:** `assets/game/ccq_a11y.js` (new), `lib/core/widgets/twine_game_host.dart`, `lib/core/widgets/twine_questionnaire_host.dart`, `pubspec.yaml`.
- **Sources:** `pill_path.tw` and `post_play_survey.tw` (both rebuilt from source), plus `quiet_landscape.tw`, `05_Bingo_Bash.tw`, `dog_quest.tw` and `06_Quiet_Minute.tw`.
- **Compiled games edited directly (held-back games):** `quiet_minute.html`, `quiet_landscape.html`, `bingo_bash.html`, `dog_quest.html`.

## How this was tested

Each disability was tested on its own first, then in combination. The tests ran against the 12 compiled Twine games in a phone-sized browser (375 px wide), using the real SugarCube engine:

- An audit script checked every screen of every game for accessible names, keyboard access, target size, text size, contrast, headings, live regions, language, image alt text and motion settings.
- Real keyboard input (Tab and Enter) and the browser accessibility tree checked what a screen reader gets.
- Narrow layouts and CSS width limits stood in for zoom.
- Colour-blind simulation used the Machado (2009) model: protanopia, deuteranopia and tritanopia.
- A reading-level check (Flesch-Kincaid grade) covered every screen.
- The Android app ran on the emulator for real gameplay.

Automated contrast numbers were checked by eye where text sits over pictures. Two false positives were found that way and are left out below.

**Not tested:** TalkBack and very large system font sizes on the device itself. Both need emulator system settings to be changed, which was not done. See "Still to test".

Severity: **High** = blocks the task for that person. **Medium** = makes it hard or error-prone. **Low** = friction.

---

## Persona 1: blind, using a screen reader

| # | Severity | Finding | Where | WCAG |
|---|---|---|---|---|
| B1 | High | After every tap the page changes but nothing is announced, and focus drops back to the top of the page. Checked with real Tab and Enter in Salt Sludge: focus returned to `<body>`. 10 of 12 games have no live region at all | All games | 2.4.3, 4.1.3 |
| B2 | High | The Sludge and Nutrients meters (Salt Sludge) and the Correct and Wrong meters (DASH) are coloured bars with no value for a screen reader. A blind player cannot follow their own progress | Salt Sludge, DASH | 1.1.1, 1.3.1 |
| B3 | High | Pill Path stones read as "✓", "·", "💊" or a number. "Taken", "taken with help" and "missed" are never said | Pill Path | 1.1.1 |
| B4 | High | Text boxes have no label. These include the **blood pressure entry boxes** (systolic and diastolic), the survey's "three words" boxes and the 24 Bingo goal boxes | Quiet Minute, Quiet Landscape, survey, Bingo Bash | 1.3.1, 3.3.2, 4.1.2 |
| B5 | Medium | Buttons with no name: drums and fidget library (11 pictures without alt text), 8 empty plaque cells, and Zombie Chase pictures without alt | While You Wait, Vascular Village, Zombie Chase | 1.1.1, 4.1.2 |
| B6 | Medium | The survey's ▲ and ▼ ranking buttons are announced only as symbols. Nothing says which game moves or in which direction | Post-play survey | 2.4.6, 4.1.2 |
| B7 | Medium | The Home and Menu icons the app adds to each game header are plain `<span>` elements: no role and not reachable by keyboard or switch | Every game in the app (bridge) | 2.1.1, 4.1.2 |
| B8 | Low | No `lang` on any page (all 12), and most game screens have no heading (Pill Path 7 of 8, Bingo 4 of 4, Zombie Chase 4 of 4) | All games | 3.1.1, 1.3.1 |

Works well: links built by `<<link>>` can be reached with Tab and activated with Enter.

## Persona 2: low vision, using zoom or large text

| # | Severity | Finding | Where | WCAG |
|---|---|---|---|---|
| L1 | High | The 7-stone path has a fixed width of about 310 px. Measured: at 280 px wide 1 stone is cut off, at 240 px 2 are, and at 187 px (200% zoom) 4 of 7 are hidden, with no way to scroll. Android "Display size: Largest" can bring a phone into this range | Pill Path | 1.4.10 |
| L2 | High | At about 240 px wide the survey's ▲▼ ranking buttons and the long answer labels move outside the card and are clipped | Post-play survey | 1.4.10 |
| L3 | High | The three "cloud" bubbles overlap, so the text "Bring along a furry team member to help…" is covered and unreadable. This was seen on screen and affects everyone | Dog Walking welcome | 1.4.10, 1.3.2 |
| L4 | Medium | Contrast too low (checked against the real background): Quiet Minute **error message** "That reading looks outside…" 2.44:1; "mmHg", "systolic" and "diastolic" labels 3.54:1; Pill Path stats 3.71:1 at 11 px; DASH "Good meal" verdict 2.10:1; Dog Walking "quests completed so far" 2.21:1; Daily Check-In body text 4.24:1 | Several | 1.4.3 |
| L5 | Medium | Many labels are 10–12 px: meter labels (11 px), Pill Path stats (11 px), Bingo labels (10 px) and Quiet Landscape (10 px) | Several | 1.4.4 |

Works well: pinch-zoom is not blocked, and Salt Sludge, DASH and Quiet Minute reflow at narrow widths.

## Persona 3: colour blind

| # | Severity | Finding | Where | WCAG |
|---|---|---|---|---|
| C1 | Medium | "Taken" and "Taken with help" stones show the same ✓ and differ only by colour. The colours stay distinct under protanopia, deuteranopia and tritanopia (ΔE ≥ 40), but the meaning depends on colour alone and the legend text is 11 px. "Missed" versus "With help" under tritanopia is borderline (ΔE 21) | Pill Path | 1.4.1 |

Works well: Salt Sludge and DASH meters have text labels, and every verdict is written in words ("Good choice", "Clean artery").

## Persona 4: motor (tremor, one finger, switch or keyboard)

| # | Severity | Finding | Where | WCAG |
|---|---|---|---|---|
| M1 | High | Tap targets under 24 px: **"I'm ready"** 50×14 px (the only way on from the breathing screen); **"Mark walk complete"** 180×16 px; "I'm done for now" 121×21 px | Quiet Minute, Dog Walking | 2.5.8 |
| M2 | High | Ranking uses ▲▼ buttons of 36×28 px with **4 px between them**, which a shaky finger hits wrongly. They cannot be reached by keyboard or switch either, so the two ranking questions cannot be answered | Post-play survey | 2.5.8, 2.1.1 |
| M3 | Medium | Under 44 px: Zombie Chase difficulty buttons 94×25; Bingo Reset, Edit and Home 36 px wide; Vascular Village targets 37 px high; Read aloud 40 px high; header Home and Menu 32×34; "Skip and enter a reading" 42 px | Several | 2.5.5 |
| M4 | Medium | One tap commits an answer that cannot be changed (Salt Sludge food, DASH answer). A mis-tap is final. Pill Path's "Wrong answer? Change it" is the good pattern | Salt Sludge, DASH | 3.3.4 |
| M5 | Medium | The Exercise chapter can only be played by walking (GPS distance or step count). There is no seated or wheelchair option. Zombie Chase "Impossible" is lost in about 20 seconds | Dog Walking, Zombie Chase | design |

Works well: food and answer cards are large (56–171 px), and double-taps are now ignored.

## Persona 5: cognitive, memory or reading difficulty

Works well: the reading level is low everywhere. The average is grade 0.4–3.3 per game (Salt Sludge 2.0, DASH 0.4, Quiet Minute 2.1, Vascular Village 3.3) and the hardest single screen is grade 5.2. Sentences are short, and the survey and feedback have Read aloud.

| # | Severity | Finding | Where |
|---|---|---|---|
| K1 | Medium | Pressing Android Back during a game leaves the game and opens a 4-question survey with no warning. Seen in the app | App host |
| K2 | Medium | When a blood pressure reading is rejected, the message is low contrast (2.44:1) and the boxes have no labels, so it is hard to understand what to fix | Quiet Minute (3.3.1) |
| K3 | Low | There are four different ways to leave a game (Home icon, ≡ menu, "Back to CardioCare Quest", Android Back → survey) | All games |
| K4 | Low | "Show me today's food" appears after a random 0.4–2 s wait with no sign that something is coming | Salt Sludge |
| K5 | Low | Zombie Chase never explains the rules (steps push zombies back, challenges remove zombies) before the chase starts | Zombie Chase |

## Persona 6: deaf or hard of hearing, and motion-sensitive

Works well: no game gives information by sound alone. Drums visibly grow when pressed, bubbles change colour, and lights glow. No flashing content was found.

| # | Severity | Finding | Where | WCAG |
|---|---|---|---|---|
| D1 | Low | Animations ignore the phone's "reduce motion" setting: breathing circle, stretch circle and spinner. The breathing circle is the exercise itself, so it needs a still alternative such as a counted text | Quiet Minute, Vascular Village, Daily Check-In | 2.3.3 |

---

## Combined disabilities (one person, several needs)

| Person | What breaks |
|---|---|
| **Older adult, low vision + hand tremor** (close to the study population) | At 200% zoom the survey's ▲▼ buttons are both tiny (4 px apart) and cut off, so ranking is impossible. In Pill Path, 4 stones are hidden and "with help" is shown by colour with an 11 px legend |
| **Blind + motor (switch, keyboard or voice control)** | The header Home and Menu icons cannot be focused, so leaving relies on in-game buttons. The survey ranking cannot be done. Voice control cannot name the unnamed drums, plaques or ▲▼ |
| **Low vision + cognitive** | A rejected blood pressure reading shows a faint 13 px message beside unlabelled boxes, so the person does not know what went wrong |
| **Colour blind + low vision** | Pill Path "taken" versus "with help" needs both colour and an 11 px legend at 3.7:1 contrast |
| **Motor + cognitive** | A mis-tapped answer cannot be undone, and Android Back drops the person into a survey |
| **Wheelchair user + low vision** | The Exercise chapter cannot be played at all, and "Mark walk complete" is a 16 px-high link |

---

## Suggested fix order

1. **Label every input**, starting with the blood pressure boxes (B4, K2).
2. **Announce screen changes** and move focus to the new heading after each passage (B1). This is one change in `ccq_bridge.js` and covers all games.
3. **Make the header Home and Menu real buttons** with names (B7, M3). Also in the bridge.
4. **Fix the tiny targets**: "I'm ready", "Mark walk complete", "I'm done for now" (M1).
5. **Rebuild survey ranking** with large, named, keyboard-reachable buttons such as "Move Salt Sludge up" (M2, B6, L2).
6. **Give meters and stones words** (`role="progressbar"` with values, stone labels such as "Day 2: taken with help") and add a non-colour mark for "with help" (B2, B3, C1).
7. **Let the Pill Path path wrap or shrink** at narrow widths (L1). **Fix the Dog Walking clouds** (L3).
8. **Fix the specific contrast and text-size spots** listed in L4 and L5. Add `lang="en"` and headings (B8).
9. **Respect reduce-motion**, with a still breathing guide (D1).
10. **Back button**: ask "Leave the game?" before opening the survey (K1). Add a seated exercise option (M5).

## Still to test (needs your go-ahead or a device setting)

- **TalkBack on the emulator:** turning it on is a system setting, so it was not changed here.
- **Android Font size and Display size at Largest:** also a system setting.
- **The app's own native Flutter screens** (login, dashboard, catalog) with TalkBack.
- **Vascular Village "Voyage" screen contrast:** the text sits over artwork, so it needs a visual check.
