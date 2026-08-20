# SubagentStart - constrain subagents to safe, read/test-only command categories.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$payload = [Console]::In.ReadToEnd()
# Containment: a foreign session's subagents follow their own project's rules.
if (Test-AppqeEventIsForeign $payload $appqeRoot) { exit 0 }
# The briefing states the ENFORCED policy exactly (command_policy.py) - see
# the sh twin's comment; user-visible sentences must match across twins.
$ctx = 'Command allowlist: subagents may ONLY run, via the project package manager: ' +
  'the API/UI test runners (in collect-only or against a non-production target), ' +
  'the OpenAPI parse/validate helper, the linter/formatter, plain curl/wget/httpie ' +
  'against non-production targets, and read-only git (log/diff/branch). Targets ' +
  'must be non-production; writes to production are never allowed. BLOCKED: rm, ' +
  'del, git push/reset, package installs, curl/wget piped to a shell or an ' +
  'interpreter (python/node/ruby/perl) or with TLS ' +
  'checks disabled, any destructive DB/filesystem operation, and any command ' +
  'carrying a secret.'
@{ hookSpecificOutput = @{ hookEventName = 'SubagentStart'; additionalContext = $ctx } } | ConvertTo-Json -Depth 5
exit 0
