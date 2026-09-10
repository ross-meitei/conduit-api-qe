---
description: 'Audit API/UI test coverage against the spec and app, and report prioritized gaps.'
agent: appqe-conduit-api-qe
tools: ['read', 'search', 'edit', 'execute', 'agent', 'todo', 'vscode/askQuestions']
---
Audit test coverage for **conduit-api-qe**.

Drive it through the orchestrator: `appqe start --mode audit "audit coverage"`, then execute
each phase and `appqe resume` with results.

- Inventory endpoints from `specs/openapi.yaml` and the critical UI flows.
- Map existing tests to endpoints/flows.
- Identify uncovered operations, missing negative/auth cases, and untested flows.
- Deliver a prioritized coverage report (public + write endpoints and critical
  flows first). Don't count "not 500"-only or schema-skipping tests as coverage.


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
