# Game routing and connection review

This is a full pass over every game's buttons, links, endings and the
navigation back to the app. The goal was to make sure every control goes
exactly where it should, nothing launches another game on its own, and every
game ends by returning cleanly to the dashboard.

All game changes were made in the Twine / SugarCube source (`assets/game-src/*.tw`)
and then recompiled to HTML with the pinned Tweego + SugarCube 2.30.0 toolchain.
Each recompiled file was compared against the shipped copy so that only the
intended lines changed. Nothing was hand-edited directly in the compiled HTML.

## How "go home" works

Every game returns to the app the same way. A button carries the
`ccq-home-trigger` class, and the shared bridge (`assets/game/ccq_bridge.js`)
listens for a tap on any such button and calls `CCQ.goHome()`, which tells the
Flutter host to close the WebView and drop back to the dashboard. The bridge
attaches both a document-level handler and a direct handler on each button so
the tap is caught reliably on the phone. This is the single, predictable exit
path used across the whole app.

## Issues found and fixed

### 1. Salt Sludge launched another game by itself
The "Final Result" passage fired a hidden call that force-opened the Daily
Check-In game the moment the player reached the ending. The player never asked
for it, and it made the game feel like it was jumping somewhere on its own. That
hidden call was removed. Salt Sludge now ends on its own result screen and waits
for the player to leave through the normal home button.

### 2. DASH ending button text
The final "BACK TO CARDIOCAREQUEST" button was missing the space in the name.
It now reads "BACK TO CARDIOCARE QUEST", matching every other game.

### 3. Daily Check-In "Back home" could stall on "Returning…"
The "Back home" button used to hop to a small in-between passage that then
called go-home. On the phone that hand-off could occasionally race the screen
redraw and leave the player stuck on a "Returning…" screen. The button now
calls go-home directly from the tap, so there is no in-between step to get stuck
on.

### 4. Two DONE buttons were missing their link target
The DONE buttons on Quiet Landscape and the post-play survey were home buttons
but had no link address, so they did not behave like proper tappable buttons.
Both now carry the standard link target used everywhere else, so they respond to
a tap the same way as the rest of the game buttons.

## Checked and already correct

- **Dog Quest** ending: the "BACK TO CARDIOCARE QUEST" button already calls
  go-home directly from the tap. No change needed.
- **Vascular Village to Quiet Landscape**: this hand-off is started by the
  player tapping "OPEN QUIET LANDSCAPE", not on its own. It is intentional and
  was kept.
- Every other game's home button already used the standard home-button pattern
  with a valid link target.
- No broken links, no dead ends, and no button that sends the player to the
  wrong game were found in any of the compiled games.

## Other related work in this pass

- The game header is now fixed to the top of the screen in every game, so it
  stays in place while the body scrolls.
- "Build your own meal" in DASH now offers a different set of ingredients each
  time it is played instead of the same fixed list.

## How every game was checked

Each game's compiled file was parsed to pull out every screen (passage) and
every place a button or link points to: `<<goto>>` jumps, `data-passage`
buttons, `<<link>>` targets, the computed `<<goto _nextStop>>` style jumps, and
the JavaScript `Engine.play(...)` jumps. Every one of those targets was matched
against the list of real screens in that game. The result:

- **Every button and link in every game points to a real screen.** There are no
  dead links and no button that drops the player onto a missing screen.
- The only "go to another game" hand-off left in the whole set is Vascular
  Village opening Quiet Landscape, and that only happens when the player taps
  "OPEN QUIET LANDSCAPE". Nothing launches on its own.

## Two leftover screens worth knowing about (not player-facing)

These are internal screens that no button reaches, so a player can never land on
them. They cause no broken navigation, but they are worth cleaning up later:

- **Pill Path — "Next Path" screen.** After a player fills a seven-day path they
  reach the Celebration screen, which only offers the home button. The old
  "Next Path" screen that would have started a fresh seven-day path is never
  linked to, so it sits unused. If a "start the next path" step is wanted, this
  is where it would connect; if not, the screen can be removed.
- **Vascular Village — "No Reading" screen.** This looks like a fallback for a
  blood-pressure capture that returns nothing, but no path currently routes to
  it. It sits unused.

Neither of these was changed in this pass because removing or wiring them is a
design decision, not a bug fix. They are listed here so the decision is a
conscious one.

## Every screen (passage) count per game

Salt Sludge (9), DASH Diet (12), Daily Check-In (13), Quiet Landscape (9),
Quiet Minute (8), Dog Quest (6), Vascular Village (16), Pill Path (10), Bingo
Bash (6), and the post-play survey (14). Game Feedback and the custom-game
template are not Twine games and were checked separately.

Each game also keeps a small unused "Central Hub" exit screen from an earlier
design. Nothing routes through it anymore now that every home button calls
go-home directly, so it is harmless leftover and was left in place.
