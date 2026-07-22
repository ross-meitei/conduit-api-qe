#!/usr/bin/env bash
# SessionStart - inject the project's QE standards AND the discovery rules
# learned by earlier runs, so the learning loop does not depend on anyone
# remembering to go and look.
set -u
. "$(dirname "$0")/appqe-resolve.sh"
if APPQE="$(appqe_resolve)"; then
  if "$APPQE" discovery context 2>/dev/null; then exit 0; fi
fi
CTX='appqe QE session. Source of truth for API tests is the OpenAPI spec; verify '
CTX+='endpoints/schemas/status codes against it (zero assumptions). Generated tests '
CTX+='must not hardcode secrets or production URLs, must assert status + schema, and '
CTX+='UI tests must use web-first assertions with no hard waits. Stop at approval gates.'
printf '{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"%s"}}\n' "$CTX"
exit 0
