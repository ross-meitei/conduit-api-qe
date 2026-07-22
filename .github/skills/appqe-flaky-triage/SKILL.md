---
name: appqe-flaky-triage
description: 'Diagnose and fix flaky API/UI tests - distinguish real defects from timing/data/isolation flakiness.'
metadata: {version: 1.0.0, access: research}
---
# Skill: Flaky-test triage

A test that passes and fails without a code change is **flaky**. Flakiness is a
defect in the test, or a real race in the product - never noise to be retried
away.

## Diagnose before you touch anything

Re-run the suspect test in isolation **3 times**, then in
the full suite the same number of times.
The difference tells you the class immediately:

| Isolated | In suite | Class |
|---|---|---|
| Passes | Fails | **Isolation** - shared state or order dependence. |
| Fails | Fails | **Timing or data** - race, or drifting fixture. |
| Passes | Passes | **Environment** - rate limit, cold start, external dependency. |

## Fixes by class

- **Isolation** - each test creates and cleans up its own data in a scoped
  fixture. Remove shared mutable state; never depend on execution order.
- **Timing** - replace every hard wait with a web-first auto-waiting assertion.
  A `waitForTimeout` is a race with a timer taped over it.
- **Data** - unique, seeded data per test (suffix a random or timestamped
  token) so parallel runs cannot collide.
- **Environment** - retry with backoff *only* for known-transient infra, and
  mock third parties. Never retry to hide a product race.

## Quarantine

Quarantine only with a tracked bug (CONDUIT), an owner, and a
date. A quarantined test with no bug is a deleted test with extra steps.

Record the cause and the fix in the discovery log - flaky causes recur, and the
next run should not re-diagnose one you have already solved.

<!-- appqe:managed | do-not-edit-manually -->
