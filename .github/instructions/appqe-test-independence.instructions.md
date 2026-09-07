---
name: appqe-test-independence
description: 'Applies to all generated API and UI tests in **conduit-api-qe**'
applyTo: '**'
---
# Test Independence & Data Management

> Applies to all generated API and UI tests in **conduit-api-qe**.

**Independence.** Every test must pass in isolation and in any order, in
parallel. No test may depend on another test's side effects or on global mutable
state.

**Data lifecycle.** A test that creates data must clean it up (scoped
fixture/teardown or a unique namespace it can safely leave). Prefer:
- unique, seeded data per test (ids/emails suffixed with a run token),
- API setup over UI setup (seed via the API, assert via the UI),
- factories/fixtures over copy-pasted payloads.

**No shared secrets or sessions baked in.** Auth comes from env/fixtures; UI
reuses an authenticated storage state, never inline credentials.

**Targets.** Tests run against a **non-production** environment
(`https://api.realworld.show/api`). Writes to production are never allowed;
`security.allow_prod` must be explicitly enabled for any prod interaction, and
even then writes stay blocked.

**Idempotence.** Re-running the suite must not accumulate data or drift. If a run
leaves residue, that is a bug in the test, not acceptable noise.

---

**Enforced by** **partly unenforceable.** Hardcoded secrets and production URLs are blocked by `appqe lint`, and unique-per-run data is visible in review - but independence, cleanup and idempotence are not statically checkable. Treat this file as a review checklist, not a gate: the suite running green in a random order is the only real evidence.

<!-- appqe:managed v0.14.8 | do-not-edit-manually | regenerate: appqe sync -->
