---
name: appqe-test-plan
description: 'Write the test plan to a reviewable markdown file and open it for the user before generating anything. Use at the plan approval gate of any generation run.'
metadata: {version: 1.0.0, access: authoring}
---
# Skill: Test plan document

A plan the user has to reconstruct from chat scrollback is not reviewable. Write
it to a file, open it, and let them read and annotate it like any other
document.

## Write it, then open it

Path: `.appqe/plans/<run_id>-test-plan.md`. The `run_id` ties the plan to the
run that produced it, so a plan is never orphaned from its telemetry.

1. Write the file **before** asking for approval.
2. **Open it in the editor** so it lands in a tab in front of the user - do not
   make them go hunting for a path.
3. Only then ask the approval question, referencing the file by name.

Never generate a single test before this gate clears.

## Structure

```markdown
# Test plan - <scope> (run <run_id>)

## Summary
<n> operations, <n> planned tests. Coverage <before>% -> <after>%
(threshold 80%).

## Scope
| Operation | Happy | Edge | Error | Auth | Total |
|---|---|---|---|---|---|
| POST /articles | 1 | 2 | 3 | 2 | 8 |

## Per operation
### POST /articles
- **Happy** - valid payload -> 201 + Article schema + Location header
- **Error** - missing title -> 422 + ErrorResponse, `errors` names `title`
- **Auth** - unauthenticated -> 401
Assumptions: `tagList` omitted when empty (see discovery log).

## Data and auth
Fixtures needed, how each is cleaned up, which credentials come from env.

## Not covered, and why
The honest list. An operation left out on purpose, with the reason and the
residual risk.

## Open questions
Anything the spec does not settle. These block generation.
```

## Rules

- **Every planned test names its expected status code and schema.** A plan row
  that says "test the endpoint" is not reviewable - the reviewer cannot tell
  whether the assertion will be right.
- **State assumptions explicitly.** Where the spec is ambiguous, write what you
  intend to assume and flag it. Silent assumptions are how a whole suite ends
  up asserting the wrong contract.
- **"Not covered" is mandatory**, even when empty. A plan that lists only what
  it will do hides its gaps.
- Keep it scannable. A reviewer who will not read three pages will approve them
  anyway, which is worse than no gate.

## After review

Apply the user's edits - to the file, not just to your intent - so the approved
document matches what you build. If they changed a case, restate the delta in
one line and re-confirm before generating.

Record the approved plan path in the run so `appqe-run-report` can cite what was
agreed versus what was delivered.

<!-- appqe:managed v0.12.1 | do-not-edit-manually | regenerate: appqe sync -->
