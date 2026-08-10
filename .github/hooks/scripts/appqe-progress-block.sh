#!/usr/bin/env bash
# PostToolUse - after a batch, put the run's status block back in front of the
# agent. Static instructions are read once and drift; this rides along with the
# work, which is the only channel looked at as often as the block changes.
# Advises, never blocks: always exit 0.
#
# The text is emitted as `hookSpecificOutput.additionalContext` JSON, which is
# the ONLY documented way to get a PostToolUse hook's words in front of the
# model. Plain stdout - what this printed for its whole life - reaches neither
# host: Claude Code states that for PostToolUse "stdout is written to the debug
# log but not shown in the transcript", and VS Code parses stdout as JSON on
# exit 0 and files anything else as a parse error. So none of this advice has
# ever been delivered on any host. Both hosts consume the identical shape.
set -u
payload="$(cat 2>/dev/null || true)"

# Batch commands produce a new block. The session-opening commands run *before*
# any run exists - that is the window where a status block gets invented from
# the mode outline ("Step 1 of 10" for a run that has 12 phases), so they are
# matched too, to say plainly that there is nothing to report yet.
case "$payload" in
  *"appqe start"*|*"appqe resume"*|*"appqe_start"*|*"appqe_resume"*) ;;
  *"appqe discovery"*|*"appqe list"*|*"appqe status"*) ;;
  *) exit 0 ;;
esac

_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
APPQE="$(appqe_resolve)" || exit 0     # no CLI, no advice - never block the turn

# Minimal JSON string escaping, in POSIX tools only: this runs before appqe is
# guaranteed importable and must not add a python3 dependency to a hook whose
# whole job is to be advisory. Backslash first or it double-escapes the rest.
json_escape() {
  sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/\r//g' -e 's/\t/\\t/g' \
    | awk 'BEGIN { ORS="" } { printf "%s\\n", $0 }'
}

emit() {
  # Claude Code caps hook output strings, additionalContext included, at 10,000
  # characters. Truncating here keeps a long run's block from being dropped
  # wholesale at the far end.
  escaped="$(printf '%s' "$1" | cut -c1-9500 | json_escape)"
  printf '{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"%s"}}\n' \
    "$escaped"
  exit 0
}

block="$("$APPQE" --project-root "$APPQE_ROOT" progress 2>/dev/null)" || block=""
if [ -z "$block" ]; then
  emit 'No appqe run is active yet, so there are no steps to report.
Do not print a status block, a step number, or a step list until
`appqe start` or `appqe resume` returns one. The mode outlines in the
agent file are a shape, not this run step list - never render them as status.'
fi

msg="$block

The block above is the current run state, for reference - the whole run, from
appqe state. What your reply pastes is decided by the batch itself: the
relay-status text at the run opening, \`progress.line\` on every batch after."

# The task-list panel is agent-driven, so it drifts. This batch already carries the
# current \`progress.todos\`; nudge the agent to write it, but do not paste the array
# back - re-emitting "Step 1 … Step N" every turn only feeds step-number chatter into
# the reply. One line, no dump.
todos="$("$APPQE" --project-root "$APPQE_ROOT" progress --todos 2>/dev/null)" || todos=""
case "$todos" in
  ""|"[]"|"[!!]"*) ;;
  *)
    msg="$msg

Then refresh the task-list panel from this batch's \`progress.todos\` - write the
whole array, replacing the old list, so the widget matches the run and is not a
phase behind. Do not narrate step numbers in your reply; the panel names them."
    ;;
esac
emit "$msg"
