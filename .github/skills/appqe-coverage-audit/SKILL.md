---
name: appqe-coverage-audit
description: 'Measure endpoint coverage, and report the gaps in the order that matters.'
metadata: {version: 1.0.0, access: research}
user-invocable: false
---
# Skill: Coverage audit

```bash
appqe coverage --kind api --detail        # measured, with every gap
appqe coverage --kind api --check-baseline  # CI: fail on regression
```

Coverage is **measured** from the spec and the test sources - it is not a number
you supply. An operation counts as **covered** only with a happy path, the
documented error cases it warrants, and an auth case where it is secured. A
single 200 test is *partial*: most defects live in the paths it does not touch.

The command already attributes each test to an operation by the call it makes
(not the test's name - a test named for one endpoint that calls another is a
coverage lie), skips quarantined tests, and orders gaps by risk: write
operations, then public ones, then auth boundaries, then read-only.

## Your job, once you have the number

**Never report a bare percentage.** A number without the gap list cannot be
acted on. Name the uncovered operations in the order the tool printed them, and
say what risk each gap carries - that judgement is yours, not the tool's.

If coverage is under 80%, that goes in the first
line of the summary, never buried.

On an existing codebase, gate CI on `--check-baseline` rather than the
threshold: an absolute gate is red on day one and gets switched off, which
enforces nothing. Move the baseline deliberately with `--accept-baseline` and
commit the diff.

<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
