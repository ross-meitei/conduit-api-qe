#!/usr/bin/env bash
# SubagentStop quality gate - fail if generated tests have blocking guardrail
# issues (hardcoded secrets/prod URLs, UI hard-waits). Scans the test dirs.
set -uo pipefail
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
payload="$(cat 2>/dev/null || true)"
# Containment before the fail-closed CLI check - a foreign session's subagent
# is not this gate's to hold, whatever the state of this project's install.
appqe_event_is_foreign "$payload" "$APPQE_ROOT" && exit 0

# appqe's guardrail linter judges tests an appqe run generated. Running it over
# whatever a project's own subagent happened to leave behind is appqe grading
# work it did not commission, in a session that never asked (#147).
appqe_session_engaged "$payload" "$APPQE_ROOT" || exit 0

# --- delegation enforcement: mark this session as running a subagent ---------
# `PreToolUse` carries no agent identifier, so the guard cannot tell a worker's
# own run of a command from the coordinator's. SubagentStart/Stop DO carry
# `agent_id`, so the pair leaves a marker and the guard reads it. Without this
# the worker would be refused the very command it was spawned to run.
_appqe_subagent_marker() {
  _sid="$(printf '%s' "$1" | sed -n 's/.*"session_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | tr -cd 'A-Za-z0-9._-')"
  [ -n "$_sid" ] || return 0
  _dir="${TMPDIR:-/tmp}/appqe-session"
  mkdir -p "$_dir" 2>/dev/null || return 0
  printf '%s' "$_dir/subagent-$_sid"
}

# Record WHICH worker finished. `agent_type` is the subagent's name, written by
# the host every time, so this is the attestation the coordinator kept forgetting
# to send: appqe reads it back when the step's result arrives and no longer has
# to take the model's word for who ran the step. Appended, not overwritten, in
# arrival order - the engine consumes records in the same order it settles steps.
_agent_type="$(printf '%s' "$payload" | sed -n 's/.*"agent_type"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | tr -cd 'A-Za-z0-9._-')"
_sid="$(printf '%s' "$payload" | sed -n 's/.*"session_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | tr -cd 'A-Za-z0-9._-')"
if [ -n "$_agent_type" ] && [ -n "$_sid" ]; then
  _spawn_dir="${TMPDIR:-/tmp}/appqe-session"
  mkdir -p "$_spawn_dir" 2>/dev/null && \
    printf '%s %s\n' "$(date +%s)" "$_agent_type" >> "$_spawn_dir/spawned-$_sid" 2>/dev/null
fi

# One subagent finished: drop one mark. The file disappears when the last one
# ends, which is when the coordinator is on its own again.
_m="$(_appqe_subagent_marker "$payload")"
if [ -n "$_m" ] && [ -f "$_m" ]; then
  _left="$(sed '$d' "$_m" 2>/dev/null)"
  if [ -n "$_left" ]; then printf '%s\n' "$_left" > "$_m" 2>/dev/null
  else rm -f "$_m" 2>/dev/null; fi
fi

APPQE="$(appqe_resolve)" || {
  echo "appqe quality gate: appqe CLI not found. The editor does not inherit your" >&2
  echo "  shell PATH. Set APPQE_BIN to its absolute path (\`which appqe\`)." >&2
  exit 2
}
# Not an appqe project (no appqe.yml at the resolved root): nothing to govern.
# Without this, a hook deployment left behind in a non-appqe folder - or a root
# resolution surprise - linted DEFAULT dirs that do not exist, "no files matched"
# exited 1, and every subagent in the workspace was told it may not finish. A
# fetch-only worker with nothing to fix was blocked seven times running, and the
# trapped agent then started editing these very scripts to escape.
[ -f "$APPQE_ROOT/appqe.yml" ] || [ -f "$APPQE_ROOT/appqe.yaml" ] || exit 0
# No paths: `appqe lint` then uses the project's configured api.tests_dir /
# ui.tests_dir. `--allow-empty` because THIS caller asks "did the subagent leave
# blocking findings in what it wrote" - a worker that wrote nothing has nothing
# to answer for, and zero matched files must not hold it hostage. CI keeps the
# strict no-files-is-no-pass behaviour by not passing the flag.
# `exec` handed the linter's own exit code straight to the host, and `appqe lint`
# exits 1 on a blocking finding. Both hosts treat 1 as advisory: Claude Code
# "treats exit code 1 as a non-blocking error and proceeds with the action, even
# though 1 is the conventional Unix failure code", and VS Code files anything
# other than 2 as a "non-blocking warning ... continue processing". So this gate
# has been reporting hardcoded secrets and prod URLs while blocking nothing.
#
# Only 2 blocks, and only stderr is read back on 2, so the findings move there.
out="$("$APPQE" --project-root "$APPQE_ROOT" lint --allow-empty 2>&1)"; rc=$?
[ "$rc" -eq 0 ] && exit 0
printf '%s\n' "$out" >&2
echo "appqe quality gate: the subagent may not finish with blocking guardrail \
findings above. Fix them in the files named, then let the step complete." >&2
exit 2
