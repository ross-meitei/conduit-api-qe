---
description: 'Investigate a failing API or UI test and deliver a root-cause verdict.'
agent: appqe-conduit-api-qe
tools: ['read', 'search', 'edit', 'execute', 'agent', 'todo', 'vscode/askQuestions']
---
Investigate the failing test for **conduit-api-qe**.

Drive it through the orchestrator: `appqe start --mode investigation "investigate <paste the failure>"`,
then execute each phase and `appqe resume` with results.

- Read the discovery log first - the failure may already be explained.
- Reproduce and capture the exact error (status/body for API; trace/screenshot for
  UI). Redact secrets.
- Triage: **test defect** vs **app defect** vs **environment/flake** (see the
  flaky-triage skill).
- Verify the root cause with evidence; run a challenge step to disprove it.
- Deliver a verdict + recommended fix; update the discovery log.


> ## Task panel - set this up first
>
> **Before you execute any step**, run the `progress.todo_call` line
> that `appqe start` returns. It is a complete `manage_todo_list` call
> with the whole run already written into it - run it as-is.
>
> **Never build the items yourself, and never build the panel out of
> `actions`.** `actions` is what you execute; `progress.todo_call` is
> what you display. They are different lengths on purpose - a 14-step
> run showing a 3-item panel is one batch's actions in the window where
> the run should be.
>
> Re-run `progress.todo_call` on every batch: it carries the current
> step as `in-progress` and settled ones as `completed`, and the tool
> replaces the list it is given, so the fresh line is always the whole
> truth. On the last batch run it once more, with everything done.
>
> If `manage_todo_list` is not in your tool list, it is not there - do
> not search for it. Say so in one line above the opener in
> your first reply - once for the whole run - and carry on. The status
> block in each reply is the step list either way. **The user must type
> `#todo` in their own message**; nothing a project file can do grants
> it, so naming that is the only useful thing you can say about it.

<!-- appqe:managed v0.14.11 | do-not-edit-manually | regenerate: appqe sync -->
