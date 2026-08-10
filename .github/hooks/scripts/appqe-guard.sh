#!/usr/bin/env bash
# PreToolUse guard - block commands/edits that carry a secret or target prod.
# Reads the host's tool payload on stdin; exits 2 to block (host surfaces stderr).
set -u
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
APPQE="$(appqe_resolve)" || {
  # Fail closed. A guardrail that cannot run is a guardrail that is not there,
  # and silently allowing is exactly the failure this hook exists to prevent.
  echo "appqe guard: appqe CLI not found. The editor does not inherit your shell PATH." >&2
  echo "  Fix: set APPQE_BIN to its absolute path (\`which appqe\` in a terminal)," >&2
  echo "  or install it somewhere standard. Blocking until the guard can run." >&2
  exit 2
}
exec "$APPQE" --project-root "$APPQE_ROOT" guard
