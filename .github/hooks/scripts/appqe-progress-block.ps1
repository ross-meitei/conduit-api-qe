# PostToolUse - after a batch, put the run's status block back in front of the
# agent. Static instructions are read once and drift; this rides along with the
# work, which is the only channel looked at as often as the block changes.
# Advises, never blocks: always exit 0.
$ErrorActionPreference = 'SilentlyContinue'
$payload = [Console]::In.ReadToEnd()

if ($payload -notmatch 'appqe[ _](start|resume|discovery|list|status)') { exit 0 }

. (Join-Path $PSScriptRoot 'appqe-resolve.ps1')
$appqe = Resolve-Appqe
if (-not $appqe) { exit 0 }            # no CLI, no advice - never block the turn

$block = & $appqe progress 2>$null
if (-not $block) {
  "No appqe run is active yet, so there are no steps to report."
  "Do not print a status block, a step number, or a step list until"
  "``appqe start`` or ``appqe resume`` returns one. The mode outlines in the"
  "agent file are a shape, not this run's step list - never render them as status."
  exit 0
}

$block -join "`n"
""
"Paste the block above verbatim at the top of your reply, before anything else."
"Do not reformat it, shorten it, or drop steps - it is the whole run, from appqe state."

# Nudge the panel refresh, but do not paste the array back - re-emitting "Step 1 ..
# Step N" every turn only feeds step-number chatter. One line, no dump.
$todos = & $appqe progress --todos 2>$null
if ($todos -and $todos -ne '[]' -and $todos -notmatch '^\[!!\]') {
  ""
  "Then refresh the task-list panel from this batch's ``progress.todos`` - write the"
  "whole array, replacing the old list, so the widget matches the run and is not a"
  "phase behind. Do not narrate step numbers in your reply; the panel names them."
}
exit 0
