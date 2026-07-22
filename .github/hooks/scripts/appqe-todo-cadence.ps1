# PostToolUse - keep the todo list a running view instead of an opening gesture.
#
# This hook NEVER blocks. Exit 2 is a blocking error in the documented contract
# and turned a reminder into a broken run; exit 0 with JSON on stdout is the
# supported way to speak. hookSpecificOutput.additionalContext is appended to the
# tool result the model reads on the same turn.
$threshold = 4
if ($env:APPQE_TODO_CADENCE) { $threshold = [int]$env:APPQE_TODO_CADENCE }

$payload = [Console]::In.ReadToEnd()
$tool = ""
if ($payload -match '"tool_name"\s*:\s*"([^"]*)"') { $tool = $Matches[1] }
$session = "default"
if ($payload -match '"session_id"\s*:\s*"([^"]*)"') { $session = $Matches[1] }

$dir = ".appqe/.session"
$counter = Join-Path $dir "todo-cadence-$session"
New-Item -ItemType Directory -Force -Path $dir | Out-Null

# The todo tool itself resets the clock. Matched by substring so this stays
# host-neutral: every host's name for it contains "todo" in some casing, and
# hooks deploy verbatim, so naming one host's tool here would be a leak.
if ($tool -match '(?i)todo') {
  Set-Content -Path $counter -Value "" -NoNewline
  exit 0
}

# One byte per call - no arithmetic state to corrupt if a run is interrupted.
Add-Content -Path $counter -Value "x" -NoNewline
$n = (Get-Item $counter).Length
if ($n -lt $threshold) { exit 0 }

Set-Content -Path $counter -Value "" -NoNewline
$msg = "appqe: the todo list has not been updated in $n tool calls. Update it " +
  "before your next action - mark what just finished as completed and the next " +
  "step as in-progress in the same call, and enrich the finished item title " +
  "with what you learned. The panel renders where the most recent update " +
  "happened, so a list left untouched scrolls out of view and shows the user " +
  "stale state."
$out = @{ hookSpecificOutput = @{ hookEventName = "PostToolUse"; additionalContext = $msg } }
$out | ConvertTo-Json -Compress -Depth 5
exit 0
