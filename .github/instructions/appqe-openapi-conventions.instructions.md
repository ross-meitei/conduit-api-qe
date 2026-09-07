---
name: appqe-openapi-conventions
description: 'Spec: `specs/openapi.yaml` (OpenAPI 3.x or Swagger 2.0)'
applyTo: 'tests/**'
---
# Working from an OpenAPI / Swagger spec

> Spec: `specs/openapi.yaml` (OpenAPI 3.x or Swagger 2.0).

**Load and validate first.** Parse the spec, confirm it is well-formed, and note
the version, `servers`/`basePath`, and `securitySchemes`. If the spec is a URL,
fetch a pinned copy so a run is reproducible.

**Derive the test matrix from the spec, not from prose:**

- Enumerate every `paths.<path>.<method>` operation.
- For each, read `parameters` + `requestBody` (required/optional, schema) and
  `responses` (every declared code + its schema).
- Group by `tags` for file/suite organization; name tests by operationId.

**$ref and components.** Resolve `$ref`s into `components/schemas`. Reuse those
schemas for response validation so tests stay in lock-step with the contract.

**Examples.** Use `example`/`examples` from the spec as fixture seeds when present
- they are the API owner's own canonical payloads.

**Drift.** When the spec changes, the contract-check workflow diffs existing
tests against it: renamed/removed operations, new required params, changed
schemas or status codes are breaking; new optional fields are additive.

---

**Enforced by** `appqe openapi` (the spec must parse and produce an inventory) and `appqe contract verify` (drift against the running API).

<!-- appqe:managed v0.14.8 | do-not-edit-manually | regenerate: appqe sync -->
