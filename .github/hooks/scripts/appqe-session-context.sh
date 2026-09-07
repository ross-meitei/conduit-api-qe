#!/usr/bin/env bash
# SessionStart - inject the project's QE standards AND the discovery rules
# learned by earlier runs, so the learning loop does not depend on anyone
# remembering to go and look.
set -u
# The host's envelope dialect, passed by the deployed hook config - never by a
# person. `--plain` is Kiro (stdout added to context verbatim); `--shape cursor`
# is Cursor's top-level additional_context; the default is the
# hookSpecificOutput envelope Claude Code and Copilot require. A host given the
# wrong one silently receives no briefing at all.
SHAPE=""
case "${1:-}" in
  --plain) SHAPE="plain" ;;
  --shape) SHAPE="${2:-}" ;;
esac
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
payload="$(cat 2>/dev/null || true)"
# Containment: a foreign session must not receive this project's QE briefing.
appqe_event_is_foreign "$payload" "$APPQE_ROOT" && exit 0
if APPQE="$(appqe_resolve)"; then
  # `--plain` rather than `--shape plain`: the CLI on this machine may be older
  # than these scripts, and it would reject a flag it has never heard of - the
  # error is swallowed below and the project-aware briefing lost for a static
  # one. The old flag still means what it meant.
  FLAG=""
  case "$SHAPE" in
    plain) FLAG="--plain" ;;
    ?*)    FLAG="--shape $SHAPE" ;;
  esac
  # shellcheck disable=SC2086 - FLAG is one or two bare words by construction.
  if "$APPQE" --project-root "$APPQE_ROOT" discovery context $FLAG 2>/dev/null; then exit 0; fi
fi
CTX='appqe QE session. Source of truth for API tests is the OpenAPI spec; verify '
CTX+='endpoints/schemas/status codes against it (zero assumptions). Generated tests '
CTX+='must not hardcode secrets or production URLs, must assert status + schema, and '
CTX+='UI tests must use web-first assertions with no hard waits. Stop at approval gates.'
# The CLI could not run, so the fallback briefing is shaped here. Same three
# dialects; a host that gets the wrong one is handed nothing.
case "$SHAPE" in
  plain)  printf '%s\n' "$CTX" ;;
  cursor) printf '{"additional_context":"%s"}\n' "$CTX" ;;
  *)      printf '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"%s"}}\n' "$CTX" ;;
esac
exit 0
