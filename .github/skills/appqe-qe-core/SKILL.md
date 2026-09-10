---
name: appqe-qe-core
description: 'The QE principles behind appqe''s gates - risk-based testing, the test pyramid, what "covered" means, and why evidence beats assertion. Read it to ground a testing decision in the reasoning the workflow already encodes.'
metadata: {version: 1.0.0, access: read-only}
user-invocable: false
---
# Skill: QE core principles

The rest of appqe's skills tell you *how* to do a step. This one is the *why*
underneath the gates, so a judgement call (is this covered? is this worth a test?
should this pass?) has a principle behind it, not just a rule. It is deliberately
short. A project can override any of it in the `appqe:custom` block at the end.

## Evidence over assertion

The core stance: **"generated" is not "passing", and "it passed" is not "it is
tested".** A claim about quality is only worth what verifies it. This is why a
phase does not complete on the agent's word: it completes on a parsed test report,
a measured coverage number, a clean lint. If you cannot point at the artifact,
you have an opinion, not a result. Never soften an assertion to make a run green;
a failure is a finding about the system, not an obstacle to the run.

## Risk-based testing

Test where the defects actually live, first. Not every operation deserves equal
effort, and coverage in endpoint order wastes it. Rank by risk:

1. **State-mutating operations** (POST/PUT/PATCH/DELETE) - they can corrupt data.
2. **Auth boundaries** - the anonymous and wrong-token cases on a secured operation.
3. **Documented error paths** - a 400/404/409 the spec promises but nothing exercises.
4. **Reads** last - lower blast radius.

A test of the happy path alone proves the feature demoes, not that it holds.

## What "covered" means

An operation is **covered** only with all three of: a happy path, its documented
error cases, and an auth case where it is secured. Anything less is **partial** -
and most defects live in the classes a partial operation skips. A single 200 test
is partial. A quarantined test counts for nothing. Coverage is measured from the
spec and the sources, never a number typed on a command line.

## The test pyramid (where appqe sits)

Most tests should be fast and low: unit at the base, integration/API in the
middle, end-to-end UI at the narrow top (slow, brittle, few). appqe generates the
API and UI layers; it does not chase UI coverage of logic an API test can pin
faster. Performance is a separate, contracted axis (see the perf stack) - it is
not part of the pyramid and is off unless the engagement calls for it.

## Independence and flakiness

Every test sets up its own state and cleans up after itself, so it is
order-independent and parallel-safe. A test that passes and fails intermittently
is not "flaky luck" to be reran away: it is a race, a shared-state leak, or a real
timing bug. Diagnose it (`appqe-flaky-triage`); quarantine only with a tracked
reason, never to make the bar go green.

## Why the gates exist

- **Plan approval** - agree what will be tested, in risk order, before code is written.
- **Self-review** - review against the spec independently, because the author is the
  worst judge of their own coverage.
- **Evidence contracts** - the phase cannot close without proof, so an unrun test can
  never be reported as passing.

The gates are not ceremony; each one guards a specific way a QE run silently lies.

<!-- appqe:custom: qe-core-standards -->
<!-- Project QE standards go here and survive `appqe sync`. Use this to state the
     team's own bar where it differs from the defaults above, e.g.:
       - Definition of done: coverage >= 85% of operations, zero partial writes.
       - Every 5xx path must have a reproduction test, not just a documented code.
       - Contract drift is a release blocker, not a warning.
     Anything between these markers is yours. -->
<!-- /appqe:custom -->

<!-- appqe:managed v0.14.11 | do-not-edit-manually | regenerate: appqe sync -->
