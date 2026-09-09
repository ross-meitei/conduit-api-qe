---
name: appqe-review-materiality
description: 'What counts as a reportable finding vs cosmetic noise, for every review pass, gate review, and audit in **conduit-api-qe**. Includes the re-review idempotency rule.'
applyTo: '**'
---
# Review Materiality — What Counts as a Finding

> The point of a review - a self-review pass, the generated-test review gate, a
> coverage audit, a PR review - is to surface what makes a test **wrong**: a test
> that passes when it should fail, or fails when it should pass, or hides a real
> defect. It is **not** to enforce a house style or to find something to change on
> every pass. A review that raises cosmetic nits buries the finding that matters.

## The one test to apply before raising anything

> **If I do not raise this, will a test pass when it should fail (or fail when it
> should pass), ship a security leak, or not compile/collect?**

- **Yes** → report it. It is material.
- **No, but it breaks a naming/style/wording convention** → skip it silently.
- **No, but it differs from the template** → skip it. The template is guidance, not
  mandatory syntax.
- **I already raised it on a previous review of this same, unchanged code** → skip
  it. See idempotency below.

## Reportable — raise it

| Criterion | Examples (API / UI) |
|---|---|
| **Functional error** | Asserts an **undocumented** status code; asserts the wrong schema field or type; passes on *any* 2xx instead of the documented code; wrong endpoint/method; a UI assertion racing the render; shared state between tests that should be independent |
| **Contract / requirement violation** | An asserted value not traceable to the spec (`specs/openapi.json`) or a documented acceptance criterion - invented or assumed, not sourced |
| **Missing coverage that matters** | A **secured** operation with no auth/negative-auth case; a **documented** error case with no test; a critical flow with only a happy path |
| **Security** | A hardcoded secret, token, password, or **production** URL in test code or config; a sanctioned target other than `https://api.realworld.show/api` / the demo host |
| **Runtime / blocking** | Won't compile or collect; raw HTTP where the project has a typed client/factory; a missing import or a fixture collision |

## Not reportable — skip silently

| Pattern | Why |
|---|---|
| Variable / constant / test-name wording preference | No effect on correctness |
| Docstring or comment phrasing that is not factually wrong | Not a functional issue |
| Code structure functionally equivalent to the template | The template is guidance, not mandatory syntax |
| Whitespace, blank lines, import ordering | Zero behavioural impact |
| A finding already raised in a **previous review** of this file and addressed | Idempotency |
| A finding on code that has **not changed** since the last review | Re-scanning unchanged code is noise, not value |
| A change that makes code "nicer" but not more correct | Out of scope |

## Idempotency — a re-review must not repeat prior findings

When asked to **review / check / verify** a file reviewed before:

1. **Look at what changed first** - new tests, changed assertions, changed calls.
2. **Nothing changed** → say so plainly: *"No new changes since the last review — no
   findings."* Do not re-scan resolved items.
3. **Something changed** → apply the filter to the **changed portion only**; do not
   re-raise findings on unchanged sections.
4. **Exception - full re-review:** a new spec version, a confirmed contract change,
   or an explicit *"full re-review"* justifies re-scanning the whole file.

> The **first** review of a file must be complete and thorough - catch everything
> above the materiality line in one pass, so later passes are scoped to what
> changed. Front-load the findings; do not dribble them out one nit per turn.

---

Applies to the `self_review` worker, the generated-test review gate, `investigate`
verdicts, and `audit` findings. Full clarity still overrides at gates and on security
findings - materiality filters *reviews*, never the honesty of a gate or a leak.

**Enforced by** the review discipline itself, not a static check - materiality is
judgement the reviewer applies, and there is no linter for "is this finding worth
raising". The **security** line it draws is enforced, though: the pre-tool guard
blocks a hardcoded secret or production URL, `appqe lint` flags weak assertions, and
`appqe coverage --check-baseline` gates the coverage this calls material. The
noise-filtering and idempotency rules are guidance the `self_review` worker and the
gate reviewer follow - nothing checks them for you.

<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
