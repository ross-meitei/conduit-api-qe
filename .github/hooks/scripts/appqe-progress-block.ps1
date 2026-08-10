# PostToolUse - after a batch, put the run's status block back in front of the
# agent. Static instructions are read once and drift; this rides along with the
# work, which is the only channel looked at as often as the block changes.
# Advises, never blocks: always exit 0.
#
# Emitted as `hookSpecificOutput.additionalContext` JSON, the only documented way
# to get a PostToolUse hook's words in front of the model. Plain stdout - what
# this printed for its whole life - reaches neither host. See the .sh twin.
$ErrorActionPreference = 'SilentlyContinue'
$payload = [Console]::In.ReadToEnd()

if ($payload -notmatch 'appqe[ _](start|resume|discovery|list|status)') { exit 0 }

. (Join-Path $PSScriptRoot 'appqe-resolve.ps1')
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$appqe = Resolve-Appqe
if (-not $appqe) { exit 0 }            # no CLI, no advice - never block the turn

function Emit-Context([string] $text) {
  # Claude Code caps hook output strings, additionalContext included, at 10,000
  # characters. Truncate rather than have a long run's block dropped wholesale.
  if ($text.Length -gt 9500) { $text = $text.Substring(0, 9500) }
  # ConvertTo-Json handles the escaping, and -Compress keeps it one line.
  $doc = [ordered]@{
    hookSpecificOutput = [ordered]@{
      hookEventName    = 'PostToolUse'
      additionalContext = $text
    }
  }
  ConvertTo-Json $doc -Depth 5 -Compress
  exit 0
}

$block = & $appqe --project-root $appqeRoot progress 2>$null
if (-not $block) {
  Emit-Context @"
No appqe run is active yet, so there are no steps to report.
Do not print a status block, a step number, or a step list until
``appqe start`` or ``appqe resume`` returns one. The mode outlines in the
agent file are a shape, not this run's step list - never render them as status.
"@
}

$msg = ($block -join "`n") + @"


The block above is the current run state, for reference - the whole run, from
appqe state. What your reply pastes is decided by the batch itself: the
relay-status text at the run opening, ``progress.line`` on every batch after.
"@

# Nudge the panel refresh, but do not paste the array back - re-emitting "Step 1 ..
# Step N" every turn only feeds step-number chatter. One line, no dump.
$todos = & $appqe --project-root $appqeRoot progress --todos 2>$null
if ($todos -and $todos -ne '[]' -and $todos -notmatch '^\[!!\]') {
  $msg = $msg + @"


Then refresh the task-list panel from this batch's ``progress.todos`` - write the
whole array, replacing the old list, so the widget matches the run and is not a
phase behind. Do not narrate step numbers in your reply; the panel names them.
"@
}
Emit-Context $msg
