#!/usr/bin/env bash
# Rebuild the Twine games from assets/game-src into assets/game.
#
# Usage:
#   tool/build_twine.sh          rebuild the games listed in BUILDABLE
#   tool/build_twine.sh --check  report drift, write nothing
#
# WHY THERE IS AN ALLOWLIST
# -------------------------
# Five games were last fixed by editing the compiled HTML directly, so their
# .tw sources are behind what ships. Rebuilding those would quietly undo the
# navigation and wording fixes. Until a source is caught up, keep it out of
# BUILDABLE. Run --check to see where each game stands.
#
# Story format is pinned to SugarCube 2.30.0, which is what every shipped
# game was built with. Do not add a newer SugarCube to the storyformats
# directory: tweego silently prefers the highest version it finds and would
# swap the engine under all ten games.

set -euo pipefail
cd "$(dirname "$0")/.."

TWEEGO_BIN="${TWEEGO_BIN:-$HOME/.local/tweego/tweego}"
export TWEEGO_PATH="${TWEEGO_PATH:-$HOME/.local/tweego/storyformats}"

# source stem : output name
# Verified byte-for-byte reproducible from source. Safe to rebuild.
BUILDABLE=(
  "01_Salt_Sludge:salt_sludge"
  "03_DASH_Diet_Game:dash_diet_game"
  "pill_path:pill_path"
  "post_play_survey:post_play_survey"
)

# Not safe to rebuild, for two different reasons.
#
# Sources behind their shipped HTML (fixes were made in the compiled file):
#   dog_quest, 05_Bingo_Bash, quiet_landscape, vascular_village, 06_Quiet_Minute
#
# Post-processed after compiling — control_game.html carries an inlined
# ccq_bridge.js and an extra stylesheet that tweego does not emit, so a
# rebuild would strip roughly 92KB of working code out of it:
#   04_Daily_Check_In
HELD_BACK=(
  "dog_quest"
  "05_Bingo_Bash"
  "quiet_landscape"
  "vascular_village"
  "06_Quiet_Minute"
  "04_Daily_Check_In"
)

if [ ! -x "$TWEEGO_BIN" ]; then
  echo "tweego not found at $TWEEGO_BIN" >&2
  echo "Set TWEEGO_BIN, or install it from https://github.com/tmedwards/tweego/releases" >&2
  exit 1
fi

KIT="assets/game-src/shared/ccq_game_kit.tw"
mode="${1:-build}"
tmp="$(mktemp -d)"
trap 'rm -rf "$tmp"' EXIT
changed=0

for pair in "${BUILDABLE[@]}"; do
  src="assets/game-src/${pair%%:*}.tw"
  out="assets/game/${pair##*:}.html"
  # Every game also gets the shared logic kit (assets/game-src/shared).
  "$TWEEGO_BIN" -o "$tmp/out.html" "$src" "$KIT"
  # Compare with line endings ignored: Windows checkouts store CRLF, tweego
  # writes LF, and that alone should not count as a change.
  if cmp -s <(tr -d '\r' < "$tmp/out.html") <(tr -d '\r' < "$out"); then
    echo "unchanged  ${pair##*:}"
  elif [ "$mode" = "--check" ]; then
    echo "WOULD CHANGE  ${pair##*:}"
    changed=1
  else
    cp "$tmp/out.html" "$out"
    echo "rebuilt    ${pair##*:}"
    changed=1
  fi
done

for name in "${HELD_BACK[@]}"; do
  echo "held back  $name (see notes at top of this script)"
done

exit 0
