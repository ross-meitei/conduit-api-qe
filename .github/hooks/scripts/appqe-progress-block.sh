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
# --plain: emit the advice as bare text on exit 0, for a host that adds hook
# stdout to the agent's context verbatim (Kiro) - there the JSON envelope
# would arrive as a blob wrapped around the words. Passed by the deployed
# hook config, never guessed here.
SHAPE=""
case "${1:-}" in
  --plain) SHAPE="plain" ;;
  --shape) SHAPE="${2:-}" ;;
esac
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
payload="$(cat 2>/dev/null || true)"
# Containment: a foreign session's batches are another project's runs.
appqe_event_is_foreign "$payload" "$APPQE_ROOT" && exit 0

# Batch commands produce a new block. The session-opening commands run *before*
# any run exists - that is the window where a status block gets invented from
# the mode outline ("Step 1 of 10" for a run that has 12 phases), so they are
# matched too, to say plainly that there is nothing to report yet.
case "$payload" in
  *"appqe start"*|*"appqe resume"*|*"appqe_start"*|*"appqe_resume"*) ;;
  *"qe_start"*|*"qe_resume"*) ;;
  *"appqe discovery"*|*"appqe list"*|*"appqe status"*) ;;
  *) exit 0 ;;
esac

# Ownership stamp: a run BELONGS to the session that drove it. On start/resume,
# record the run ids this payload carries against this session, so the stop
# hook can tell "settle your run" from "leave someone else's alone". (The
# engine writes the same marker itself when the host exports a session id -
# this hook remains for hosts that export nothing; the two dedupe.) Reported
# live: a session doing UI work in one project was stop-prompted about a
# sibling API project's incomplete run and closed it as "stale, not related" -
# a healthy run, driven by another session, destroyed by housekeeping pressure.
#
# The start/resume decision reads the COMMAND (`tool_input.command`) or the
# tool name - never the whole payload, because `appqe list`'s own OUTPUT prints
# "appqe resume <id>" hint lines for every open run, and matching the response
# would stamp all of them as this session's, defeating the attribution.
# FIRST occurrence, not last: sed's greedy `.*` would take the final "command"
# in the payload, and a start/resume RESPONSE carries the batch's own action
# commands - the last one is "npx playwright test ...", the stamp decision
# fails, and the run goes unattributed. The .ps1 twin's regex already takes the
# first match; this keeps the twins one hook. The value pattern tolerates
# JSON-escaped quotes (\"), or `cd \"My Project\" && appqe resume ...` would
# truncate at the first backslash and never stamp - reproduced live.
cmd="$(printf '%s' "$payload" | grep -oE '"command"[[:space:]]*:[[:space:]]*"(\\.|[^"\\])*"' | head -1)"
tool="$(printf '%s' "$payload" | grep -oE '"tool_name"[[:space:]]*:[[:space:]]*"(\\.|[^"\\])*"' | head -1)"
case "$cmd|$tool" in
  *"appqe start"*|*"appqe resume"*|*"appqe_start"*|*"appqe_resume"*|*"qe_start"*|*"qe_resume"*)
    # Shared with the stop hook, which READS the stamps written below - the two
    # must agree on the key or attribution silently breaks (appqe-resolve.sh).
    session="$(appqe_session_id "$payload")"
    # `default` is not attributable: every anonymous session would inherit every
    # other one's runs. Preserved from the original explicitly, because the
    # shared helper returns `default` rather than an empty string.
    case "$session" in default) session="" ;; esac
    project="$(printf '%s' "${APPQE_ROOT:-$PWD}" | tr -cd 'A-Za-z0-9._-' | tail -c 60)"
    if [ -n "$session" ]; then
      marker_dir="$(appqe_session_dir "$APPQE_ROOT")"
      # Harvest ids ONLY from the command that ran and from `"run_id"` keys.
      # Never from free response text: `appqe start`'s own output can NAME other
      # sessions' runs ("Awaiting runs: ...", the duplicate-run refusal list),
      # and stamping those as this session's recreates the settle-pressure that
      # closed a bystander's healthy run. The run_id key appears only in the
      # batch JSON of a run this command actually opened or advanced; refusals
      # and preambles print bare ids without it.
      mkdir -p "$marker_dir" 2>/dev/null && \
        { printf '%s' "$cmd" | grep -oE 'qe-[0-9a-f]{6,}'
          printf '%s' "$payload" | grep -oE 'run_id[\\"]*[[:space:]]*:[[:space:]]*[\\"]*qe-[0-9a-f]{6,}' \
            | grep -oE 'qe-[0-9a-f]{6,}'; } | sort -u \
          >> "$marker_dir/runs-${project:-project}-$session" 2>/dev/null || true
    fi
    ;;
esac

APPQE="$(appqe_resolve)" || exit 0     # no CLI, no advice - never block the turn

# Minimal JSON string escaping, in POSIX tools only: this runs before appqe is
# guaranteed importable and must not add a python3 dependency to a hook whose
# whole job is to be advisory. Backslash first or it double-escapes the rest.
json_escape() {
  sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' -e 's/\r//g' -e 's/\t/\\t/g' \
    | awk 'BEGIN { ORS="" } { printf "%s\\n", $0 }'
}

emit() {
  if [ "$SHAPE" = "plain" ]; then
    # head -c caps the TOTAL, matching the intent; cut -c would cap each line.
    printf '%s\n' "$1" | head -c 9500
    exit 0
  fi
  # Claude Code caps hook output strings, additionalContext included, at 10,000
  # characters. Truncating here keeps a long run's block from being dropped
  # wholesale at the far end.
  escaped="$(printf '%s' "$1" | cut -c1-9500 | json_escape)"
  # Cursor's postToolUse reads a top-level `additional_context`; Claude Code and
  # Copilot read the hookSpecificOutput envelope. Wrong shape = ignored,
  # silently, which is what Cursor got until 0.14.
  if [ "$SHAPE" = "cursor" ]; then
    printf '{"additional_context":"%s"}\n' "$escaped"
    exit 0
  fi
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
# One call, not two: the target line is empty exactly when there are no todos to
# write, so its emptiness answers "is there a panel to refresh" as well. This hook
# runs on every tool batch, so a spare interpreter start is a spare start per batch.
target="$("$APPQE" --project-root "$APPQE_ROOT" progress --todos --panel-target 2>/dev/null)" || target=""
case "$target" in
  ""|"[!!]"*) ;;
  *)
    # The nudge names a number the agent can CHECK against the widget in front of
    # it. "Write the whole array" is advice it cannot verify it followed - a panel
    # measured at 4-of-13 while appqe handed over 13 correct items is the failure
    # this closes: the payload was right and nobody could tell the panel was wrong.
    msg="$msg

Then refresh the task-list panel by running this batch's
\`progress.todo_call\` - the complete call, whole run already written into it.
Not \`actions\`: those are the steps to execute, and a panel built from them is
one batch wide. Do not narrate step numbers in your reply; the panel names them.
**After running it the panel must read exactly: $target.** Compare it; if it does
not match, you built the items yourself - run \`progress.todo_call\` now."
    ;;
esac
emit "$msg"
