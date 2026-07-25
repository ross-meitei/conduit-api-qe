---
type: bundle-index
title: appqe QE findings bundle
bundle_updated: 2026-07-24
okf_version: "0.1"
description: Typed cross-run QE findings. Status lifecycle promotes a repeated finding into a permanent rule.
---

# appqe QE findings

> OKF v0.1 - 8 finding(s) across 4 domain(s)

## Status lifecycle

`NEW` (seen once) -> `CONFIRMED` (seen 2+) -> `PROMOTED` (rule made permanent)

### post-users (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [The RealWorld demo target (api.realworld.show) does not enforce registration uniqueness: a repeated username+email registration returns 201 again, not the documented 422](post-users/the-realworld-demo-target-api-realworld-show-does-not-enforc.md) | NEW | 1 | Do not assert 422-on-duplicate against the demo target; the 422 contract is unobservable there. Pin observed 201 behavior or test uniqueness only against an enforcing environment |

### post-users-post-users-login (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [422 validation bodies are field-keyed: {"errors":{"<field>":["can't be blank"]}} (e.g. errors.username / errors.email / errors.password)](post-users-post-users-login/422-validation-bodies-are-field-keyed-errors-field-can-t-be-.md) | NEW | 1 | For /users 422 cases assert the field-keyed error shape (errors.<field> is a non-empty string[]), not the GenericErrorModel schema; verified by direct curl |

### put-user (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [PUT /user accepts an email already owned by another account: returns 200 + UserResponse where specs/openapi.json documents 422 + GenericErrorModel](put-user/put-user-accepts-an-email-already-owned-by-another-account-r.md) | NEW | 1 | Do not assert 422 on PUT /user duplicate email against api.realworld.show - the case is quarantined in tests/api/UserApiTests.cs; un-skip only after the product or the spec is corrected |

### user (5)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [GET /user and PUT /user have zero tests; POST /users and POST /users/login are called only as unasserted fixtures (ProfilesApiTests.RegisterUserAsync, utils/AuthTokenGenerator.cs). UpdateUser has no required fields, so partial update is the default caller behaviour.](user/get-user-and-put-user-have-zero-tests-post-users-and-post-us.md) | NEW | 1 | PUT /user tests must register a throwaway user per test - PUT mutates the session user, so a shared fixture lets the email/username/password cases contaminate each other. Do not count POST /users fixture calls as coverage. |
| [PUT /user accepts an email already registered to another account: returns 200 instead of the documented 422, persists the duplicate, and POST /users/login with that email then resolves to the original owner - the second account is locked out of email login. Verified by direct probe against api.realworld.show, not only by the failing assertion.](user/put-user-accepts-an-email-already-registered-to-another-acco.md) | NEW | 1 | UpdateCurrentUser_DuplicateEmail_Returns422 in tests/api/UserApiTests.cs is expected to be RED until the API enforces uniqueness on update. Do not weaken it to accept 200 - it is asserting the documented contract. Left red deliberately, approved at the generated-test review gate of run qe-8350452bca95. |
| [PUT /user mutates the caller's own account, so tests sharing the session token corrupt each other](user/put-user-mutates-the-caller-s-own-account-so-tests-sharing-t.md) | NEW | 1 | Every /user test must register its own throwaway user (TestDataFactory.NewUser + CreateRawClientForToken), never the shared session client |
| [QEBS-81 retested 2026-07-24 (run qe-7cee40f932a4): NOT FIXED, still 200 instead of 422. A control run showed POST /users with an already-registered email also returns 201, so api.realworld.show enforces email uniqueness on neither the create nor the update path - QEBS-81's original claim that registration enforces it was wrong and is corrected in comment 353341.](user/qebs-81-retested-2026-07-24-run-qe-7cee40f932a4-not-fixed-st.md) | NEW | 1 | Before asserting a uniqueness contract on this target, run the control on the create path first - the demo backend enforces uniqueness nowhere, so a 422-on-duplicate expectation will fail on every endpoint, not just PUT /user. Retest QEBS-81 with the bug-retest workflow; do not re-diagnose it. |
| [The duplicate-email defect on PUT /user is tracked as QEBS-81 (https://slalom.atlassian.net/browse/QEBS-81), filed 2026-07-24.](user/the-duplicate-email-defect-on-put-user-is-tracked-as-qebs-81.md) | NEW | 1 | Reference QEBS-81 when UpdateCurrentUser_DuplicateEmail_Returns422 fails - it is the tracked defect, not a new one. Retest it with the bug-retest workflow rather than re-diagnosing. |

## Promotion candidates

_None. A finding is promotable once seen twice with a rule._
