---
description: 'Generate API tests from the OpenAPI spec into the project''s stack.'
agent: appqe-agent
---
Generate API tests for **conduit-api-qe** from `specs/openapi.json`.

Drive it through the orchestrator: `appqe start "generate api tests from the
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

<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
