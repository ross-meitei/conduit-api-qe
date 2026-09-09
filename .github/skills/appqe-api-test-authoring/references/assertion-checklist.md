<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
# Reference: assertion checklist by response class

Load this when writing assertions for a status class you have not covered yet.
It is deliberately not in SKILL.md - it is long, and only one section is ever
relevant at a time.

## 2xx success

- Exact documented code (`200` vs `201` vs `204` are different contracts).
- `201` -> `Location` header present and pointing at the created resource.
- `204` -> body is empty; asserting a schema here is a bug in the test.
- Response validates against the documented success schema.
- Every field the spec marks required is present and non-null.
- Round-trip: values sent are the values returned (allowing for
  server-generated ids, timestamps, and normalised casing).

## 4xx client error

- Exact documented code. `400` vs `422` is a real distinction: malformed
  syntax vs semantically invalid content.
- `401` when unauthenticated; `403` when authenticated but not permitted.
  A test that accepts either is asserting nothing.
- `404` for an id that does not exist - and confirm it is not leaking whether
  the resource exists for a *different* tenant.
- `409` for a genuine conflict (duplicate unique field).
- Error body validates against the documented error schema.
- The error names the offending field. "Something went wrong" is a product bug
  worth raising, not a test to weaken.

## 5xx server error

Never assert a 5xx as expected behaviour. A 5xx in a test run is a defect -
record it and raise it. The only legitimate case is a deliberately injected
fault in a resilience test, and that must be explicitly commented as such.

## Headers and content type

- `Content-Type` matches the spec, including charset when declared.
- Pagination headers or envelope fields when the endpoint is a collection.
- Cache headers where the spec declares them.
- No unexpected `Set-Cookie` on a token-authenticated API.

## What does not count as schema validation

A generated client (NSwag, openapi-generator, oapi-codegen) deserialising the
body into a typed DTO is **not** a schema check. Generated DTOs are permissive:
a missing `required` field becomes `null`, an unexpected field is dropped, and a
wrong-but-coercible type is converted. The response can violate the contract in
all three ways and still deserialise cleanly.

Validate the body against the documented schema itself - `components.schemas`,
resolved - then assert the fields the case is about. If the stack has no
validator handy, assert every `required` field explicitly and say in the test
why. A comment asserting that the client validates the schema is documentation
of an assumption, not a check.

## Anti-patterns

| Anti-pattern | Why it fails |
|---|---|
| `assert status < 500` | Passes for 404 on a happy-path test. |
| `assert response is not None` | Passes for every error body ever returned. |
| Asserting only the count of a collection | Misses wrong ordering and wrong contents. |
| Snapshotting the whole body | Breaks on every unrelated field addition. |
| Re-asserting the schema field by field | Duplicates the schema check; assert the schema, then the fields this case is about. |
