---
type: bundle-index
title: appqe QE findings bundle
bundle_updated: 2026-07-27
okf_version: "0.1"
description: Typed cross-run QE findings. Status lifecycle promotes a repeated finding into a permanent rule.
---

# appqe QE findings

> OKF v0.1 - 2 finding(s) across 2 domain(s)

## Status lifecycle

`NEW` (seen once) -> `CONFIRMED` (seen 2+) -> `PROMOTED` (rule made permanent)

### user (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [6 test cases generated for GET and PUT /user; 3 cases per operation covering happy path, missing-auth (401), invalid-auth (401) scenarios. Core test structure working: Arrange-Act-Assert pattern, raw HttpClient assertions, schema validation. Auth tests require managed session from ApiClientFactory.CreateRawClient(authenticated: false). Error case (422) deferred to phase 2.](user/6-test-cases-generated-for-get-and-put-user-3-cases-per-oper.md) | NEW | 1 | On /user endpoint follow-up: extend PUT /user with 422 cases (invalid email, missing required fields). Validate auth token through ApiClientFactory session injection — do not hardcode tokens. |

### user-endpoints (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [Email validation not enforced by the API - PUT /user with invalid email returns 200 instead of documented 422](user-endpoints/email-validation-not-enforced-by-the-api-put-user-with-inval.md) | NEW | 1 | When testing /user PUT endpoint updates, test valid updates and auth cases; do not assume 422 for invalid email formats without verifying against the running API |

## Promotion candidates

_None. A finding is promotable once seen twice with a rule._
