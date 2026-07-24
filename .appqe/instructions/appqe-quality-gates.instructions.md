---
name: appqe-quality-gates
description: 'Applies to generation and audit workflows for **conduit-api-qe**'
---
# Quality Gates

> Applies to generation and audit workflows for **conduit-api-qe**.

**Self-review is a loop, not a glance.** Review generated tests at least
**2** passes before reporting done: pass 1 -
correctness vs spec (status codes, schema, auth); pass 2 - standards
(independence, no secrets/prod URLs, web-first UI assertions); further passes
until clean.

**Coverage thresholds.** An audit/generation run should meet:
- API: **80%** of operations have a test (happy +
  the negative cases each warrants).
- UI: **70%** of critical flows have an E2E test.

Check it in CI: `appqe coverage --kind api --covered N --total M` exits non-zero
below threshold. Do not count "not 500"-only or schema-skipping tests as coverage.

**Flakiness.** Classify a failing test by re-running it
**3 times**: consistent fail = real defect; mixed = flaky. Fix the
cause (timing/isolation/data) - a blanket retry masks real bugs. Quarantine a
diagnosed flaky test only with a tracked bug.

**Contract drift severity.** breaking (removed/renamed op, new required param,
changed schema/status, added auth) -> must fix; additive (new optional field /
endpoint) -> add coverage; cosmetic -> no action.

---

**Enforced by** `appqe coverage --check-baseline` (regression gate), `appqe lint --strict`, and the phase evidence contracts - `run_tests` cannot complete without a parsed test report newer than the generated code.

<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
