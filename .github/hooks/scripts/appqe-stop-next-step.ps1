# Stop - refuse the first attempt to end a session without offering next steps.
. "$PSScriptRoot\appqe-resolve.ps1"
$payload = [Console]::In.ReadToEnd()
$session = "default"
if ($payload -match '"session_id"\s*:\s*"([^"]*)"') { $session = $Matches[1] }
$markerDir = ".appqe/.session"
$marker = Join-Path $markerDir "stop-prompted-$session"
if (Test-Path $marker) { exit 0 }
New-Item -ItemType Directory -Force -Path $markerDir | Out-Null
New-Item -ItemType File -Force -Path $marker | Out-Null

# Name the runs that never finished. A run abandoned at run_tests has generated
# tests nobody has seen pass, recorded nothing in the discovery log and produced
# no report - and it looks finished, because the phases it skipped are the last.
$stalled = ""
$appqe = Resolve-Appqe
if ($appqe) { $stalled = (& $appqe list --incomplete 2>$null | Select-Object -First 5) -join "`n" }
if ($stalled) {
  [Console]::Error.WriteLine("appqe: do not end here - these runs never completed:")
  [Console]::Error.WriteLine($stalled)
  [Console]::Error.WriteLine("Offer to resume one (appqe status <run_id>), or say plainly that the run is being abandoned and what is therefore unverified. Then ask a structured next-step question with concrete options, plus 'Done for now' and 'Something else'.")
  exit 2
}
[Console]::Error.WriteLine("appqe: do not end here. Ask the user a structured next-step question first - concrete options from the current run state, plus 'Done for now' and 'Something else'.")
exit 2
