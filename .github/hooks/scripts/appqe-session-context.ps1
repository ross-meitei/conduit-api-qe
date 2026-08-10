# SessionStart - inject the project's QE standards AND the discovery rules learned
# by earlier runs, so the learning loop does not depend on anyone remembering to go
# and look.
#
# The `appqe discovery context` call is the point of this hook: it carries a rule
# found in an earlier run into the next session. This script omitted it while its
# .sh twin had it, so a Windows developer got the static preamble only and every
# lesson the project had already learned stayed on disk.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$appqe = Resolve-Appqe
if ($appqe) {
  $ctx = & $appqe --project-root $appqeRoot discovery context 2>$null
  if ($LASTEXITCODE -eq 0 -and $ctx) {
    $ctx
    exit 0
  }
}
$fallback = 'appqe QE session. Source of truth for API tests is the OpenAPI spec; verify ' +
  'endpoints/schemas/status codes against it (zero assumptions). Generated tests must ' +
  'not hardcode secrets or production URLs, must assert status + schema, and UI tests ' +
  'must use web-first assertions with no hard waits. Stop at approval gates.'
@{ hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $fallback } } | ConvertTo-Json -Depth 5
exit 0
