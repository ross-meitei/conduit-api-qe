#!/usr/bin/env bash
# SessionStart - inject the project's QE standards AND the discovery rules
# learned by earlier runs, so the learning loop does not depend on anyone
# remembering to go and look.
set -u
# --plain: bare text instead of the JSON envelope, for a host that adds hook
# stdout to context verbatim (Kiro). Passed by the deployed hook config.
PLAIN=""
case "${1:-}" in --plain) PLAIN=1 ;; esac
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
payload="$(cat 2>/dev/null || true)"
# Containment: a foreign session must not receive this project's QE briefing.
appqe_event_is_foreign "$payload" "$APPQE_ROOT" && exit 0
if APPQE="$(appqe_resolve)"; then
  if "$APPQE" --project-root "$APPQE_ROOT" discovery context ${PLAIN:+--plain} 2>/dev/null; then exit 0; fi
fi
CTX='appqe QE session. Source of truth for API tests is the OpenAPI spec; verify '
CTX+='endpoints/schemas/status codes against it (zero assumptions). Generated tests '
CTX+='must not hardcode secrets or production URLs, must assert status + schema, and '
CTX+='UI tests must use web-first assertions with no hard waits. Stop at approval gates.'
if [ -n "$PLAIN" ]; then
  printf '%s\n' "$CTX"
else
  printf '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"%s"}}\n' "$CTX"
fi
exit 0
