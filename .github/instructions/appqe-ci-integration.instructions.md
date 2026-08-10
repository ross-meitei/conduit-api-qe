---
name: appqe-ci-integration
description: 'How generated tests should run in CI for **conduit-api-qe**'
applyTo: '.github/workflows/**'
---
# CI Integration

> How generated tests should run in CI for **conduit-api-qe**.

- **API:** `dotnet test` produces a machine-readable report (JUnit XML /
  TRX / pytest-junit) the pipeline can surface. Fail the build on any failure.
- **UI:** `npx playwright test` runs **headless**; upload the Playwright HTML
  report + trace on failure as artifacts.
- **Secrets** come from the CI secret store as env vars - never committed, never
  inline in a test. The credential scanner redacts captured output.
- **Target** the non-production base URL from CI config; production is blocked by
  default (`security.allow_prod: false`).
- **Flake policy:** a targeted, tracked quarantine tag - not a blanket retry that
  hides real failures.
- Run the **guardrail linter** (`appqe lint tests`) as a CI step so a hardcoded
  secret, production URL, or UI hard-wait fails the build.
- **Gate coverage on regression, not on the threshold.** `appqe coverage
  --check-baseline` fails when a change covers less than the committed
  `.appqe/coverage-baseline.json`. An absolute threshold is red on day one for
  any existing suite, so it gets switched off and enforces nothing; a ratchet is
  enforceable immediately. Raise the baseline deliberately with
  `appqe coverage --accept-baseline` and commit the diff - the move should be
  reviewed, not silent. Keep the threshold reported alongside, as the target.

---

**Enforced by** nothing automatic - this describes the pipeline you build. `appqe doctor` checks the wiring exists; the steps themselves are yours to add.

<!-- appqe:managed v0.12.1 | do-not-edit-manually | regenerate: appqe sync -->
