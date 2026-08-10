#!/usr/bin/env bash
# SubagentStop quality gate - fail if generated tests have blocking guardrail
# issues (hardcoded secrets/prod URLs, UI hard-waits). Scans the test dirs.
set -uo pipefail
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
APPQE="$(appqe_resolve)" || {
  echo "appqe quality gate: appqe CLI not found. The editor does not inherit your" >&2
  echo "  shell PATH. Set APPQE_BIN to its absolute path (\`which appqe\`)." >&2
  exit 2
}
# No paths: `appqe lint` then uses the project's configured api.tests_dir /
# ui.tests_dir. The old `tests`/`tests/api` guess was relative to the process cwd,
# so in a multi-root workspace it linted a sibling project - or, where the suite
# does not live under `tests/`, silently linted nothing and exited 0.
# `exec` handed the linter's own exit code straight to the host, and `appqe lint`
# exits 1 on a blocking finding. Both hosts treat 1 as advisory: Claude Code
# "treats exit code 1 as a non-blocking error and proceeds with the action, even
# though 1 is the conventional Unix failure code", and VS Code files anything
# other than 2 as a "non-blocking warning ... continue processing". So this gate
# has been reporting hardcoded secrets and prod URLs while blocking nothing.
#
# Only 2 blocks, and only stderr is read back on 2, so the findings move there.
out="$("$APPQE" --project-root "$APPQE_ROOT" lint 2>&1)"; rc=$?
[ "$rc" -eq 0 ] && exit 0
printf '%s\n' "$out" >&2
echo "appqe quality gate: the subagent may not finish with blocking guardrail \
findings above. Fix them in the files named, then let the step complete." >&2
exit 2
