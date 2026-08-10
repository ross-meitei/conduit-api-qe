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
