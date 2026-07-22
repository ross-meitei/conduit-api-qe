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
