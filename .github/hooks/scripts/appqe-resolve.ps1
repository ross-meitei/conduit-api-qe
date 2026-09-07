# Locate the appqe CLI. Dot-sourced by the hook scripts.
# A GUI editor does not inherit the login shell's PATH, so a user-scoped install
# is invisible to it even though it works in a terminal.
function Resolve-Appqe {
  if ($env:APPQE_BIN -and (Test-Path $env:APPQE_BIN)) { return $env:APPQE_BIN }
  $cmd = Get-Command appqe -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  foreach ($c in @(
      "$env:USERPROFILE\.local\bin\appqe.exe",
      "$env:LOCALAPPDATA\uv\tools\appqe\Scripts\appqe.exe",
      "$env:APPDATA\Python\Scripts\appqe.exe",
      ".venv\Scripts\appqe.exe")) {
    if (Test-Path $c) { return $c }
  }
  return $null
}

# The project this hook belongs to, resolved from the hook script's own location
# rather than the process cwd. See appqe-resolve.sh for why: with no root the CLI
# falls back to `.`, which in a multi-root workspace is whichever folder the editor
# handed the hook - so the guard judged one project's command against another
# project's security.allowed_hosts.
#
# Deploy layout is <root>/<.github|.appqe>/hooks/scripts/, so root is three up.
function Get-AppqeProjectRoot {
  param([string]$ScriptDir)
  $root = Join-Path $ScriptDir "..\..\.."
  try { return (Resolve-Path -LiteralPath $root -ErrorAction Stop).Path } catch { return "." }
}

# Containment for multi-root hosts - see appqe-resolve.sh for the reasoning.
function Test-AppqeEventIsForeign {
  param([string]$Payload, [string]$Root)
  $cwd = $null
  if ($Payload -match '"cwd"\s*:\s*"([^"]*)"') { $cwd = $Matches[1] }
  if (-not $cwd) {
    # Cursor's sessionStart/stop payloads carry workspace_roots, never cwd -
    # any root inside/above this project keeps the event ours (.sh twin).
    if ($Payload -match '"workspace_roots"\s*:\s*\[([^\]]*)\]') {
      try { $rootR = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).Path } catch { return $false }
      foreach ($m in [regex]::Matches($Matches[1], '"([^"]*)"')) {
        try { $wr = (Resolve-Path -LiteralPath $m.Groups[1].Value -ErrorAction Stop).Path } catch { continue }
        if ($wr.StartsWith($rootR) -or $rootR.StartsWith($wr)) { return $false }
      }
      return $true
    }
    return $false
  }
  try {
    # The .sh twin resolves PHYSICAL paths (pwd -P): macOS aliases /tmp to
    # /private/tmp and a logical cwd made the project's own events look
    # foreign. Windows has no equivalent default alias, and Resolve-Path's
    # normalization covers the drive-case/8.3 variants that exist here.
    $cwdR  = (Resolve-Path -LiteralPath $cwd -ErrorAction Stop).Path
    $rootR = (Resolve-Path -LiteralPath $Root -ErrorAction Stop).Path
  } catch { return $false }
  if ($cwdR.StartsWith($rootR) -or $rootR.StartsWith($cwdR)) { return $false }
  return $true
}

# Twins of appqe_session_id / appqe_session_dir in appqe-resolve.sh - see there
# for the reasoning (#138: a once-per-session nudge firing after every response).
function Get-AppqeSessionId {
  param([string]$Payload)
  # conversation_id FIRST: a host that also sends a per-REQUEST session_id
  # re-keys the marker every turn when session_id wins.
  $id = ""
  if ($Payload -match '"conversation_id"\s*:\s*"([^"]*)"') { $id = $Matches[1] }
  if (-not $id -and $Payload -match '"session_id"\s*:\s*"([^"]*)"') { $id = $Matches[1] }
  # Never build a path out of unsanitised input.
  $id = ($id -replace '[^A-Za-z0-9._-]', '')
  if (-not $id) { $id = "default" }
  return $id
}

function Get-AppqeSessionDir {
  param([string]$ProjectRoot)
  # .git/appqe when there is a repo: a marker has to survive between two turns of
  # one session, and a per-invocation temp directory does not. Still outside the
  # working tree, so it stays out of the explorer and out of `git status`.
  if ($ProjectRoot -and (Test-Path (Join-Path $ProjectRoot ".git") -PathType Container)) {
    return (Join-Path (Join-Path $ProjectRoot ".git") "appqe\session")
  }
  return (Join-Path ([System.IO.Path]::GetTempPath()) "appqe-session")
}
