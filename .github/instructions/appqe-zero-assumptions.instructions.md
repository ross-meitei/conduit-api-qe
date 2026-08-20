---
name: appqe-zero-assumptions
description: 'Applies to all API test generation and contract checks in **conduit-api-qe**'
applyTo: '**'
---
# Zero Assumptions - the OpenAPI spec is the source of truth

> Applies to all API test generation and contract checks in **conduit-api-qe**.

**Never assume an endpoint's shape from memory, a name, or a code comment.** The
`specs/openapi.json` OpenAPI/Swagger spec is the single source of truth for:

- Path, method, and operation id
- Required vs optional parameters (path/query/header/body) and their types
- Request and response **schemas** (validate response bodies against them)
- Declared status codes (assert the documented ones - success AND error)
- Security scheme (Token JWT (Authorization: Token <jwt>)) - every protected endpoint needs an
  authorized and an unauthorized case

Before writing or reviewing a test, resolve the endpoint in the spec and derive
EXPECTED values from it. A test that asserts values not grounded in the spec is
not verification-grade - it is a guess.

When the spec and the running API disagree, that is a finding (a contract
drift), not something to paper over in the test.

---

**Enforced by** `appqe lint --strict` (`undocumented-status-assertion`, `wrong-success-code` - the asserted code is compared against the spec's documented responses) and `appqe contract verify`, which settles whether the spec or the test is stale.

<!-- appqe:managed v0.12.11 | do-not-edit-manually | regenerate: appqe sync -->
