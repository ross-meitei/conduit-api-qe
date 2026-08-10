---
name: appqe-pr-summary
description: 'Produce a clear PR summary for generated/updated tests, including coverage and how to run them.'
metadata: {version: 1.0.0, access: research}
---
# Skill: PR summary

Summarise a QE change for a reviewer who was not present. Lead with risk, not
with volume - "47 tests added" says nothing about whether the release is safe.

## Structure

- **What changed** - operations or flows now covered, cases per class
  (happy/edge/error/auth), files added or modified.
- **Coverage delta** - before -> after, against
  80%. Name what is *still* uncovered.
- **Failures** - separated into **product defect** and **test defect**. These go
  to different people; merging them wastes both their time.
- **How to run** - `dotnet test` (API), `npx playwright test` (UI), plus
  fixture and env setup. Never include a real secret.
- **Guardrails** - confirm explicitly: no hardcoded secrets or production URLs;
  status + schema assertions present; UI web-first with no hard waits.
- **Follow-ups** - known gaps, quarantined tests with their tracked bug.

## Rules

State clearly whether tests were **run** or merely **generated**. A reviewer
will assume passing unless told otherwise, and that assumption is how an
untested change ships.

appqe answers this for you: the run summary's `provenance` map marks each phase
`measured`, `observed` or `agent_asserted`, and `run_tests` cannot complete at
all without a parsed test report newer than the generated code. Quote the label
rather than paraphrasing it.

Keep it short enough to read before the diff. If the summary is longer than the
reviewer's patience, the risks at the bottom will not be read.

<!-- appqe:managed v0.12.1 | do-not-edit-manually | regenerate: appqe sync -->
