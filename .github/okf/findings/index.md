---
type: bundle-index
title: appqe QE findings bundle
bundle_updated: 2026-08-10
okf_version: "0.1"
description: Typed cross-run QE findings. Status lifecycle promotes a repeated finding into a permanent rule.
---

# appqe QE findings

> OKF v0.1 - 5 finding(s) across 4 domain(s)

## Status lifecycle

`NEW` (seen once) -> `CONFIRMED` (seen 2+) -> `PROMOTED` (rule made permanent)

### post-users (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [Duplicate email registration returns 201 instead of 422](post-users/duplicate-email-registration-returns-201-instead-of-422.md) | NEW | 1 | Expect POST /users with duplicate email to return 201 until fixed; test is intentionally red |

### post-users-login (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [422 error body schema drift: errors.body field missing from GenericErrorModel response](post-users-login/422-error-body-schema-drift-errors-body-field-missing-from-g.md) | NEW | 1 | POST /users/login 422 body does not match GenericErrorModel; waive schema assertion on errors.body or expect test to fail |

### put-user (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [PUT /user does not enforce email format validation or body structure validation — returns 200 for malformed email and missing user wrapper where spec documents 422](put-user/put-user-does-not-enforce-email-format-validation-or-body-st.md) | NEW | 1 | Tests asserting 422 for invalid email format and malformed body on PUT /user stay red as contract drift findings. Do not weaken to accept 200. |

### user (2)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [3 contract-drift findings: (1) POST /users/login returns 401 not 422 for unknown email, (2) POST /users accepts duplicate emails (returns 201 not 422), (3) GenericErrorModel errors use field-named keys (errors.email, errors.username) not spec errors.body[]](user/3-contract-drift-findings-1-post-users-login-returns-401-not.md) | NEW | 1 | Tests for /user 422 cases must expect contract drift: login unknown-email returns 401 (not 422), registration duplicate-email returns 201 (not 422), and error bodies use errors.<field> keys (not errors.body[]). Assert the spec and leave tests red as findings. |
| [The demo API does not enforce documented 422 validation on 4 cases: POST /users duplicate email (returns 201), POST /users missing username (returns 201-equivalent), POST /users/login missing password (returns non-422), PUT /user invalid email format (returns 200). The spec documents 422 for each.](user/the-demo-api-does-not-enforce-documented-422-validation-on-4.md) | NEW | 1 | Keep tests asserting documented 422; treat green-on-wrong-code as contract drift, not as a reason to soften assertions |

## Promotion candidates

_None. A finding is promotable once seen twice with a rule._
