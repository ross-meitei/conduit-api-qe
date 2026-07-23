#!/usr/bin/env bash
# Stop - reconcile the todo panel before the turn ends (turn-boundary enforcement).
#
# The cadence hook (PostToolUse) keeps a one-byte-per-call counter and truncates
# it whenever the todo tool is called. So at turn end a NON-EMPTY counter means
# tool calls have happened since the last todo update: the panel is showing stale
# state and the run is about to end on top of it - a step finished but not marked,
# an item left in-progress. That dangling list is what rots into next turn's stale
# panel, the #1 reported failure. The cadence hook cannot see this: it only fires
# on a tool call, and ending the turn is not one.
#
# Reminds once, lets the model reconcile (which truncates the counter via the
# cadence hook's todo-match), then allows the turn to end. Never an inescapable
# loop: reconciling clears the trigger, and a second attempt with the counter
# still stale is allowed through, mirroring stop-next-step's once-per-episode
# guard.
set -u

payload="$(cat 2>/dev/null || true)"
session="$(printf '%s' "$payload" |
  sed -n 's/.*"session_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
[ -n "$session" ] || session="default"

# Same directory and counter the cadence hook writes - this reads its signal.
dir="${TMPDIR:-/tmp}/appqe-session"
counter="$dir/todo-cadence-$session"
reminded="$dir/todo-terminalize-$session"

# No counter, or empty (-s is "exists and non-empty"): the last action was a todo
# update, or no tools ran. The panel is current - nothing to reconcile. Clear the
# re-arm marker so a later stale turn gets a fresh reminder.
if [ ! -s "$counter" ]; then
  rm -f "$reminded" 2>/dev/null || true
  exit 0
fi
n="$(wc -c < "$counter" 2>/dev/null | tr -d ' ')"

# Stale panel at turn end. Remind once; allow the second attempt through so the
# model is never trapped if it declines to reconcile.
if [ -f "$reminded" ]; then
  rm -f "$reminded" 2>/dev/null || true
  exit 0
fi
mkdir -p "$dir" 2>/dev/null || exit 0
: > "$reminded" 2>/dev/null || exit 0

echo "appqe: before ending, reconcile the todo panel - ${n} tool call(s) have run \
since the last update, so it is showing stale state. Mark the step that just \
finished completed, and close every remaining item to a terminal state (completed, \
or completed with a blocked/skipped note). Never leave an item in-progress or \
not-started at the end of a turn - a dangling list is what carries into next turn \
as a stale panel." >&2
exit 2
