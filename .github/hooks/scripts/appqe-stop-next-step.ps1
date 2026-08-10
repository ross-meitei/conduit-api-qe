# Stop - refuse the first attempt to end a session without offering next steps.
. "$PSScriptRoot\appqe-resolve.ps1"
$appqeRoot = Get-AppqeProjectRoot $PSScriptRoot
$appqe = Resolve-Appqe
# Refresh the telemetry dashboard so every session leaves a readable record. The
# .sh twin has always done this; without it a Windows session ended with a stale
# report, which is the kind of drift that makes two scripts stop being one hook.
if ($appqe) { & $appqe --project-root $appqeRoot dashboard *> $null }

$payload = [Console]::In.ReadToEnd()
$session = "default"
if ($payload -match '"session_id"\s*:\s*"([^"]*)"') { $session = $Matches[1] }
# Never build a path out of unsanitised input: this comes straight from the hook
# payload, so a "..\" in it would write the marker outside the temp directory.
$session = ($session -replace '[^A-Za-z0-9._-]', '')
if (-not $session) { $session = "default" }
# Outside the repo: a once-per-session marker is not a project artifact.
#
# Namespaced per project as well as per session: keyed on the session alone with a
# shared "default" fallback, one repo's marker suppressed another repo's prompt.
# The hook's OWN project, not the cwd - see the .sh twin.
$projectRoot = $appqeRoot
$project = ($projectRoot -replace '[^A-Za-z0-9._-]', '')
if ($project.Length -gt 60) { $project = $project.Substring($project.Length - 60) }
if (-not $project) { $project = "project" }
$markerDir = Join-Path ([System.IO.Path]::GetTempPath()) "appqe-session"
$marker = Join-Path $markerDir "stop-prompted-$project-$session"
New-Item -ItemType Directory -Force -Path $markerDir | Out-Null
# Markers are never read after their session ends, so sweep the stale ones rather
# than growing that directory for the life of the machine.
Get-ChildItem -Path $markerDir -Filter 'stop-*' -File -ErrorAction SilentlyContinue |
  Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
  Remove-Item -Force -ErrorAction SilentlyContinue

# Name the runs that never finished. A run abandoned at run_tests has generated
# tests nobody has seen pass, recorded nothing in the discovery log and produced
# no report - and it looks finished, because the phases it skipped are the last.
#
# Checked BEFORE the once-per-session marker and not covered by it: a session
# ended with four steps never run because the generic prompt had already spent
# the single block. "Offer next steps" is a nicety that must not trap anyone; an
# unfinished run is a concrete state with a one-command escape (`appqe abandon`),
# so it may keep blocking. Bounded at 3 either way.
$stalled = ""
if ($appqe) { $stalled = (& $appqe --project-root $appqeRoot list --incomplete 2>$null | Select-Object -First 5) -join "`n" }
if ($stalled) {
  $triesFile = Join-Path $markerDir "stop-stalled-$project-$session"
  $tries = 0
  if (Test-Path $triesFile) { [int]::TryParse((Get-Content $triesFile -Raw -ErrorAction SilentlyContinue), [ref]$tries) | Out-Null }
  $tries = $tries + 1
  Set-Content -Path $triesFile -Value $tries -ErrorAction SilentlyContinue
  if ($tries -le 3) {
    [Console]::Error.WriteLine("appqe: this session still owns runs that never completed:")
    [Console]::Error.WriteLine($stalled)
    [Console]::Error.WriteLine("Settle each one before wrapping up: resume it (appqe resume <run_id> ...), or if it will not be finished, close it on the record with 'appqe abandon <run_id> --reason ""..."" ' (--all clears every one) and say what is therefore unverified. **A step you cannot get past is not a step to narrate around**: report it FAILED through appqe with the reason, so the run records what stopped it instead of ending with the step still open. Then ask a structured next-step question with concrete options, plus 'Done for now' and 'Something else'. (Prompt $tries of 3; after that this stops blocking and the runs stay open.)")
    exit 2
  }
  [Console]::Error.WriteLine("appqe: [!!] ending with run(s) still incomplete after 3 prompts. They stay open, 'appqe list --incomplete' keeps reporting them, and whatever they generated is unverified.")
  exit 0
}

if (Test-Path $marker) { exit 0 }
New-Item -ItemType File -Force -Path $marker | Out-Null
# Phrased for two readers at once - see the .sh twin. The model needs an
# unambiguous instruction; the user sees this under a host label that already says
# "Blocked", where "do not end here" read as a reprimand for a session in which
# nothing was wrong. The trailing sentence was missing here entirely, so the two
# twins gave different instructions on different platforms.
[Console]::Error.WriteLine("appqe: one step left before this session ends - give the user their next steps. Recompute the progress line, then ask a structured next-step question: concrete options drawn from the current run state, plus 'Done for now' and 'Something else'. This runs once per session - the next stop ends normally.")
exit 2
