# How the Capstone team built their Twine games: logic and flow

Scope: game logic, data flow and development process only. UI and styling are left out on purpose.

Evidence base: Jesse Ceja's five commits on `Capstone` (21 Oct to 5 Nov 2026), the Twine sources and shipped HTML for Zombie Chase and While You Wait, and the Flutter side that hosts them (`zombie_chase.dart`, `twine_game_host.dart`, `health_service.dart`).

## 1. Development timeline (what they built, in what order)

| Date | Commit | What it did | What it tells us |
|---|---|---|---|
| 21 Oct | While you wait added | A pure-Twine fidget toy, plus a host widget to load it | Started with the easiest thing: no Flutter data needed |
| 1 Nov | Updated Measurements game | Audio, drums, and a change to `twine_game_host.dart` | Needed the host to be more flexible |
| 1 Nov | Zombie Chase ported | A standalone Twine prototype (source in `assets/game-src/ZombieChase/`) | Prototyped game rules in plain Twine first, with a fake "Run!" button standing in for steps |
| 2 Nov | Step counting added | Health Connect/HealthKit step reading, a polling loop, and the `enableGpsTracking` switch | Connected the real sensor only after the rules worked |
| 5 Nov | Exercise data saving | Distance persisted on the Flutter side, with load/save/reset messages | Added persistence last |

The pattern is deliberate: get the game rules working in isolation, then add the sensor, then add persistence. Each step is one small commit.

## 2. Architecture: Twine owns rules, Flutter owns the device

The key design decision is a clear split of responsibility.

- **Twine (JavaScript) owns the game state and rules:** zombie distance, zombie count, step multiplier, challenge progress, win and lose conditions.
- **Flutter (Dart) owns everything that needs the phone:** step sensor, permissions, and storage.
- **They talk only through messages:**
  - Twine to Flutter: `window.FlutterBridge.postMessage(JSON)` via one helper, `setup.sendFlutterMessage(type, data)`.
  - Flutter to Twine: `webView.runJavaScript('SugarCube.setup.<fn>(...)')`.

### Message protocol (Zombie Chase)

| Direction | Message or call | Purpose |
|---|---|---|
| Twine to Flutter | `GET_ZOMBIE_DISTANCE` | Home screen asks for saved distance (display only) |
| Twine to Flutter | `LOAD_ZOMBIE_DISTANCE` | Chase screen asks for saved distance (then starts) |
| Twine to Flutter | `START_STEP_TRACKING` / `STOP_STEP_TRACKING` | Turn the step poller on and off |
| Twine to Flutter | `SAVE_ZOMBIE_DISTANCE {distance}` | Persist after every change |
| Twine to Flutter | `RESET_ZOMBIE_DISTANCE` | Back to 20,000 |
| Flutter to Twine | `setup.getZombieDistance(n)` | Show the saved distance on Home |
| Flutter to Twine | `setup.setZombieDistance(n)` | Restore, then `beginChase()` starts the game |
| Flutter to Twine | `setup.incZombieDist(newSteps)` | Feed real steps into the game |

Worth copying: the Dart handler is a single `switch` on `data['type']` that returns `true` when handled. `TwineGameHost` calls it through an optional `onCustomBridgeMessage` hook, so a new game plugs in its own messages without editing the shared host.

## 3. Game flow (Zombie Chase)

```
Home ──GET_ZOMBIE_DISTANCE──▶ Flutter reads saved distance ──▶ Home shows it
  │ pick difficulty (sets mode, resets multiplier and challenge list)
  ▼
Chase passage loads
  └─ startChase() ──LOAD_ZOMBIE_DISTANCE──▶ Flutter ──setZombieDistance(n)──▶ beginChase()
       beginChase():
         • START_STEP_TRACKING ──▶ Flutter polls steps every 10 s
         • start a 1-second timer: zombies advance (distance -= speed)
         • show the first 3 challenges
  ▼ (every poll)
  Flutter computes newSteps = current − last  ──▶ incZombieDist(newSteps)
       • distance += newSteps × multiplier
       • every challenge's remaining steps −= steps; finished ones apply their reward
       • checkWinLoss(); save distance
  ▼
  Win  (zombieAmount ≤ 0)  or  Lose (distance ≤ 0)
       • reset distance to 20,000, save, stop tracking
       • after 3 s, go to the You Win / You Lose passage
       • "Go Again" calls reset() and returns Home
```

## 4. Logic patterns worth learning from

1. **Prototype with a fake input first.** The first version used a "Run!" button as a stand-in for steps. The shipped one keeps a `DEVplayInComputer()` function that starts the game when no bridge exists, so the game is still playable and testable in a desktop browser.
2. **Delta-based sensor reading.** Flutter stores `_lastKnownSteps` and sends only the difference. It handles a negative delta (new day or sensor correction) by resetting the baseline, and ignores zero deltas. This stops double counting.
3. **Guard against overlap.** `_isPolling` stops two health queries from running at once. `if (_stepTimer != null) return;` stops two timers from starting.
4. **Cleanup on every exit path.** `STOP_STEP_TRACKING` is sent on Return, on win, on lose and on reset, and Flutter's `dispose()` also stops the timer.
5. **Data-driven challenges.** All challenges live in one `GameData.extras` table (name, reward type, reward, required steps, original steps). Adding a challenge means adding one row. The renderer, the progress bar and the reward logic all read that table.
6. **A sliding window of challenges.** Only three are shown at once. When one completes it fades out and `createExtra` pulls in the next, with a "No New Extras!" fallback when the list runs out.
7. **Rules tied to real-world numbers.** Zombie speeds are set just above average walking, jogging and running cadence (about 102, 150 and 192 steps per minute), and the step multiplier is balanced around the 800-step reward. The reasoning is in code comments.
8. **A platform switch instead of a fork.** `enableGpsTracking: false` lets the same host serve a step game and a GPS game. Android and iOS read different health data types through one `_readTypes` getter.
9. **Persist on every change.** Saving after each distance change means a closed app resumes where it left off. It also floods the bridge (one message per second), which is a cost to weigh.
10. **Sound as game logic (While You Wait).** Each fidget is a small state machine: light state is `[element, on/off, colourIndex]`, bubble sounds avoid repeating the previous one, and drums clone the audio track so rapid taps overlap instead of cutting off.

## 5. Weaknesses to avoid (so we learn the good parts only)

| Problem | Effect |
|---|---|
| Source out of date with shipped HTML | `assets/game-src/ZombieChase/` is the old prototype. Rebuilding would replace the working game. While You Wait has no source at all |
| No research logging | Neither game calls `CCQ.telemetry`, `logQuestCompletion` or `finishQuest`, and neither awards points |
| One device-wide save key (`zombie_distance`) | Two participants on one device share progress |
| `redeem()` returns early when one element is missing | Other finished challenges in that pass are skipped |
| Hidden challenges accumulate progress | A challenge can appear already part-complete |
| The progress bar calls `Math.max` with one argument | No upper clamp on the bar |
| "Time Spent" is a countdown that starts in `StoryInit` and never stops | No end state at 00:00 |
| Build output (`problems-report.html`) committed | Noise in every diff |
| A hardcoded machine-specific JDK path in `gradle.properties` | Breaks builds on other machines (already fixed on our merge branch) |

## 6. Recommended standard for our games (what to adopt)

1. Keep the Twine-owns-rules, Flutter-owns-device split and the `onCustomBridgeMessage` hook.
2. Adopt the delta-based polling, overlap guards and cleanup on every exit path for any sensor game.
3. Keep rules in one data table per game, as in `GameData.extras`.
4. Keep a desktop-browser fallback so games are testable without a phone.
5. Add what they lack, taken from our games: `CCQ.telemetry` events, `CCQ.logQuestCompletion` with points, per-participant save keys, and the build-script allowlist so source and shipped HTML cannot drift apart.
