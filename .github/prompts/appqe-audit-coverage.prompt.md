---
description: 'Audit API/UI test coverage against the spec and app, and report prioritized gaps.'
agent: appqe-agent
---
**Keep the todo list live.** Seed it before your first action and update it at every step transition - see *Todo visibility protocol* in the agent. Seeding once and moving on is the failure this is here to prevent.

Audit test coverage for **conduit-api-qe**.

Drive it through the orchestrator: `appqe start "audit coverage"`, then execute
each phase and `appqe resume` with results.

- Inventory endpoints from `specs/openapi.json` and the critical UI flows.
- Map existing tests to endpoints/flows.
- Identify uncovered operations, missing negative/auth cases, and untested flows.
- Deliver a prioritized coverage report (public + write endpoints and critical
  flows first). Don't count "not 500"-only or schema-skipping tests as coverage.

<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
