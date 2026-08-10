# SubagentStop quality gate - fail if generated tests have blocking guardrail issues.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$appqe = Resolve-Appqe
if (-not $appqe) {
  [Console]::Error.WriteLine("appqe quality gate: appqe CLI not found. Set APPQE_BIN to its absolute path.")
  exit 2
}
# Passing the linter's own exit code through blocked nothing: `appqe lint` exits
# 1 on a blocking finding, and both hosts treat 1 as advisory and continue. Only
# 2 blocks, and only stderr is read back on 2. See the .sh twin.
#
# No paths: the project's configured tests dirs are used. The old `tests/api`
# probe was relative to the process cwd, so it could lint a sibling project.
$out = & $appqe --project-root $appqeRoot lint 2>&1
if ($LASTEXITCODE -eq 0) { exit 0 }
[Console]::Error.WriteLine(($out | Out-String))
[Console]::Error.WriteLine("appqe quality gate: the subagent may not finish with blocking guardrail findings above. Fix them in the files named, then let the step complete.")
exit 2
