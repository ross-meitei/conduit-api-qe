# Locate the appqe CLI. Sourced by the hook scripts.
#
# A GUI editor does not inherit the login shell's PATH, so a user-scoped install
# (uv tool, pipx, ~/.local/bin) is invisible to it even though it works in a
# terminal. Resolve explicitly rather than assume PATH.
appqe_resolve() {
  if [ -n "${APPQE_BIN:-}" ] && [ -x "${APPQE_BIN}" ]; then
    printf '%s' "${APPQE_BIN}"; return 0
  fi
  _found="$(command -v appqe 2>/dev/null)"
  if [ -n "$_found" ]; then printf '%s' "$_found"; return 0; fi
  for _c in "$HOME/.local/bin/appqe" \
            "$HOME/.local/share/uv/tools/appqe/bin/appqe" \
            "$HOME/.pyenv/shims/appqe" \
            "/opt/homebrew/bin/appqe" \
            "/usr/local/bin/appqe" \
            "$HOME/.venv/bin/appqe" \
            ".venv/bin/appqe"; do
    [ -x "$_c" ] && { printf '%s' "$_c"; return 0; }
  done
  return 1
}

# The project this hook belongs to, resolved from the hook script's own location
# rather than the process cwd.
#
# Every hook used to invoke appqe with no root, so the CLI fell back to `.`. In a
# single-folder window cwd IS the project and nothing looks wrong. In a multi-root
# workspace it is whichever folder the editor happened to hand the hook, and the
# hooks then answer for a DIFFERENT project than the one whose file was touched:
# the guard reads a sibling's security.allowed_hosts and blocks a sanctioned host
# (reported live - a run against www.xfinity.com was refused against a sibling's
# allowlist of demo.realworld.show, while the agent read the correct appqe.yml and
# could not reconcile the two), `progress` reports a sibling's run, `discovery
# context` injects a sibling's findings.
#
# The scripts are deployed to <root>/<.github|.appqe>/hooks/scripts/, so the root
# is three levels up from here - a fact about the deploy layout, not about how the
# editor was launched.
appqe_project_root() {
  _dir="${1:-$(dirname "$0")}"
  ( CDPATH= cd -- "$_dir/../../.." 2>/dev/null && pwd ) || printf '%s' "."
}
