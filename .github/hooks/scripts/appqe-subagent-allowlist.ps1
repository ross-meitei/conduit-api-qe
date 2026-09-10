# SubagentStart - constrain subagents to safe, read/test-only command categories.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$payload = [Console]::In.ReadToEnd()
# Containment: a foreign session's subagents follow their own project's rules.
if (Test-AppqeEventIsForeign $payload $appqeRoot) { exit 0 }

# Whose subagent is this? (#147) A project's own agents are not appqe's to
# brief or to mark. A developer's my-repo-refactor-agent was told it "may
# ONLY run" test runners, and this hook wrote the marker the PreToolUse
# guard reads to identify a worker - so the briefing was ENFORCED too.
# Before the marker, deliberately: writing it was the half with teeth.
if (-not (Test-AppqeSessionEngaged $payload $appqeRoot)) { exit 0 }

# --- delegation enforcement: mark this session as running a subagent ---------
# PreToolUse carries no agent identifier, so the guard cannot tell a worker's own
# run of a command from the coordinator's. SubagentStart/Stop do carry agent_id,
# so the pair leaves a marker and the guard reads it. Without this the worker
# would be refused the very command it was spawned to run.
function Get-AppqeSubagentMarker([string]$p) {
  $m = [regex]::Match($p, '"session_id"\s*:\s*"([^"]*)"')
  if (-not $m.Success) { return $null }
  $sid = ($m.Groups[1].Value -replace '[^A-Za-z0-9._-]', '')
  if (-not $sid) { return $null }
  $dir = Join-Path ([System.IO.Path]::GetTempPath()) 'appqe-session'
  New-Item -ItemType Directory -Force -Path $dir -ErrorAction SilentlyContinue | Out-Null
  Join-Path $dir "subagent-$sid"
}

# Append, never overwrite: subagents can nest or overlap, and a marker cleared by
# the first to finish would unguard the others.
$marker = Get-AppqeSubagentMarker $payload
if ($marker) { Add-Content -Path $marker -Value 'x' -ErrorAction SilentlyContinue }

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
