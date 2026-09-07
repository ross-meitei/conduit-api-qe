# PostToolUse - after a batch, put the run's status block back in front of the
# agent. Static instructions are read once and drift; this rides along with the
# work, which is the only channel looked at as often as the block changes.
# Advises, never blocks: always exit 0.
#
# Emitted as `hookSpecificOutput.additionalContext` JSON, the only documented way
# to get a PostToolUse hook's words in front of the model. Plain stdout - what
# this printed for its whole life - reaches neither host. See the .sh twin.
$ErrorActionPreference = 'SilentlyContinue'
# --plain: bare text instead of the JSON envelope, for a host that adds hook
# stdout to context verbatim (Kiro). Passed by the deployed hook config.
$Shape = ''
if ($args -contains '--plain') { $Shape = 'plain' }
$i = [array]::IndexOf($args, '--shape')
if ($i -ge 0 -and $i + 1 -lt $args.Count) { $Shape = $args[$i + 1] }
. (Join-Path $PSScriptRoot 'appqe-resolve.ps1')
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$payload = [Console]::In.ReadToEnd()
# Containment: a foreign session's batches are another project's runs.
if (Test-AppqeEventIsForeign $payload $appqeRoot) { exit 0 }

if ($payload -notmatch 'appqe[ _](start|resume|discovery|list|status)' -and
    $payload -notmatch 'qe_(start|resume)') { exit 0 }

# Ownership stamp: a run BELONGS to the session that drove it - see the .sh
# twin. The start/resume decision reads the COMMAND or tool name, never the
# whole payload: `appqe list`'s own output prints "appqe resume <id>" hints for
# every open run, and matching the response would stamp them all as this
# session's, defeating the attribution.
$cmd = ""; $tool = ""
# Escaped-quote-aware value patterns - `cd \"My Project\" && appqe resume ...`
# must not truncate at the backslash and skip the stamp. See the .sh twin.
if ($payload -match '"command"\s*:\s*"((?:\\.|[^"\\])*)"') { $cmd = $Matches[1] }
if ($payload -match '"tool_name"\s*:\s*"((?:\\.|[^"\\])*)"') { $tool = $Matches[1] }
if ($cmd -match 'appqe (start|resume)' -or $tool -match '^(app)?qe_(start|resume)$') {
  # Shared with the stop hook, which READS the stamps written below - the two
  # must agree on the key or attribution silently breaks (see the .sh twin).
  $session = Get-AppqeSessionId $payload
  # "default" is not attributable: every anonymous session would inherit every
  # other one's runs. The shared helper returns it; ownership must not use it.
  if ($session -eq "default") { $session = "" }
  if ($session) {
    $project = ($appqeRoot -replace '[^A-Za-z0-9._-]', '')
    if ($project.Length -gt 60) { $project = $project.Substring($project.Length - 60) }
    if (-not $project) { $project = "project" }
    $markerDir = Get-AppqeSessionDir $appqeRoot
    New-Item -ItemType Directory -Force -Path $markerDir | Out-Null
    # Ids ONLY from the command and from "run_id" keys - never free response
    # text, whose "Awaiting runs"/refusal listings name other sessions' runs.
    # See the .sh twin.
    $ids = @([regex]::Matches($cmd, 'qe-[0-9a-f]{6,}') | ForEach-Object { $_.Value })
    $ids += [regex]::Matches($payload, 'run_id[\\"]*\s*:\s*[\\"]*(qe-[0-9a-f]{6,})') |
      ForEach-Object { $_.Groups[1].Value }
    $ids = $ids | Sort-Object -Unique
    if ($ids) { Add-Content -Path (Join-Path $markerDir "runs-$project-$session") -Value ($ids -join "`n") }
  }
}

$appqe = Resolve-Appqe
if (-not $appqe) { exit 0 }            # no CLI, no advice - never block the turn

function Emit-Context([string] $text) {
  # Claude Code caps hook output strings, additionalContext included, at 10,000
  # characters. Truncate rather than have a long run's block dropped wholesale.
  if ($text.Length -gt 9500) { $text = $text.Substring(0, 9500) }
  if ($Shape -eq 'plain') {
    $text
    exit 0
  }
  # ConvertTo-Json handles the escaping, and -Compress keeps it one line.
  # Cursor reads a top-level `additional_context`; the others read the envelope.
  if ($Shape -eq 'cursor') {
    ConvertTo-Json ([ordered]@{ additional_context = $text }) -Depth 5 -Compress
    exit 0
  }
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
#
# One call, not two: the target line is empty exactly when there are no todos to
# write, so its emptiness answers "is there a panel to refresh" as well. And the
# nudge names a number the agent can CHECK against the widget in front of it -
# "write the whole array" is advice it cannot verify it followed, which is how a
# panel sat at 4-of-13 while appqe handed over 13 correct items.
$target = (& $appqe --project-root $appqeRoot progress --todos --panel-target 2>$null) -join ''
if ($target -and $target -notmatch '^\[!!\]') {
  $msg = $msg + @"


Then refresh the task-list panel from this batch's ``progress.todos`` - write the
whole array, replacing the old list, so the widget matches the run and is not a
phase behind. Do not narrate step numbers in your reply; the panel names them.
**After writing it the panel must read exactly: $target.** Compare it; if it
does not match, you did not write this batch's array - write it again now.
"@
}
Emit-Context $msg
