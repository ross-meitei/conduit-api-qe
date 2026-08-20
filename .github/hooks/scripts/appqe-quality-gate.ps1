# SubagentStop quality gate - fail if generated tests have blocking guardrail issues.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$payload = $input | Out-String
# Containment first - a foreign event is never blocked by OUR missing CLI.
if (Test-AppqeEventIsForeign $payload $appqeRoot) { exit 0 }
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
