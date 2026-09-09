---
description: 'Generate API tests from the OpenAPI spec into the project''s stack.'
agent: appqe-conduit-api-qe
tools: ['read', 'search', 'edit', 'execute', 'agent', 'todo', 'vscode/askQuestions']
---
Generate API tests for **conduit-api-qe** from `specs/openapi.json`.

Drive it through the orchestrator: `appqe start --mode api_generation "generate api tests from the
openapi spec"`, then execute each phase and `appqe resume` with results.

> **⛔ Output:** every `appqe start`/`resume` prints a finished, emoji-formatted
> status block. **Paste that block verbatim as your reply — it is your whole
> message.** Do not narrate ("Opening the session", "Run started", "Now let me…").
> The tool commands collapse on their own; the block is the update.

- Stack: **xUnit** (C#); run with `dotnet test`.
- Source of truth: the OpenAPI spec (zero assumptions).
- Per endpoint: happy, edge, error, and auth cases (see the assertion standards).
- Stop at the **plan** gate for approval before generating, and at the
  **generated-test review** gate before considering them final.


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

<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
