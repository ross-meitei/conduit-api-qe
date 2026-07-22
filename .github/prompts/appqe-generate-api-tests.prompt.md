---
description: 'Generate API tests from the OpenAPI spec into the project''s stack.'
agent: appqe-agent
---
**Keep the todo list live.** Seed it before your first action and update it at every step transition - see *Todo visibility protocol* in the agent. Seeding once and moving on is the failure this is here to prevent.

Generate API tests for **conduit-api-qe** from `specs/openapi.json`.

Drive it through the orchestrator: `appqe start "generate api tests from the
openapi spec"`, then execute each phase and `appqe resume` with results.

- Stack: **xUnit** (C#); run with `dotnet test`.
- Source of truth: the OpenAPI spec (zero assumptions).
- Per endpoint: happy, edge, error, and auth cases (see the assertion standards).
- Stop at the **plan** gate for approval before generating, and at the
  **generated-test review** gate before considering them final.

<!-- appqe:managed | do-not-edit-manually -->
