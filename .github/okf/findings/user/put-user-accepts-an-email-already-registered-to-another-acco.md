---
type: finding
title: 'PUT /user accepts an email already registered to another account: returns 200 instead of the documented 422, persists the duplicate, and POST /users/login with that email then resolves to the original owner - the second account is locked out of email login. Verified by direct probe against api.realworld.show, not only by the failing assertion.'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'UpdateCurrentUser_DuplicateEmail_Returns422 in tests/api/UserApiTests.cs is expected to be RED until the API enforces uniqueness on update. Do not weaken it to accept 200 - it is asserting the documented contract. Left red deliberately, approved at the generated-test review gate of run qe-8350452bca95.'
okf_version: "0.1"
---

## Finding

PUT /user accepts an email already registered to another account: returns 200 instead of the documented 422, persists the duplicate, and POST /users/login with that email then resolves to the original owner - the second account is locked out of email login. Verified by direct probe against api.realworld.show, not only by the failing assertion.

## Root cause

The backend does not enforce email uniqueness on update, only on registration. The OpenAPI spec documents 422 for an invalid PUT /user body, so this is product behaviour diverging from the contract.

## Rule

> UpdateCurrentUser_DuplicateEmail_Returns422 in tests/api/UserApiTests.cs is expected to be RED until the API enforces uniqueness on update. Do not weaken it to accept 200 - it is asserting the documented contract. Left red deliberately, approved at the generated-test review gate of run qe-8350452bca95.
