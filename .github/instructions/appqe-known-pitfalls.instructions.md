---
name: appqe-known-pitfalls
description: 'Assumption → reality catalogue for API/UI test writing in **conduit-api-qe**. The traps that make a test pass when it should fail. Auto-applied when working on tests.'
applyTo: '**'
---
# Known Pitfalls — Verify, Don't Assume

> Each row is an assumption that produces a test which *passes without proving
> anything*, or fails for the wrong reason. Check the spec (`specs/openapi.json`)
> and, where they disagree, the running API - never guess.

## API

| Assumption | Reality |
|---|---|
| A `2xx` means the test passed | Assert the **documented** status code, not "not 500". A test that accepts any 2xx passes on the wrong one. |
| A successful `POST` returns `200` | Many create endpoints return **`201`** (and some `202`). Read the spec's response for *that* operation. |
| `401` and `403` are interchangeable | `401` = unauthenticated, `403` = authenticated-but-forbidden. Assert the one the spec documents. |
| A `DELETE`/update returns a body | Often **`204 No Content`** - asserting a body fails. Check the documented response. |
| The spec's status codes are complete | The running API may return an **undocumented** code (classic `400` vs `422`). Verify against the live API; if they differ, report the drift - do not silently pick one or weaken the assertion. |
| A field the spec marks optional is always present | Assert only **documented-required** fields; treat optional ones as may-be-absent. |
| A test can reuse one shared, logged-in client | `PUT`/`POST`/`DELETE` **mutate state**. Each test registers its own throwaway data / user so order and parallelism can't couple them. |
| A minted JWT/token can be hardcoded for reuse | Never. Obtain it at runtime (login or env); a token in source is a leak and it expires. |
| The demo/staging API enforces every documented rule | It may not (e.g. a `422` that isn't enforced). A happy-path test written *assuming* enforcement passes trivially - assert the documented behaviour and, if the API doesn't enforce it, that is a **finding**, not a test to soften. |
| Pagination/filtering defaults are obvious | Read the spec: default page size, max, and whether an empty result is `200 []` or `404`. |
| Auth on one operation implies auth on the sibling | Check each operation's security block; `GET` may be open where `PUT` is secured. |

## UI

| Assumption | Reality |
|---|---|
| An element is present the instant the page loads | **Web-first**: wait for the element/state, never a fixed `sleep`/hard-wait. A hard-wait is flaky and slow. |
| `data-testid` is the stable selector | Prefer **role + accessible name** (`role and data-testid`); fall back to `data-testid`. A brittle selector is a false failure waiting to happen. |
| A visible element is interactable | Assert it is enabled/editable before acting; visibility ≠ actionable. |
| One test can depend on the previous test's state | Each E2E test sets up and tears down its own state; a shared session couples them. |

## When a schema/contract error surfaces mid-write — recover

1. **Stop**; do not keep building on a wrong reference.
2. Re-read the operation in `specs/openapi.json` (path, params, request/response
   schema, status codes, security) - or, for UI, re-check the selector.
3. Fix every reference the mistake touched.
4. Resume only once the references are confirmed.

---

**Enforced by** parts of this, not all: `appqe lint` flags a hardcoded token/secret,
a production URL, and (UI) a hard-wait; the phase evidence contract makes `run_tests`
prove a real run. The rest - asserting the documented status code, isolating state,
sourcing values from the spec - is discipline no linter checks, which is exactly why
it is catalogued here.

<!-- appqe:managed v0.12.1 | do-not-edit-manually | regenerate: appqe sync -->
