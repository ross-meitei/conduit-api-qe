# Locate the appqe CLI. Sourced by the hook scripts.
#
# A GUI editor does not inherit the login shell's PATH, so a user-scoped install
# (uv tool, pipx, ~/.local/bin) is invisible to it even though it works in a
# terminal. Resolve explicitly rather than assume PATH.
appqe_resolve() {
  if [ -n "${APPQE_BIN:-}" ] && [ -x "${APPQE_BIN}" ]; then
    printf '%s' "${APPQE_BIN}"; return 0
  fi
  _found="$(command -v appqe 2>/dev/null)"
  if [ -n "$_found" ]; then printf '%s' "$_found"; return 0; fi
  for _c in "$HOME/.local/bin/appqe" \
            "$HOME/.local/share/uv/tools/appqe/bin/appqe" \
            "$HOME/.pyenv/shims/appqe" \
            "/opt/homebrew/bin/appqe" \
            "/usr/local/bin/appqe" \
            "$HOME/.venv/bin/appqe" \
            ".venv/bin/appqe"; do
    [ -x "$_c" ] && { printf '%s' "$_c"; return 0; }
  done
  return 1
}

# The project this hook belongs to, resolved from the hook script's own location
# rather than the process cwd.
#
# Every hook used to invoke appqe with no root, so the CLI fell back to `.`. In a
# single-folder window cwd IS the project and nothing looks wrong. In a multi-root
# workspace it is whichever folder the editor happened to hand the hook, and the
# hooks then answer for a DIFFERENT project than the one whose file was touched:
# the guard reads a sibling's security.allowed_hosts and blocks a sanctioned host
# (reported live - a run against www.xfinity.com was refused against a sibling's
# allowlist of demo.realworld.show, while the agent read the correct appqe.yml and
# could not reconcile the two), `progress` reports a sibling's run, `discovery
# context` injects a sibling's findings.
#
# The scripts are deployed to <root>/<.github|.appqe>/hooks/scripts/, so the root
# is three levels up from here - a fact about the deploy layout, not about how the
# editor was launched.
appqe_project_root() {
  _dir="${1:-$(dirname "$0")}"
  ( CDPATH= cd -- "$_dir/../../.." 2>/dev/null && pwd ) || printf '%s' "."
}

# Does this event belong to THIS project? Containment for multi-root hosts.
#
# VS Code and Claude Code both merge hooks across workspace folders, so every
# project's hooks fire for every session's events - observed live: an API
# project's quality gate, whose configured tests dir held only a .gitkeep,
# blocked a UI project's fetch-only worker seven times running. A hook must
# establish relevance before it may block; a non-relevant event exits silently,
# because the event's own project's hooks are the ones that own it.
#
# The signal is documented on both hosts: `cwd` is a common payload field
# ("working directory for the agent session"). Foreign means NEITHER path
# contains the other - if a host ever reports the multi-root parent as cwd,
# containment degrades to acting (never to mis-excluding), and an absent cwd
# behaves exactly as before this existed.
#
# Usage:  appqe_event_is_foreign "$payload" "$APPQE_ROOT" && exit 0
appqe_event_is_foreign() {
  _payload="$1"; _root="$2"
  _cwd="$(printf '%s' "$_payload" | sed -n 's/.*"cwd"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
  if [ -z "$_cwd" ]; then
    # Cursor's sessionStart/stop payloads carry workspace_roots (an array),
    # never cwd. Any root that resolves inside/above this project keeps the
    # event ours; roots that all point elsewhere make it foreign. No roots at
    # all stays the old rule: not provably foreign.
    _roots="$(printf '%s' "$_payload" | sed -n 's/.*"workspace_roots"[[:space:]]*:[[:space:]]*\[\([^]]*\)\].*/\1/p' | head -1)"
    [ -n "$_roots" ] || return 1
    _rootr="$(cd -- "$_root" 2>/dev/null && pwd -P)" || return 1
    printf '%s\n' "$_roots" | tr ',' '\n' | sed 's/^[[:space:]]*"//; s/"[[:space:]]*$//' | while IFS= read -r _wr; do
      _wrr="$(cd -- "$_wr" 2>/dev/null && pwd -P)" || continue
      case "$_wrr/" in "$_rootr"/*) echo MATCH; break ;; esac
      case "$_rootr/" in "$_wrr"/*) echo MATCH; break ;; esac
    done | grep -q MATCH && return 1
    return 0
  fi
  # pwd -P (physical): macOS aliases /tmp to /private/tmp, and a host that
  # reports the LOGICAL cwd would compare unequal against the physical root -
  # making the project's own events read as foreign, which silently disarms
  # every guardrail for that session. Found by an aliased-cwd probe, 2026-08.
  _cwd="$(cd -- "$_cwd" 2>/dev/null && pwd -P)" || return 1
  _rootr="$(cd -- "$_root" 2>/dev/null && pwd -P)" || return 1
  case "$_cwd/" in "$_rootr"/*) return 1 ;; esac
  case "$_rootr/" in "$_cwd"/*) return 1 ;; esac
  return 0
}

# The id that makes "once per session" mean once. Reported on Cursor (#138): the
# stop nudge fired after EVERY response instead of once, and measured, only two
# things produce that - a marker key that changes per turn, or a marker that does
# not persist. Both are addressed here so the two hooks that share these markers
# cannot drift apart on either.
#
# `conversation_id` FIRST, deliberately. Cursor names the conversation that, and a
# host that ALSO sends a per-REQUEST `session_id` re-keys the marker on every turn
# when session_id is read first - which turns a once-per-session handoff into
# per-turn noise. Preferring the conversation is correct on every host: where only
# `session_id` exists it is still the session.
appqe_session_id() {
  _id="$(printf '%s' "${1:-}" | sed -n 's/.*"conversation_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
  [ -n "$_id" ] || _id="$(printf '%s' "${1:-}" | sed -n 's/.*"session_id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)"
  # Never build a path out of unsanitised input: this comes straight from the hook
  # payload, so a `../` in it would write the marker outside the marker directory.
  _id="$(printf '%s' "$_id" | tr -cd 'A-Za-z0-9._-')"
  [ -n "$_id" ] || _id="default"
  printf '%s' "$_id"
}

# Where a once-per-session marker lives. `.git/appqe/` when there is a repo,
# because a marker has to survive between two turns of one session and a
# sandboxed or per-invocation $TMPDIR does not - the second cause of #138.
#
# Still outside the working tree, which is why these were moved out of `.appqe/`
# in the first place: under `.appqe/` they surfaced in the file explorer and in
# `git status`. `.git/` does neither, and run telemetry already lives there.
# No repo (a scaffolded folder, a test fixture) falls back to $TMPDIR, so the
# containment guarantees the traversal test pins are unchanged.
appqe_session_dir() {
  if [ -d "${1:-}/.git" ]; then
    printf '%s' "${1}/.git/appqe/session"
  else
    printf '%s' "${TMPDIR:-/tmp}/appqe-session"
  fi
}
