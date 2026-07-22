---
name: appqe-api-assertion-standards
description: 'Framework: **xUnit** (C#). Runner: `dotnet test`'
applyTo: 'tests/api/**'
---
# API Assertion Standards

> Framework: **xUnit** (C#). Runner: `dotnet test`.
> Base URL: `https://api.realworld.show/api`.

Every generated API test must assert, at minimum:

1. **Status code** - the documented code for the case (2xx happy; the specific
   4xx/5xx for error cases). Never assert only "not 500".
2. **Response schema** - validate the body against the OpenAPI response schema
   (shape, required fields, types), not just a couple of fields.
3. **Key field values** - for cases with a known expected value, assert it
   explicitly (EXPECTED vs ACTUAL), with a message naming the field.
4. **Headers/content-type** where the spec declares them.

Case coverage per endpoint (the plan gate enforces this):

- **Happy path** - valid request -> documented success.
- **Edge** - boundaries, empty/optional fields, pagination limits.
- **Error** - missing required params, invalid types, not-found ids -> the
  documented error code + error schema.
- **Auth** - unauthenticated and (where roles exist) unauthorized -> 401/403.

Keep tests **independent and idempotent**: no shared mutable state across tests;
create and clean up fixtures within the test or a scoped fixture. Assertion
messages must include enough context (endpoint, case, EXPECTED vs ACTUAL) to
diagnose a failure without re-running.

---

**Enforced by** `appqe lint <test dirs> --strict` (missing/ambiguous/ranged status assertions, null-only assertions, absent schema validation) and `appqe coverage --kind api` (happy + error + auth per operation).

<!-- appqe:managed | do-not-edit-manually -->
