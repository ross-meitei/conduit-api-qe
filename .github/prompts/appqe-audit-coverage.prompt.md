---
description: 'Audit API/UI test coverage against the spec and app, and report prioritized gaps.'
agent: appqe-conduit-api-qe
---
Audit test coverage for **conduit-api-qe**.

Drive it through the orchestrator: `appqe start --mode audit "audit coverage"`, then execute
each phase and `appqe resume` with results.

- Inventory endpoints from `specs/openapi.yaml` and the critical UI flows.
- Map existing tests to endpoints/flows.
- Identify uncovered operations, missing negative/auth cases, and untested flows.
- Deliver a prioritized coverage report (public + write endpoints and critical
  flows first). Don't count "not 500"-only or schema-skipping tests as coverage.

<!-- appqe:managed v0.14.8 | do-not-edit-manually | regenerate: appqe sync -->
