---
name: appqe-quality-checklist
description: 'The pre-flight to run before reporting a generation or audit run done for **conduit-api-qe** - one scannable list, not prose. If any item is unchecked, the run is not done.'
applyTo: '**'
---
# Quality Checklist — Before You Report Done

> Run this list before the generated-test review gate and before "run complete".
> It is the standards - assertion, independence, zero-assumptions - reduced to a
> yes/no pass. **An unchecked box is not a nit - it is a run that is not finished.**

## Every generated test

- [ ] Calls the **typed client**, never raw HTTP.
- [ ] Asserts the **documented status code** for that operation - not "not 500", not any `2xx`.
- [ ] Asserts the **response schema** - the required fields and their types - not just the code.
- [ ] Covers the operation's **documented error cases**, not only the happy path.
- [ ] Has an **auth case** (missing/invalid token) on every **secured** operation.
- [ ] Is **independent** - its own data/user, no shared session state, order-agnostic.
- [ ] Contains **no** hardcoded secret, token, password, or production URL. Only
      `https://api.realworld.show/api` / the demo host; creds from env or a factory.
- [ ] (UI) Web-first waits, no hard-waits; selectors by `role and data-testid`.
- [ ] Every asserted value traces to the spec (`specs/openapi.yaml`) or a
      documented acceptance criterion - nothing invented.

## The run

- [ ] The generated tests **actually ran** - a parsed report **newer** than the code
      (the `run_tests` evidence contract), not an asserted "should pass".
- [ ] Failures are **triaged** - real defect vs flaky (3 reruns) -
      not blanket-retried away. A deliberately-red test stays red.
- [ ] **Coverage measured** (`appqe coverage`), meets `80%`,
      gaps reported in **risk order** - not asserted, not a count of files.
- [ ] Built **exactly the approved plan** - no cases added or dropped without saying so.
- [ ] The status block and task panel match the real run state; findings sourced and cited.

## If a box can't be checked

Say which, and why, in the run report - do not report done with an honest gap
hidden. A run that skipped coverage or never proved its tests ran is **partial**;
report it as partial. See `appqe-review-materiality` for what, among the gaps, is
worth raising.

---

**Enforced by** the pieces that can be: the `run_tests` and `coverage` evidence
contracts refuse an unproven claim, `appqe lint` catches secrets/prod-URLs/hard-waits,
and `appqe coverage --check-baseline` gates the threshold. The independence,
error-case, and schema-assertion boxes are discipline - `appqe lint --strict` flags
the weakest of them, the rest are yours to hold.

<!-- appqe:managed v0.14.11 | do-not-edit-manually | regenerate: appqe sync -->
