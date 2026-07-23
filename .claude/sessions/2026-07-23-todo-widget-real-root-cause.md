# Session — todo widget "not firing" in Copilot Chat: the REAL root cause

- **Date:** 2026-07-23
- **Repo:** conduit-api-qe · branch `qe/audit-remediation`
- **Symptom:** The live todo/task-list widget does not appear when running the
  `appqe-agent` in GitHub Copilot Chat.
- **Environment (this time, confirmed from screenshots):** VS Code **1.130.0**,
  **GitHub Copilot Chat 0.58.0**, appqe-agent selected, model Claude Sonnet 4.6,
  inside a **multi-root workspace** (`c-builder.code-workspace`, 7 folders).

## The real root cause (confirmed)

**appqe writes a setting name that VS Code 1.130 / Copilot 0.58 has renamed.**

- appqe's `editor_settings()` writes `chat.todoListTool.enabled` (+
  `chat.useCustomAgentHooks`) into `.vscode/settings.json`. Those were correct for
  **VS Code 1.103** (experimental). By **1.130** the widget setting is
  **`chat.tools.todos.showWidget`**; the old keys are now flagged
  **"Unknown Configuration Setting"** and do nothing.
- Empirical proof from the user's own editor:
  - `chat.tools.todos.showWidget: true` → **no warning** (valid, current).
  - `chat.todoListTool.enabled` / `chat.useCustomAgentHooks` → **both "unknown."**
- The **tool itself is fine.** The 🛠 tools picker showed **`todo` (singular),
  checked** — "Manage and track todo items for task planning." The agent's
  `tools: [… 'todo' …]` grant is correct (appqe's source even documents that
  `todo` singular is right and `todos` plural withholds the tool).
- With the correct setting on (`chat.tools.todos.showWidget: true`, set by the
  user at **workspace scope** in `c-builder.code-workspace`), typing **`#todo`**
  in the agent chat fired the tool and **rendered the Todos (1/10) widget**
  correctly. Plumbing is 100% intact.

### Two compounding factors
1. **Stale key** (primary): appqe set `chat.todoListTool.enabled`, inert on 1.130.
2. **Scope** (secondary): these are window-scoped chat settings. In a multi-root
   workspace they belong in the `.code-workspace` `settings` block or User
   settings — a per-folder `conduit-api-qe/.vscode/settings.json` (where appqe
   writes) is the wrong scope. The user correctly placed the setting at workspace
   scope.

## What was NOT the cause (dead ends — do not re-chase)
- **"Copilot Chat not installed."** FALSE. It is 0.58.0. The earlier logs (and
  this session's first pass) ran `code --list-extensions`, which resolves a
  *different* VS Code install/scope than the user's editor and does not list it.
  The screenshot of the extension + the working `#todo` are authoritative.
- **Tool name `todos` (plural).** FALSE. The picker confirms `todo` (singular).
- **Model not auto-invoking `todo`.** UNCONFIRMED — never tested on a normal run.
  `#todo` was an explicit invocation. The widget only failed to *display* before
  the setting was fixed; auto-invocation on a plain run should be re-tested before
  any agent-prompt surgery. The §0 mandate is already hoisted to the top.

## Fix
- **appqe source (durable):** `src/appqe/platforms/base.py` `editor_settings()`
  → `chat.tools.todos.showWidget: True`; update the QE.md template and
  `tests/test_content_sync.py`. Installed appqe is a **uv tool**, not editable —
  needs `uv tool install --reinstall` (or `-e`) before `appqe sync`/`doctor` pick
  up the change. See the appqe repo for the commit.
- **conduit-api-qe:** folder `.vscode/settings.json` reverted to `{}` — the
  workspace-scope `chat.tools.todos.showWidget` is what matters and is correct.
  `appqe doctor` will keep reporting `[!!]` until the CLI is reinstalled from the
  fixed source; that `[!!]` is now a **false alarm** (it checks the old key).

## UPDATE (2026-07-23, later) — auto-invocation is a hard Copilot 0.58 limit

Re-tested extensively. The setting fix (above) makes the widget *render when the
tool is called*, but the agent **cannot auto-call the tool** on Copilot Chat 0.58:

- Across multiple runs the model reports **`manage_todo_list` is not in its
  available tools** and falls back to printing a markdown checklist (no widget).
- The tools picker shows `todo` **checked/granted**, and typed **`#todo` works** —
  so the tool exists, but is only injected when the **user types `#todo` in their
  own message**. It is NOT handed to the model via any of:
  - the agent `tools: [… 'todo' …]` allowlist,
  - a `#todo` reference inside a `.prompt.md` (tested — no injection),
  - the agent-body instructions.
- Not model-specific (user confirmed switching models doesn't change it).
- Tested and ruled out as causes: tool name (`todo` is correct), the `tools:`
  allowlist (removing it entirely still yields no tool), a `#todo` reference in a
  `.prompt.md` (no injection), and a companion "enable/offer" setting (none exists
  on 0.58 beyond `chat.tools.todos.showWidget`, which only controls rendering).
- Exact upstream cause NOT pinned. (An earlier draft blamed vscode#269055 - that
  issue is from 2025 and may be stale/unrelated; do not cite it as the cause. The
  *behaviour* is proven; the specific VS Code/Copilot bug behind it is not.)

**Conclusion (CONFIRMED WORKING 2026-07-23):** appqe is configured correctly. On
Copilot 0.58 the todo tool is injected into a custom agent's model **only when the
user types `#todo` in their own message**. Verified end to end:

    #todo Generate api tests for the /user endpoint

renders the widget AND drives the full GENERATION template (10 todos, enriched
titles, gate pauses) - screenshot on file. Bare `/appqe-generate-api-tests` does
NOT inject the tool; the `#todo` must be in the typed message. Across gate turns,
re-include `#todo` to keep updates flowing.

### Ruled out: granting `manage_todo_list` via the `tools:` allowlist (2026-07-23)

Prompted by vscode-copilot-release#14096 (no `tools: ['*']` wildcard; users must
list built-ins explicitly), we added `'manage_todo_list'` to the agent's `tools:`.
A no-`#todo` run then *appeared* to work - a panel rendered. It was a **false
positive**: VS Code flagged the entry `Unknown tool 'manage_todo_list' will be
ignored` (promptValidator), and the agent's own reasoning admitted it had created
the list "through **vscode_askQuestions as a workaround** since manage_todo_list
isn't directly available." So the "panel" was an **askQuestions card**, not the
native todo widget (note the Submit button vs. the real "Todos (n/n)" panel from a
`#todo` run). The allowlist entry is a **no-op on Copilot 0.58** and was reverted.

**Net:** there is currently NO way to grant `manage_todo_list` to a custom agent's
model on this build - not the `todo` alias, not `manage_todo_list` (ignored), not
removing the allowlist (no wildcard). Only a **user-typed `#todo`** injects the
real widget. Track vscode-copilot-release#14096 for when explicit/​wildcard tool
inclusion actually lands.

## Open next steps
- [x] Reinstall appqe from fixed source; `doctor` now checks
      `chat.tools.todos.showWidget` (the `[!!]` that remains is the folder-vs-
      workspace scope false-alarm).
- [x] Re-test auto-invocation — confirmed it does NOT work on Copilot 0.58 (above).
- [ ] File/track a VS Code issue (ref vscode#269055) for the todo tool not being
      offered to custom agents; revisit when Copilot Chat updates.
- [ ] Workaround for users now: prefix appqe-agent runs with `#todo`.
- [ ] Decide the uncommitted branch pile (Claude harness, hooks, findings/index).
