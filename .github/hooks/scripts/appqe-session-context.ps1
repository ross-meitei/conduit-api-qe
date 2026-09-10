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

# One emitter, three dialects. Duplicating the envelope is how a host ends up
# handed the wrong shape and silently receives nothing at all.
function Write-AppqeContext {
  param([string]$Text)
  switch ($Shape) {
    'plain'  { $Text }
    'cursor' { @{ additional_context = $Text } | ConvertTo-Json -Depth 5 }
    default  { @{ hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $Text } } | ConvertTo-Json -Depth 5 }
  }
}

# A session appqe is not driving gets ONE inert line, not the QE briefing
# (#147). A developer opening a session to fix a bug was told where the API
# tests live and instructed to record findings with `appqe discovery add` -
# instructions for work they had not asked to do.
#
# SessionStart is the one event that cannot consult the latch and get a useful
# answer: it fires before the session has done anything. That is why this is a
# line rather than silence - discoverability is why the hook exists, and the
# full briefing arrives with the run itself.
if (-not (Test-AppqeSessionEngaged $payload $appqeRoot)) {
  Write-AppqeContext ('appqe is available in this project. Ask for a QE workflow in your own words ' +
    '(for example: generate api tests from the openapi spec) or run appqe --help. It stays out of ' +
    'the way otherwise - appqe conventions and its command policy apply to appqe runs, not to this session.')
  exit 0
}

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
# The CLI could not run, so the fallback briefing is shaped here.
Write-AppqeContext $fallback
exit 0
