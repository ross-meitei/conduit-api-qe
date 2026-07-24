# SubagentStop quality gate - fail if generated tests have blocking guardrail issues.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqe = Resolve-Appqe
if (-not $appqe) {
  [Console]::Error.WriteLine("appqe quality gate: appqe CLI not found. Set APPQE_BIN to its absolute path.")
  exit 2
}
$dirs = if (Test-Path "tests/api") { @("tests/api","tests/e2e","tests") } else { @("tests") }
& $appqe lint @dirs
exit $LASTEXITCODE
