# Session — Claude Code harness setup + todo-panel root cause closed

- **Date:** 2026-07-22
- **Repo:** conduit-api-qe · branch `qe/audit-remediation`
- **Goal:** Stop chasing the missing Copilot todo panel; stand up a Claude Code
  harness for this workspace with session tracking and an efficient workflow.

## Root cause of "todo panel not showing" (now closed)

The live task-list panel is a **GitHub Copilot Chat** agent-mode feature
(`chat.todoListTool.enabled`, the `manage_todo_list` widget, `.agent.md` custom
agents with `target: vscode`). Per VS Code / GitHub docs it requires the
**GitHub Copilot Chat extension**.

That extension is **not installed anywhere** on this machine (checked stable CLI,
insiders, and on-disk extension dirs). The only "copilot" extensions are
unrelated (`qe-team.copilot-slides-agent`, `ms-azuretools.vscode-azure-github-copilot`,
`tokenystcopilot.tokenyst-copilot`). The only AI chat extension present is
**`anthropic.claude-code@2.1.218`**.

So every config item the earlier session verified/fixed was correct but targeted
an absent host — which is why restoring the `todo` tool grant changed nothing.
The prior log had flagged this as "secondary / verify"; it was primary.

**Decision:** drive this workspace through Claude Code (installed) instead of the
Copilot host. Claude Code renders its own todo list natively (TodoWrite) — that
is the panel equivalent.

## What was set up

- `CLAUDE.md` — repo guide: build/test/client-gen commands, appqe read-only
  checks, layout, conventions, current suite state, and the session/todo workflow.
- `.claude/settings.json` — committed permission allowlist so common
  `dotnet` / `nswag` / read-only `appqe` / `allure` / read-only `git` commands
  don't prompt. (`settings.local.json` with its two machine-local allows kept.)
- `.claude/sessions/` — this session-log folder + this file.

## Open next steps

- [ ] If the Copilot panel is ever genuinely wanted, install `github.copilot` +
      `github.copilot-chat`, reload, select the appqe agent in agent mode.
- [ ] Consider porting the appqe QE workflow to a Claude Code subagent
      (`.claude/agents/appqe-agent.md`) if the lightweight setup proves too thin.
- [ ] Uncommitted changes still on the branch: todo-cadence hooks
      (`.github/hooks/...`) and `findings/index.md` — decide whether to commit.

---

## CORRECTION (2026-07-23) — "extension not installed" was wrong

Superseded by `.claude/sessions/2026-07-23-todo-widget-real-root-cause.md`.

GitHub Copilot Chat **is** installed (**0.58.0**) — confirmed by screenshot and a
working `#todo` that renders the widget. The `code --list-extensions` /
on-disk checks resolved a different VS Code scope than the user's actual editor,
producing a false "not installed." The decision to drive this workspace through
Claude Code still stands as a *choice*, but it was **not forced** by a missing
extension. The real todo-widget blocker was a **stale VS Code setting name**
(`chat.todoListTool.enabled` → `chat.tools.todos.showWidget` on VS Code 1.130),
fixed in appqe source.
