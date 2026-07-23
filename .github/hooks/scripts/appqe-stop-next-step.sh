#!/usr/bin/env bash
# Stop - refuse the first attempt to end a session without offering next steps.
#
# Blocks once per session (exit 2 hands control back to the model), then allows
# the session to end. The marker keeps this from becoming an inescapable loop.
set -u
. "$(dirname "$0")/appqe-resolve.sh"
# Refresh the telemetry dashboard so every session leaves a readable record.
if APPQE="$(appqe_resolve)"; then "$APPQE" dashboard >/dev/null 2>&1 || true; fi
payload="$(cat 2>/dev/null || true)"
session="$(printf '%s' "$payload" | sed -n 's/.*"session_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
[ -n "$session" ] || session="default"
# Outside the repo: a once-per-session marker is not a project artifact, and
# under .appqe/ it surfaced in the explorer and in git status.
marker_dir="${TMPDIR:-/tmp}/appqe-session"
marker="$marker_dir/stop-prompted-$session"
if [ -f "$marker" ]; then
  exit 0                      # already prompted this session - let it end.
fi
mkdir -p "$marker_dir" 2>/dev/null || exit 0
: > "$marker" 2>/dev/null || exit 0

# Name the runs that never finished. A run abandoned at run_tests has generated
# tests nobody has seen pass, recorded nothing in the discovery log and produced
# no report - and it looks finished, because the phases it skipped are the last
# ones. Ending the session silently on top of that is how the gap ships.
stalled=""
if [ -n "${APPQE:-}" ] || APPQE="$(appqe_resolve)"; then
  stalled="$("$APPQE" list --incomplete 2>/dev/null | head -5)"
fi
if [ -n "$stalled" ]; then
  echo "appqe: do not end here - these runs never completed:" >&2
  echo "$stalled" >&2
  echo "Offer to resume one (appqe status <run_id>), or say plainly that the run is \
being abandoned and what is therefore unverified. Then ask a structured next-step \
question with concrete options, plus 'Done for now' and 'Something else'." >&2
  exit 2
fi
echo "appqe: do not end here. Ask the user a structured next-step question first \
- concrete options drawn from the current run state, plus 'Done for now' and \
'Something else'. Recompute the progress line before asking." >&2
exit 2
