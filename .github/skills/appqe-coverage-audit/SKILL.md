---
name: appqe-coverage-audit
description: 'Measure test coverage against the spec/app - which endpoints and flows (and negative paths) are untested.'
metadata: {version: 1.0.0, access: research}
---
# Skill: Coverage audit

## What counts as covered

An operation is **covered** only when it has a happy path *and* the negative
cases it warrants. A single 200 test is **partial**, not covered - most defects
live in the error paths.

| State | Meaning |
|---|---|
| Covered | Happy + documented error codes + auth cases where applicable. |
| Partial | Happy path only, or missing the auth case on a protected operation. |
| Uncovered | No test exercises this operation at all. |

## Procedure

1. Build the endpoint inventory (`appqe-openapi-parse`).
2. Index existing tests by the operation they exercise - path + method, not test
   file name. A test named for an endpoint that actually calls another is a
   coverage lie; verify by the call, not the name.
3. Classify each operation covered / partial / uncovered.
4. Run `appqe coverage --covered N --total M` to check the
   80% threshold.

## Prioritise the gaps

Report gaps in risk order, not spec order:

1. **Write operations** (POST/PUT/PATCH/DELETE) - they change state.
2. **Unauthenticated or public** operations - reachable by anyone.
3. **Auth and permission** boundaries - the 401/403 cases.
4. Read-only operations on non-sensitive data.

## Reporting

Never report a bare percentage. A number without the gap list cannot be acted
on. Name the uncovered operations, in priority order, and say what risk each
gap carries. If coverage is under threshold, that goes in the first line.

<!-- appqe:managed | do-not-edit-manually -->
