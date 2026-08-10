# PreToolUse guard - block commands/edits that carry a secret or target prod.
# Reads the host's tool payload on stdin; exits 2 to block.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$appqe = Resolve-Appqe
if (-not $appqe) {
  # Fail closed: a guardrail that cannot run is a guardrail that is not there.
  [Console]::Error.WriteLine("appqe guard: appqe CLI not found. The editor does not inherit your shell PATH.")
  [Console]::Error.WriteLine("  Fix: set APPQE_BIN to its absolute path, or install it somewhere standard.")
  exit 2
}
$input | & $appqe --project-root $appqeRoot guard
exit $LASTEXITCODE
