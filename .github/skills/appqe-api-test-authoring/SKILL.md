---
name: appqe-api-test-authoring
description: 'Author API tests in the project''s stack from the endpoint inventory - Arrange-Act-Assert, one behaviour per test, complete assertions.'
metadata: {version: 1.0.0, access: authoring}
user-invocable: false
---
# Skill: Author API tests

Stack: **xUnit** (C#); HTTP via HttpClient / RestSharp; assertions via
FluentAssertions; tests live in `tests`; run with `dotnet test`.

Before the first test: read two existing tests and the utilities/helpers directory,
and build on what the suite already has - assume nothing about its structure.


## Structure: Arrange-Act-Assert

Every test has three visible sections, in order, separated by a blank line. One
**Act** per test - if you need two, you have two tests.

```
Arrange   build the request: fixtures, auth, test data, preconditions
Act       exactly one call to the endpoint under test
Assert    status, then schema, then the field values the case is about
```

Keep the sections honest: no assertions in Arrange, no request-building in
Assert. A reader who knows nothing about the endpoint should be able to point at
the one line that exercises it.

## Naming

`<Endpoint>_<Condition>_<ExpectedOutcome>` - e.g. `CreateArticle_MissingTitle_Returns422`.
The name states the behaviour, so a failure in CI is diagnosable from the test
name alone, without opening the file.

## Cases per endpoint

The plan gate enforces this set:

- **Happy** - valid request -> documented success code + response schema.
- **Edge** - boundaries, optional and empty fields, pagination limits.
- **Error** - missing/invalid params, unknown ids -> the documented error code
  and error schema.
- **Auth** - unauthenticated, and unauthorized if roles exist -> 401/403.

## Assertions

Every test asserts, in this order:

1. **Status** - the documented code. Never "not 500", never a range.
2. **Schema** - the response validates against the documented schema. A typed
   client deserialising the body is *not* this check - see the reference.
3. **Values** - the fields this case is about, EXPECTED vs ACTUAL, with a message
   naming the field.
4. **Headers/content-type** where the spec declares them.

A test that only asserts status is not a test; it is a smoke check. Assert what
the endpoint *promised*, not merely that it answered.

For a status class you have not covered before - 201 vs 204, 400 vs 422, 401 vs
403 - read `references/assertion-checklist.md` in this skill. It is kept out of
this file deliberately: only one section is ever relevant, so it loads only when
you actually need it.

## Independence

Each test sets up and tears down its own data, in a scoped fixture. No test
depends on another's leftovers, on execution order, or on a shared mutable
record. Run the file in reverse and it must still pass.

Unique data per run (suffix a random or timestamped token) so parallel runs and
reruns do not collide on a unique constraint.

## Configuration and safety

Base URL and auth come from config, fixtures, or env - `https://api.realworld.show/api`,
`Token JWT (Authorization: Token <jwt>)` - and **never** from a hardcoded token or literal URL in a
test file. Run against a non-production target; writes to production are never
allowed. The guardrail linter blocks violations, and a block is a real finding.

## Worked shape

```
Test: CreateArticle_MissingTitle_Returns422

  Arrange   client authenticated from env credentials
            payload = valid article, title removed

  Act       response = client.create_article(payload)

  Assert    response.status == 422                    # documented, exact
            response.body matches ErrorResponse schema
            response.body.errors contains "title"     # the point of this case
```

<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
