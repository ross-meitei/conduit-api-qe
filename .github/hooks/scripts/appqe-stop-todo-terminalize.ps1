# Stop - reconcile the todo panel before the turn ends (turn-boundary enforcement).
#
# The cadence hook keeps a one-byte-per-call counter and truncates it whenever the
# todo tool is called. A NON-EMPTY counter at turn end means tool calls have run
# since the last todo update: the panel is stale and the run is about to end on
# top of it. The cadence hook cannot see this - it only fires on a tool call, and
# ending the turn is not one. Reminds once, then allows the turn to end; never an
# inescapable loop, because reconciling the list clears the trigger.
$payload = [Console]::In.ReadToEnd()
$session = "default"
if ($payload -match '"session_id"\s*:\s*"([^"]*)"') { $session = $Matches[1] }

$dir = Join-Path ([System.IO.Path]::GetTempPath()) "appqe-session"
$counter = Join-Path $dir "todo-cadence-$session"
$reminded = Join-Path $dir "todo-terminalize-$session"

# No counter, or empty: the panel is current. Clear the re-arm marker.
$n = 0
if (Test-Path $counter) { $n = (Get-Item $counter).Length }
if ($n -eq 0) {
  Remove-Item $reminded -ErrorAction SilentlyContinue
  exit 0
}

# Stale panel. Remind once; allow the second attempt through.
if (Test-Path $reminded) {
  Remove-Item $reminded -ErrorAction SilentlyContinue
  exit 0
}
New-Item -ItemType Directory -Force -Path $dir | Out-Null
New-Item -ItemType File -Force -Path $reminded | Out-Null

[Console]::Error.WriteLine("appqe: before ending, reconcile the todo panel - $n tool call(s) have run since the last update, so it is showing stale state. Mark the step that just finished completed, and close every remaining item to a terminal state (completed, or completed with a blocked/skipped note). Never leave an item in-progress or not-started at the end of a turn - a dangling list is what carries into next turn as a stale panel.")
exit 2
