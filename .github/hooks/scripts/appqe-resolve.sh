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
