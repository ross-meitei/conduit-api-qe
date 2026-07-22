---
name: appqe-openapi-parse
description: 'Parse and validate the OpenAPI/Swagger spec and build the endpoint inventory that drives API test generation.'
metadata: {version: 1.0.0, access: read-only}
---
# Skill: Parse the OpenAPI spec

Use when you need the authoritative list of operations for **conduit-api-qe**.

## Procedure

1. **Load** `specs/openapi.json`. If it is a URL, save a pinned local copy so
   the run is reproducible and a spec change mid-run cannot go unnoticed.
2. **Record the envelope**: OpenAPI version, `servers`/`basePath`,
   `securitySchemes`, and any global parameters. These decide base URL and auth
   for every generated test.
3. **Build the inventory** - one row per `paths.<path>.<method>`: operationId,
   tags, parameters (name/in/required/schema), requestBody schema, and every
   documented response code with its schema.
4. **Resolve `$ref`s** before reasoning about a schema. An unresolved ref is the
   most common cause of a test asserting the wrong shape.
5. **Flag spec defects** rather than guessing past them (see below).

`appqe openapi --json` produces this inventory directly; prefer it over
re-parsing the spec by hand.

## Spec defects to flag, never guess

| Defect | Why it matters |
|---|---|
| Operation with no documented error responses | You cannot write the error case without inventing a contract. |
| `200` declared with no schema | Nothing to assert beyond the status. |
| Required field also marked nullable | Ambiguous - ask which is intended. |
| Auth declared globally but absent on an operation | Either a spec bug or a genuine public endpoint; the difference is a security finding. |
| Two operations sharing an operationId | Breaks generated client naming. |

Record each in the discovery log with the decision taken.

## Output

A stable, ordered inventory: one line per operation, `METHOD /path -> codes
[auth] +body`. Deterministic ordering matters - it is diffed between runs to
detect spec drift.

<!-- appqe:managed | do-not-edit-manually -->
