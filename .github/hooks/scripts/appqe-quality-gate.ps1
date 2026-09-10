# SubagentStop quality gate - fail if generated tests have blocking guardrail issues.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$payload = $input | Out-String
# Containment first - a foreign event is never blocked by OUR missing CLI.
if (Test-AppqeEventIsForeign $payload $appqeRoot) { exit 0 }

# appqe's guardrail linter judges tests an appqe run generated. Running it
# over whatever a project's own subagent left behind is appqe grading work
# it did not commission, in a session that never asked (#147).
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

# Record WHICH worker finished: agent_type is the subagent's name, written by the
# host every time, so the engine reads it back instead of taking the model's word.
$at = [regex]::Match($payload, '"agent_type"\s*:\s*"([^"]*)"')
$sm = [regex]::Match($payload, '"session_id"\s*:\s*"([^"]*)"')
if ($at.Success -and $sm.Success) {
  $agentType = ($at.Groups[1].Value -replace '[^A-Za-z0-9._-]', '')
  $sid = ($sm.Groups[1].Value -replace '[^A-Za-z0-9._-]', '')
  if ($agentType -and $sid) {
    $spawnDir = Join-Path ([System.IO.Path]::GetTempPath()) 'appqe-session'
    New-Item -ItemType Directory -Force -Path $spawnDir -ErrorAction SilentlyContinue | Out-Null
    $epoch = [int][double]::Parse((Get-Date -UFormat %s))
    Add-Content -Path (Join-Path $spawnDir "spawned-$sid") -Value "$epoch $agentType" -ErrorAction SilentlyContinue
  }
}

# One subagent finished: drop one mark. The file disappears when the last one
# ends, which is when the coordinator is on its own again.
$marker = Get-AppqeSubagentMarker $payload
if ($marker -and (Test-Path $marker)) {
  $lines = @(Get-Content $marker -ErrorAction SilentlyContinue)
  if ($lines.Count -gt 1) { Set-Content -Path $marker -Value $lines[0..($lines.Count - 2)] -ErrorAction SilentlyContinue }
  else { Remove-Item $marker -Force -ErrorAction SilentlyContinue }
}

$appqe = Resolve-Appqe
if (-not $appqe) {
  # Same explanation and path forward the sh twin gives - Windows users get
  # the full story, not an abbreviated refusal.
  [Console]::Error.WriteLine("appqe quality gate: appqe CLI not found. The editor may not")
  [Console]::Error.WriteLine("  inherit your shell PATH. Set APPQE_BIN to its absolute path")
  [Console]::Error.WriteLine("  ((Get-Command appqe).Source in a terminal that can run it).")
  exit 2
}
# Not an appqe project: nothing to govern - see the .sh twin for the trap this
# prevents.
if (-not ((Test-Path (Join-Path $appqeRoot "appqe.yml")) -or
          (Test-Path (Join-Path $appqeRoot "appqe.yaml")))) { exit 0 }
# Passing the linter's own exit code through blocked nothing: `appqe lint` exits
# 1 on a blocking finding, and both hosts treat 1 as advisory and continue. Only
# 2 blocks, and only stderr is read back on 2. See the .sh twin.
#
# No paths: the project's configured tests dirs are used. The old `tests/api`
# probe was relative to the process cwd, so it could lint a sibling project.
$out = & $appqe --project-root $appqeRoot lint --allow-empty 2>&1
if ($LASTEXITCODE -eq 0) { exit 0 }
[Console]::Error.WriteLine(($out | Out-String))
[Console]::Error.WriteLine("appqe quality gate: the subagent may not finish with blocking guardrail findings above. Fix them in the files named, then let the step complete.")
exit 2
