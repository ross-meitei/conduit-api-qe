---
description: 'Check existing tests against the current OpenAPI spec for contract drift.'
agent: appqe-agent
---
**Keep the todo list live.** Seed it before your first action and update it at every step transition - see *Todo visibility protocol* in the agent. Seeding once and moving on is the failure this is here to prevent.

Check contract drift for **conduit-api-qe**.

Drive it through the orchestrator: `appqe start "check contract drift against the
openapi"`, then execute each phase and `appqe resume` with results.

- Load the current spec (`specs/openapi.json`) and index existing tests by
  operation + assertions (see the contract-diff skill).
- Diff tests vs spec; classify each finding **breaking / additive / cosmetic**.
- Deliver a drift report with the affected test files and recommended updates.

<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
