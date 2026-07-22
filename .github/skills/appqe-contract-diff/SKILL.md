---
name: appqe-contract-diff
description: 'Diff existing tests against the current OpenAPI spec to find contract drift and classify severity.'
metadata: {version: 1.0.0, access: read-only}
---
# Skill: Contract diff

Use in the `contract_check` workflow when the spec may have changed.

## When to use

The `contract_check` workflow, or any time the spec may have moved: before a
release, after a backend deploy, or when tests fail in a way that smells like a
changed contract rather than a defect.

## Procedure

1. Build the current inventory from `specs/openapi.json`.
2. Index existing tests by operation, and by what they assert - status codes,
   required fields, schema shape.
3. Diff and classify every difference.

## Classification

| Class | Examples | Action |
|---|---|---|
| **Breaking** | Operation removed or renamed; new required parameter; changed response schema; changed status code | Tests will fail or assert stale expectations - update, and confirm the change was intended. |
| **Additive** | New optional field; new operation; new documented error code | No existing test breaks, but coverage now has a hole - add tests. |
| **Cosmetic** | Description, example, or tag changes | No action. |

## The question that matters

For every breaking change, ask **which side is wrong**. A spec that changed to
match a buggy implementation is a documented defect, not a contract update.
Do not silently update tests to match new behaviour - that erases the evidence
that behaviour changed. Raise it, get a decision, then update.

Record each breaking change in the discovery log with the decision and its date.

<!-- appqe:managed | do-not-edit-manually -->
