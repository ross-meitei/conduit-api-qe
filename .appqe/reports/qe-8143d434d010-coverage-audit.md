# Coverage audit — `/user` surface

**Run** `qe-8143d434d010` · conduit-api-qe · source of truth `specs/openapi.json`
**Verdict: 0% covered against the 80% threshold.** Neither `GET /user` nor
`PUT /user` has a single test.

## Scope

| Method | Path | Secured | Documented responses | Request schema | Response schema |
|---|---|---|---|---|---|
| GET | `/user` | yes (Token JWT) | 200, 401, 422 | — | `UserResponse` |
| PUT | `/user` | yes (Token JWT) | 200, 401, 422 | `UpdateUserRequest` | `UserResponse` |

Support operations pulled in by the scope (unsecured):

| Method | Path | Current status |
|---|---|---|
| POST | `/users` | used as a throwaway-registration fixture in `ProfilesApiTests.RegisterUserAsync` — no assertions on 201 or schema |
| POST | `/users/login` | used only inside `utils/AuthTokenGenerator.cs` — never asserted |

Schema facts that drive the cases:

- `User` **requires** `email`, `token`, `username`, `bio`, `image`; `bio` and
  `image` are `nullable: true` — present-but-null is valid, absent is not.
- `UpdateUser` has **no required fields** — every one of `email`, `password`,
  `username`, `bio`, `image` is optional, so partial update is the normal case.
- `GenericErrorModel` requires `errors.body` as an array of strings.

## Existing suite

26 tests across 4 files — `ArticlesApiTests.cs`, `CommentsApiTests.cs`,
`ProfilesApiTests.cs`, `TagsApiTests.cs`. **None** reference `GET /user` or
`PUT /user`. There is no `UserApiTests.cs`.

| Operation | Tests | Happy | Edge | Error | Auth |
|---|---|---|---|---|---|
| `GET /user` | 0 | ✗ | ✗ | ✗ | ✗ |
| `PUT /user` | 0 | ✗ | ✗ | ✗ | ✗ |
| `POST /users` | 0 asserted | ✗ | ✗ | ✗ | n/a |
| `POST /users/login` | 0 asserted | ✗ | ✗ | ✗ | n/a |

The fixture use of `POST /users` is deliberately **not** counted as coverage: it
calls the endpoint but asserts nothing about status code or response shape, so a
regression there surfaces as an unrelated Profiles failure, not as a user-API
failure.

## Prioritized gaps

### P0 — write these first (secured write path + auth boundary)

| # | Operation | Case | Type | Why first |
|---|---|---|---|---|
| 1 | `PUT /user` | 200 single-field update (`bio`) persists — confirm via a follow-up `GET /user` | happy | the only write on the surface; nothing verifies a write lands |
| 2 | `PUT /user` | partial update does not clobber unspecified fields | edge | `UpdateUser` is all-optional, so this is the default caller behaviour and the highest-value silent-data-loss bug |
| 3 | `PUT /user` | 422 duplicate email/username returns `GenericErrorModel` with `errors.body` | error | the one documented business error on the write path |
| 4 | `PUT /user` | 401 with no `Authorization` header | auth | write endpoint reachable unauthenticated is the worst outcome here |
| 5 | `GET /user` | 200 + full `UserResponse` schema validation | happy | every other test depends on the token this returns |
| 6 | `GET /user` | 401 with no `Authorization` header | auth | documented, cheap, currently unverified |

### P1 — next

| # | Operation | Case | Type |
|---|---|---|---|
| 7 | `PUT /user` | email + username round-trip returns the updated values | happy |
| 8 | `PUT /user` | password change, then `POST /users/login` with the new password succeeds | edge |
| 9 | `PUT /user` | 401 with a malformed token | auth |
| 10 | `GET /user` | 401 with a malformed/expired token | auth |
| 11 | `GET /user` | the returned `token` is usable on a subsequent secured call | edge |
| 12 | `POST /users` | 201 + `UserResponse`, and 422 on duplicate email and on missing required fields | error |
| 13 | `POST /users/login` | 200 + token, 401 on wrong password, 422 on missing fields | error |

### P2 — spec questions, not yet tests

| # | Operation | Question |
|---|---|---|
| 14 | `PUT /user` | empty `{"user": {}}` body — the schema permits it; behaviour is undocumented. Confirm against the live API before asserting anything. |
| 15 | `GET /user`, `PUT /user` | both document **422** with no named trigger. `GET /user` takes no request body, so a 422 has no obvious cause — likely spec noise, but confirm before writing a case for it. |

## Recommended next step

Generate the 6 P0 cases into `tests/api/UserApiTests.cs` (xUnit, `dotnet test`),
matching the conventions in `ProfilesApiTests.cs`: `[Fact(DisplayName = ...)]`,
Allure descriptions, `SchemaValidator.AssertMatches` against `UserResponse`, and
a throwaway user registered per test so cases stay self-contained. That alone
takes the surface from 0% to both operations covered on happy + auth, with the
write path's key edge and error cases included.

Fixture note for whoever writes them: `PUT /user` mutates the session user, so
each test must register its own throwaway user first rather than sharing one —
otherwise the email/username/password cases contaminate each other.
