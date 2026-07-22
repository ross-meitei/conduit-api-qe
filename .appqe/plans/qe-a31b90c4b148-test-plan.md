# Test Plan — `/user` endpoint
**Run:** `qe-a31b90c4b148` | **Generated:** 2026-07-22 | **Stack:** xUnit (C#) / `dotnet test`

## Endpoint inventory

| Method | Path | Operation ID | Auth | Success | Error codes |
|--------|------|-------------|------|---------|-------------|
| GET | `/user` | `GetCurrentUser` | Token JWT (required) | 200 UserResponse | 401, 422 GenericErrorModel |
| PUT | `/user` | `UpdateCurrentUser` | Token JWT (required) | 200 UserResponse | 401, 422 GenericErrorModel |

### Key schemas (from `specs/openapi.json`)

**User** (required fields in response):
- `email` (string, required)
- `token` (string, required)
- `username` (string, required)
- `bio` (string, nullable, required)
- `image` (string, nullable, required)

**UpdateUser** (all fields optional — send only what changes):
- `email?`, `password?`, `username?`, `bio?`, `image?`

**GenericErrorModel**: `{ errors: { body: string[] } }`

### Auth

Global security scheme: `Token JWT` via `Authorization: Token <jwt>`.
Both `/user` operations are protected by the global scheme. The `ApiClientFactory.SessionToken`
shared instance provides the authenticated credential. Unauthenticated cases use
`ApiClientFactory.CreateRawClient(authenticated: false)`.

---

## Test cases

### GET /user

| # | Test name | Type | Asserts |
|---|-----------|------|---------|
| G1 | `GetCurrentUser_Authenticated_Returns200AndValidSchema` | Happy | Status 200; schema matches UserResponse; `email`, `token`, `username` non-empty; `bio` and `image` present (may be null per spec) |
| G2 | `GetCurrentUser_NoAuth_Returns401` | Auth | Status 401 with no auth token |

**Note:** 422 is documented for GET /user but no trigger condition is identified in the spec or the API surface — no test written for it.

---

### PUT /user

| # | Test name | Type | Asserts |
|---|-----------|------|---------|
| U1 | `UpdateCurrentUser_UpdateBio_Returns200WithUpdatedBio` | Happy | Status 200; schema matches UserResponse; `bio` in response equals the sent value |
| U2 | `UpdateCurrentUser_UpdateImage_Returns200WithUpdatedImage` | Happy | Status 200; `image` in response equals the sent URL |
| U3 | `UpdateCurrentUser_UpdateEmail_Returns200WithUpdatedEmail` | Happy (teardown) | Status 200; `email` in response equals sent value; teardown restores original email |
| U4 | `UpdateCurrentUser_UpdateUsername_Returns200WithUpdatedUsername` | Happy (teardown) | Status 200; `username` in response equals sent value; teardown restores original username |
| U5 | `UpdateCurrentUser_EmptyUserObject_Returns200` | Edge | Status 200 when `{ "user": {} }` is sent — all fields are optional per spec |
| U6 | `UpdateCurrentUser_NoAuth_Returns401` | Auth | Status 401 with no auth token |
| U7 | `UpdateCurrentUser_MissingUserWrapper_Returns422` | Error | Status 422; body matches GenericErrorModel — sends `{}` without the required `"user"` key |

**Teardown strategy for U3/U4:** GET /user before the test to capture the current email/username,
run the update, then restore with a second PUT /user. All raw HTTP calls — no typed client
(so status assertions stay exact).

---

## Fixtures and utilities

- `ApiClientFactory.CreateRawClient()` — authenticated raw client (uses `SessionToken`)
- `ApiClientFactory.CreateRawClient(authenticated: false)` — anonymous client
- `SchemaValidator.AssertMatches(body, "UserResponse", context)` — schema validation
- No `TestDataFactory` method needed for user data — generate inline with `Guid.NewGuid()`-seeded values

---

## Coverage check

| Endpoint | Happy | Edge | Error | Auth | Total |
|----------|-------|------|-------|------|-------|
| GET /user | G1 | — | — | G2 | 2 |
| PUT /user | U1, U2, U3, U4 | U5 | U7 | U6 | 6 |
| **Total** | **5** | **1** | **1** | **2** | **8** |

8 tests across 2 operations — 100% of documented operations covered.
