# DRAFT — not posted to QEBS

**PUT /user accepts an email already registered to another account and locks that account out of email login**

| | |
|---|---|
| Severity | High |
| Component | User and Authentication — `PUT /user` (`UpdateCurrentUser`) |
| Environment | `https://api.realworld.show/api` |
| Found | 2026-07-24, appqe run `qe-8350452bca95` |
| Spec | `specs/openapi.json` — `PUT /user` documents `200`, `401`, `422` |
| Linked test | `tests/api/UserApiTests.cs` → `UpdateCurrentUser_DuplicateEmail_Returns422` (currently red, deliberately) |

## Summary

`PUT /user` does not enforce email uniqueness. A user can set their email to one
already owned by another account: the API answers **200** instead of the
documented **422**, and the duplicate is persisted. `POST /users/login` with that
email then resolves to the **original** owner, so the account that changed its
email can no longer log in by email at all.

Uniqueness *is* enforced on `POST /users` at registration — only the update path
is unchecked.

## Steps to reproduce

```bash
BASE=https://api.realworld.show/api

# 1. Register user A
curl -s -X POST "$BASE/users" -H 'Content-Type: application/json' \
  -d '{"user":{"username":"probea","email":"probea@example.com","password":"Passw0rd123"}}'
# -> 201

# 2. Register user B, keep B's token
curl -s -X POST "$BASE/users" -H 'Content-Type: application/json' \
  -d '{"user":{"username":"probeb","email":"probeb@example.com","password":"Passw0rd123"}}'
# -> 201

# 3. As B, claim A's email
curl -s -X PUT "$BASE/user" -H "Authorization: Token <B_TOKEN>" \
  -H 'Content-Type: application/json' \
  -d '{"user":{"email":"probea@example.com"}}'
# -> 200   (expected 422)

# 4. Read B back
curl -s "$BASE/user" -H "Authorization: Token <B_TOKEN>"
# -> 200, {"user":{"username":"probeb","email":"probea@example.com",...}}

# 5. Log in with that email and A's password
curl -s -X POST "$BASE/users/login" -H 'Content-Type: application/json' \
  -d '{"user":{"email":"probea@example.com","password":"Passw0rd123"}}'
# -> 200, resolves to username "probea"
```

## Expected

`422 Unprocessable Entity` with a `GenericErrorModel` body naming `email`, and
the caller's email left unchanged — matching both the documented responses for
`PUT /user` and the uniqueness already enforced by `POST /users`.

## Actual

`200 OK` with a `UserResponse` carrying the duplicated email. The value is
persisted (confirmed by a follow-up `GET /user`). Two accounts then share one
email address, and login by that email resolves to whichever account the backend
picks — in this reproduction, always the original owner.

Observed with usernames `probea26023` / `probeb26023` on 2026-07-24.

## Impact

- **Account lockout.** The user who changed their email is no longer reachable by
  email login. Their token still works until it expires; after that there is no
  documented way back in.
- **Ambiguous identity.** Any flow that treats email as a unique key — login,
  password reset, account lookup — now has two candidate accounts.
- **Silent.** The API returns 200, so a client has no signal that anything went
  wrong and no reason to surface an error to the user.

## Test coverage

`UpdateCurrentUser_DuplicateEmail_Returns422` asserts the documented 422 and is
**expected to fail** until this is fixed. It was left red on purpose rather than
weakened to accept 200 — approved at the generated-test review gate of run
`qe-8350452bca95` and recorded in the discovery log under area `/user`. The test
passing is the fix's acceptance criterion.

## Suggested fix

Apply the same uniqueness check the registration path already uses to the update
path, and return `422` with `errors.body` naming `email` when it fails. Worth
checking `username` on the same code path — this report does not cover it, and it
may have the identical gap.
