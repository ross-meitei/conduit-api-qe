#!/usr/bin/env bash
# Stop - refuse the first attempt to end a session without offering next steps.
#
# Blocks once per session (exit 2 hands control back to the model), then allows
# the session to end. The marker keeps this from becoming an inescapable loop.
#
# `--shape cursor`: Cursor's `stop` takes a `followup_message` on stdout and
# lists exit 2 as "block the action" only for events that carry a permission -
# which `stop` does not. So the nudge may simply have been discarded there.
# Rather than leave that for a live smoke, this emits Cursor's documented shape
# on exit 0 for that host, which is correct whichever way its exit codes behave.
set -u
SHAPE=""
case "${1:-}" in --shape) SHAPE="${2:-}" ;; esac
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
# Refresh the telemetry dashboard so every session leaves a readable record.
if APPQE="$(appqe_resolve)"; then
  "$APPQE" --project-root "$APPQE_ROOT" dashboard >/dev/null 2>&1 || true
fi

# One exit path for the nudge, so a host's dialect is decided in one place.
# Cursor reads `followup_message` on stdout; every other host reads stderr and
# takes exit 2 as "hand control back to the model".
_appqe_stop_exit() {
  if [ "$SHAPE" = "cursor" ]; then
    printf '{"followup_message":"appqe: one step left before this session ends - give the user their next steps, drawn from the current run state."}\n'
    exit 0
  fi
  exit 2
}

payload="$(cat 2>/dev/null || true)"
# Containment: a foreign session's stop is not this project's to prompt.
appqe_event_is_foreign "$payload" "$APPQE_ROOT" && exit 0

# A nudge needs a run to nudge about (#147). This asked a session that had
# never issued an appqe command to "give the user their next steps [...] drawn
# from the current run state" - there was no run, and the developer's own
# session was held open for one turn to say so.
appqe_session_engaged "$payload" "$APPQE_ROOT" || exit 0
# One derivation, shared with the progress-block hook - these two exchange
# ownership stamps through this directory, so they cannot disagree about the key.
# See appqe_session_id in appqe-resolve.sh for why the conversation wins (#138).
session="$(appqe_session_id "$payload")"
# Outside the repo: a once-per-session marker is not a project artifact, and
# under .appqe/ it surfaced in the explorer and in git status.
#
# Namespaced per project as well as per session. The marker used to be keyed on
# the session alone with a shared `default` fallback, so one repo's marker
# suppressed another repo's stop prompt whenever the id was missing.
# Keyed on the hook's OWN project, not the cwd: in a multi-root workspace $PWD is
# whichever folder the editor handed the hook, so two projects could share a marker.
project="$(printf '%s' "${APPQE_ROOT:-${CLAUDE_PROJECT_DIR:-${PWD}}}" | tr -cd 'A-Za-z0-9._-' | tail -c 60)"
marker_dir="$(appqe_session_dir "$APPQE_ROOT")"
marker="$marker_dir/stop-prompted-${project:-project}-$session"
mkdir -p "$marker_dir" 2>/dev/null || exit 0
# Markers are never read after their session ends, so sweep the stale ones rather
# than growing that directory for the life of the machine.
# Ownership stamps (runs-*) live longer than stop markers: a stamp swept while
# its run is still open silently releases the true owner from settling it, and
# 30 days outlives any plausible session. TMPDIR purges and reboots can still
# strip attribution - that degradation deliberately lands on the informed-once
# path, never on pressure aimed at the wrong session.
find "$marker_dir" -name 'stop-*' -type f -mtime +7 -delete 2>/dev/null || true
find "$marker_dir" -name 'runs-*' -type f -mtime +30 -delete 2>/dev/null || true

# Name the runs that never finished. A run abandoned at run_tests has generated
# tests nobody has seen pass, recorded nothing in the discovery log and produced
# no report - and it looks finished, because the phases it skipped are the last
# ones. Ending the session silently on top of that is how the gap ships.
#
# Checked BEFORE the once-per-session marker, and deliberately not covered by it.
# Reported: a session ended with steps 10-13 never run, because the generic
# "offer next steps" prompt had already spent the single block earlier on, so
# the incomplete run passed unchallenged.
#
# The two conditions are not alike. "Offer next steps" is a nicety that must
# never trap anyone, so one prompt is enough. An unfinished run is a concrete
# state with a concrete resolution, and it always has an escape - `appqe abandon`
# closes it on the record in one command - so it may keep blocking until it is
# dealt with. Bounded at 3 regardless: if something is wrong enough that the
# agent cannot resolve it, an inescapable loop helps nobody.
stalled=""
if [ -n "${APPQE:-}" ] || APPQE="$(appqe_resolve)"; then
  stalled="$("$APPQE" --project-root "$APPQE_ROOT" list --incomplete 2>/dev/null | head -5)"
fi
# A run BELONGS to the session that drove it. Ownership stamps come from the
# progress-block hook and from the engine itself (appqe start/resume stamps
# when the host exports a session id); only runs stamped for THIS session are
# this session's to settle. The rest are another session's work - the user may be
# running a UI workflow and an API workflow at once - and "settle or abandon"
# pressure aimed at a bystander is how a healthy run got closed as "stale run
# from previous session, not related to current work": the closer had never
# driven it and only knew its own housekeeping motive. A run this hook cannot
# attribute is treated as someone else's - informed once, never pressured -
# because the lesser harm points that way.
mine=""; others=""
if [ -n "$stalled" ]; then
  # Ownership only for a session the payload actually NAMED. The once-per-
  # session markers fall back to "default" harmlessly, but ownership must not:
  # the stamp writer refuses to write under an empty session id, and if every
  # anonymous session shared a "default" stamp file, each would inherit all the
  # others' runs - the cross-attribution this split exists to end. No session
  # id -> nothing is attributable -> everything lands on the informed-once path.
  owned="$marker_dir/runs-${project:-project}-$session"
  case "$session" in default) owned="" ;; esac
  while IFS= read -r line; do
    [ -n "$line" ] || continue
    hit=""
    if [ -f "$owned" ]; then
      for rid in $(printf '%s' "$line" | grep -oE 'qe-[0-9a-f]{6,}'); do
        if grep -qF "$rid" "$owned" 2>/dev/null; then hit=1; break; fi
      done
    fi
    if [ -n "$hit" ]; then
      mine="$(printf '%s\n%s' "$mine" "$line")"
    else
      others="$(printf '%s\n%s' "$others" "$line")"
    fi
  done <<APPQE_STALLED
$stalled
APPQE_STALLED
fi
others_note=""
if [ -n "$others" ]; then
  others_note="Also open, but NOT this session's to settle - another session \
drove these (or this one did, before a restart renamed it), and closing \
someone else's live run destroys their work:$(printf '%s' "$others" | tr '\n' ' '). \
Leave them: mention they exist, and let whoever drove them resume them. Only a \
human decides 'appqe abandon' for a run this session did not drive."
fi
# Nothing of this session's left unsettled: the task is done, so release the
# browser appqe's own binding started. A session left open is what blocks the
# NEXT run - reported twice, the second time as "MCP browser binding
# unavailable - locked by another Chrome session ... Requires the other session
# to close before the browser can be used", which failed the survey outright.
#
# Only when the run is SETTLED, never mid-run, and only the MCP servers appqe
# wires plus Playwright's own bundled browser; a browser on the user's profile
# is never touched. `ui.release_browser_after_run: false` turns it off, which is
# the right call when several sessions share one machine - the process scan is
# machine-wide, so a settling session can close a server another is using.
# `--auto` is what makes this the SETTING's decision rather than the hook's: it
# no-ops unless `ui.release_browser_after_run` is on. The hook stays thin and the
# policy lives in one place; a person running `explore --release --yes` by hand
# is never gated by it.
if [ -z "$mine" ] && [ -n "${APPQE:-}" ]; then
  "$APPQE" --project-root "$APPQE_ROOT" explore --release --yes --auto \
    >/dev/null 2>&1 || true
fi
if [ -n "$mine" ]; then
  tries_file="$marker_dir/stop-stalled-${project:-project}-$session"
  tries="$(cat "$tries_file" 2>/dev/null || echo 0)"
  case "$tries" in ''|*[!0-9]*) tries=0 ;; esac
  tries=$((tries + 1))
  printf '%s' "$tries" > "$tries_file" 2>/dev/null || true
  if [ "$tries" -le 3 ]; then
    echo "appqe: this session still owns runs that never completed:" >&2
    echo "$mine" >&2
    echo "Settle each one before wrapping up: resume it (appqe resume <run_id> \
...), or if it will not be finished, close it on the record with 'appqe abandon \
<run_id> --reason \"...\"' - a reason that says what stopped the run, not why \
you are tidying it - and say what is therefore unverified. **A step you cannot \
get past is not a step to narrate around**: report it FAILED through appqe with \
the reason, so the run records what stopped it instead of ending with the step \
still open. Then ask a structured next-step question with concrete options, \
plus 'Done for now' and 'Something else'. \
(Prompt $tries of 3; after that this stops blocking and the runs stay open.) \
$others_note" >&2
    _appqe_stop_exit
  fi
  echo "appqe: [!!] ending with run(s) still incomplete after 3 prompts. They \
stay open, 'appqe list --incomplete' keeps reporting them, and whatever they \
generated is unverified. $others_note" >&2
  exit 0
fi
if [ -n "$others_note" ] && [ ! -f "$marker_dir/stop-others-${project:-project}-$session" ]; then
  : > "$marker_dir/stop-others-${project:-project}-$session" 2>/dev/null || exit 0
  echo "appqe: $others_note" >&2
  _appqe_stop_exit
fi

if [ -f "$marker" ]; then
  exit 0                      # already prompted this session - let it end.
fi
: > "$marker" 2>/dev/null || exit 0
# Phrased for two readers at once. The model needs an unambiguous instruction, so
# the verbs stay imperative - but the user SEES this text, under a host label that
# already says "Blocked", and "do not end here" read to them as a reprimand for a
# session where nothing was wrong. Naming the routine ("once per session", "the
# next one ends normally") is what tells a human this is a handoff step and not a
# fault. Same control flow; only the prose changed.
echo "appqe: one step left before this session ends - give the user their next \
steps. Recompute the progress line, then ask a structured next-step question: \
concrete options drawn from the current run state, plus 'Done for now' and \
'Something else'. This runs once per session - the next stop ends normally." >&2
_appqe_stop_exit
