---
name: appqe-openapi-parse
description: 'Build the endpoint inventory that drives API test generation, and flag the spec defects a parser cannot decide.'
metadata: {version: 1.0.0, access: read-only}
user-invocable: false
---
# Skill: The endpoint inventory

```bash
appqe openapi --json     # the full inventory
appqe openapi            # one line per operation
```

That is the whole procedure. It loads `specs/openapi.yaml`, resolves `$ref`s,
and emits one row per operation - operationId, tags, parameters, request schema,
every documented response with its schema, and whether auth is required
(`[auth]`) or optional (`[auth?]`). Ordering is deterministic so two runs diff
cleanly. Do not re-parse the spec by hand: a hand-built inventory is the most
common source of a test asserting the wrong shape.

## What the command cannot decide

These need a person, and are the reason to read the spec rather than only its
inventory. Record each in the discovery log with the decision taken.

| Defect | Why it matters |
|---|---|
| Operation with no documented error responses | You cannot write the error case without inventing a contract. |
| `200` declared with no schema | Nothing to assert beyond the status. |
| Required field also marked nullable | Ambiguous - ask which is intended. |
| Auth declared globally but absent on an operation | Either a spec bug or a genuine public endpoint; the difference is a security finding. |
| Two operations sharing an operationId | Breaks generated client naming. |

If the spec is only reachable by URL, fetch it to a local file and point
`api.openapi_path` at that copy - a spec that can change mid-run makes the run
unreproducible.

<!-- appqe:managed v0.14.11 | do-not-edit-manually | regenerate: appqe sync -->
