#!/usr/bin/env bash
# SessionStart - inject the project's QE standards AND the discovery rules
# learned by earlier runs, so the learning loop does not depend on anyone
# remembering to go and look.
set -u
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
if APPQE="$(appqe_resolve)"; then
  if "$APPQE" --project-root "$APPQE_ROOT" discovery context 2>/dev/null; then exit 0; fi
fi
CTX='appqe QE session. Source of truth for API tests is the OpenAPI spec; verify '
CTX+='endpoints/schemas/status codes against it (zero assumptions). Generated tests '
CTX+='must not hardcode secrets or production URLs, must assert status + schema, and '
CTX+='UI tests must use web-first assertions with no hard waits. Stop at approval gates.'
printf '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"%s"}}\n' "$CTX"
exit 0
