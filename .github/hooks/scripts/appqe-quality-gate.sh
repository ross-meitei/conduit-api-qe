#!/usr/bin/env bash
# SubagentStop quality gate - fail if generated tests have blocking guardrail
# issues (hardcoded secrets/prod URLs, UI hard-waits). Scans the test dirs.
set -uo pipefail
. "$(dirname "$0")/appqe-resolve.sh"
APPQE="$(appqe_resolve)" || {
  echo "appqe quality gate: appqe CLI not found. The editor does not inherit your" >&2
  echo "  shell PATH. Set APPQE_BIN to its absolute path (\`which appqe\`)." >&2
  exit 2
}
DIRS="tests"
[ -d tests/api ] && DIRS="tests/api tests/e2e tests"
exec "$APPQE" lint $DIRS
