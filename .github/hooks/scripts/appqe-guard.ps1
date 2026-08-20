# PreToolUse guard - block commands/edits that carry a secret or target prod.
# Reads the host's tool payload on stdin; exits 2 to block.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$payload = $input | Out-String
# Containment first - a foreign event is never blocked by OUR missing CLI.
if (Test-AppqeEventIsForeign $payload $appqeRoot) { exit 0 }
$appqe = Resolve-Appqe
if (-not $appqe) {
  # Fail closed - for our events only.
  [Console]::Error.WriteLine("appqe guard: appqe CLI not found. The editor does not inherit your shell PATH.")
  [Console]::Error.WriteLine("  Fix: set APPQE_BIN to its absolute path, or install it somewhere standard.")
  exit 2
}
$payload | & $appqe --project-root $appqeRoot guard
exit $LASTEXITCODE
