#!/usr/bin/env bash
# PostToolUse - after a batch, put the run's status block back in front of the
# agent. Static instructions are read once and drift; this rides along with the
# work, which is the only channel looked at as often as the block changes.
# Advises, never blocks: always exit 0.
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

. "$(dirname "$0")/appqe-resolve.sh"
APPQE="$(appqe_resolve)" || exit 0     # no CLI, no advice - never block the turn

block="$("$APPQE" progress 2>/dev/null)" || block=""
if [ -z "$block" ]; then
  printf 'No appqe run is active yet, so there are no steps to report.\n'
  printf 'Do not print a status block, a step number, or a step list until\n'
  printf '`appqe start` or `appqe resume` returns one. The mode outlines in the\n'
  printf 'agent file are a shape, not this run step list - never render them as status.\n'
  exit 0
fi

printf '%s\n\n' "$block"
printf 'Paste the block above verbatim at the top of your reply, before anything else.\n'
printf 'Do not reformat it, shorten it, or drop steps - it is the whole run, from appqe state.\n'

# The task-list panel is agent-driven, so it drifts. This batch already carries the
# current `progress.todos`; nudge the agent to write it, but do not paste the array
# back - re-emitting "Step 1 … Step N" every turn only feeds step-number chatter into
# the reply. One line, no dump.
todos="$("$APPQE" progress --todos 2>/dev/null)" || todos=""
case "$todos" in
  ""|"[]"|"[!!]"*) ;;
  *)
    printf '\nThen refresh the task-list panel from this batch'"'"'s `progress.todos` - write the\n'
    printf 'whole array, replacing the old list, so the widget matches the run and is not a\n'
    printf 'phase behind. Do not narrate step numbers in your reply; the panel names them.\n'
    ;;
esac
exit 0
