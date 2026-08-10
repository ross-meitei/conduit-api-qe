---
description: 'Check existing tests against the current OpenAPI spec for contract drift.'
agent: appqe-conduit-api-qe
---
Check contract drift for **conduit-api-qe**.

Drive it through the orchestrator: `appqe start "check contract drift against the
openapi"`, then execute each phase and `appqe resume` with results.

- Load the current spec (`specs/openapi.json`) and index existing tests by
  operation + assertions (see the contract-diff skill).
- Diff tests vs spec; classify each finding **breaking / additive / cosmetic**.
- Deliver a drift report with the affected test files and recommended updates.

<!-- appqe:managed v0.12.1 | do-not-edit-manually | regenerate: appqe sync -->
