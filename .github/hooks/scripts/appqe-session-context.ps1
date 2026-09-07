# SessionStart - inject the project's QE standards AND the discovery rules learned
# by earlier runs, so the learning loop does not depend on anyone remembering to go
# and look.
#
# The `appqe discovery context` call is the point of this hook: it carries a rule
# found in an earlier run into the next session. This script omitted it while its
# .sh twin had it, so a Windows developer got the static preamble only and every
# lesson the project had already learned stayed on disk.
. "$PSScriptRoot\appqe-resolve.ps1"
# The host's envelope dialect, passed by the deployed hook config - never by a
# person. `--plain` is Kiro (stdout added to context verbatim); `--shape cursor`
# is Cursor's top-level additional_context; the default is the
# hookSpecificOutput envelope Claude Code and Copilot require. A host given the
# wrong one silently receives no briefing at all.
$Shape = ''
if ($args -contains '--plain') { $Shape = 'plain' }
$i = [array]::IndexOf($args, '--shape')
if ($i -ge 0 -and $i + 1 -lt $args.Count) { $Shape = $args[$i + 1] }
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$payload = [Console]::In.ReadToEnd()
# Containment: a foreign session must not receive this project's QE briefing.
if (Test-AppqeEventIsForeign $payload $appqeRoot) { exit 0 }
$appqe = Resolve-Appqe
if ($appqe) {
  # `--plain` rather than `--shape plain` - see the .sh twin: the resolved CLI
  # can be older than these scripts and would reject an unknown flag.
  $extra = @()
  if ($Shape -eq 'plain') { $extra = @('--plain') }
  elseif ($Shape) { $extra = @('--shape', $Shape) }
  $ctx = & $appqe --project-root $appqeRoot discovery context @extra 2>$null
  if ($LASTEXITCODE -eq 0 -and $ctx) {
    $ctx
    exit 0
  }
}
$fallback = 'appqe QE session. Source of truth for API tests is the OpenAPI spec; verify ' +
  'endpoints/schemas/status codes against it (zero assumptions). Generated tests must ' +
  'not hardcode secrets or production URLs, must assert status + schema, and UI tests ' +
  'must use web-first assertions with no hard waits. Stop at approval gates.'
# The CLI could not run, so the fallback briefing is shaped here. Same three
# dialects; a host that gets the wrong one is handed nothing.
switch ($Shape) {
  'plain'  { $fallback }
  'cursor' { @{ additional_context = $fallback } | ConvertTo-Json -Depth 5 }
  default  { @{ hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $fallback } } | ConvertTo-Json -Depth 5 }
}
exit 0
