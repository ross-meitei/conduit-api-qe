---
name: appqe-contract-diff
description: 'Find drift between the spec and the running API, and decide which side is wrong.'
metadata: {version: 1.0.0, access: read-only}
user-invocable: false
---
# Skill: Contract drift

```bash
appqe contract verify                     # probe the API against the spec
appqe contract verify --write-discovery   # and record each finding
appqe lint <test dirs> --strict           # tests asserting undocumented codes
```

Use before a release, after a backend deploy, or when tests fail in a way that
smells like a changed contract rather than a defect.

The sweep finds what is mechanically findable: status codes the spec does not
document, response bodies breaching their published schema, and secured
operations served to an anonymous caller. It is read-only by default; writes
need `--include-writes` and will mutate the target.

## Classification

| Class | Examples | Action |
|---|---|---|
| **Breaking** | Operation removed or renamed; new required parameter; changed response schema; changed status code | Tests will fail or assert stale expectations - update, and confirm the change was intended. |
| **Additive** | New optional field; new operation; new documented error code | No existing test breaks, but coverage now has a hole - add tests. |
| **Cosmetic** | Description, example, or tag changes | No action. |

## The question the tool cannot answer

For every breaking change, ask **which side is wrong**. The sweep reports that
the spec and the implementation disagree; it cannot tell you which one to
change. A spec edited to match a buggy implementation is a documented defect,
not a contract update.

Do not silently update tests to match new behaviour - that erases the evidence
that behaviour changed. Raise it, get a decision, then update, and record the
decision and its date in the discovery log.

<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
