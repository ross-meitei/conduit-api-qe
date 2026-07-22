#!/usr/bin/env bash
# PostToolUse - keep the todo list a running view instead of an opening gesture.
#
# The instruction lives in the agent and in every prompt and is still honoured
# once per session: the model seeds the list, the panel renders at that point in
# the transcript, and the run scrolls away from it, leaving the user watching
# stale state. Counting tool calls is the only reliable signal available here.
#
# This hook NEVER blocks. Per the documented contract, exit 2 is a blocking error
# that stops the tool call, which turned a reminder into a broken run. Exit 0 with
# JSON on stdout is the supported way to speak: hookSpecificOutput.additionalContext
# is appended to the tool result the model reads on the same turn, so the nudge
# lands as guidance rather than as a failure.
set -u

THRESHOLD="${APPQE_TODO_CADENCE:-4}"

payload="$(cat 2>/dev/null || true)"
tool="$(printf '%s' "$payload" |
  sed -n 's/.*"tool_name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
session="$(printf '%s' "$payload" |
  sed -n 's/.*"session_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
[ -n "$session" ] || session="default"

dir=".appqe/.session"
counter="$dir/todo-cadence-$session"
mkdir -p "$dir" 2>/dev/null || exit 0

# The todo tool itself resets the clock. Matched by substring so this stays
# host-neutral: every host's name for it contains "todo" in some casing, and
# hooks deploy verbatim, so naming one host's tool here would be a leak.
case "$tool" in
  *todo*|*Todo*|*TODO*)
    : > "$counter" 2>/dev/null || true
    exit 0
    ;;
esac

# One byte per call - no arithmetic state to corrupt if a run is interrupted.
printf 'x' >> "$counter" 2>/dev/null || exit 0
n="$(wc -c < "$counter" 2>/dev/null | tr -d ' ')"
[ -n "$n" ] || exit 0
[ "$n" -ge "$THRESHOLD" ] 2>/dev/null || exit 0

: > "$counter" 2>/dev/null || true
printf '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"appqe: the todo list has not been updated in %s tool calls. Update it before your next action - mark what just finished as completed and the next step as in-progress in the same call, and enrich the finished item title with what you learned. The panel renders where the most recent update happened, so a list left untouched scrolls out of view and shows the user stale state."}}\n' "$n"
exit 0
