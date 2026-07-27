---
type: bundle-index
title: appqe QE findings bundle
bundle_updated: 2026-07-27
okf_version: "0.1"
description: Typed cross-run QE findings. Status lifecycle promotes a repeated finding into a permanent rule.
---

# appqe QE findings

> OKF v0.1 - 1 finding(s) across 1 domain(s)

## Status lifecycle

`NEW` (seen once) -> `CONFIRMED` (seen 2+) -> `PROMOTED` (rule made permanent)

### user (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [GenericErrorModel schema mismatch on 401 responses. Spec declares errors.body as required, but 401 responses from /user GET and PUT endpoints return response body without errors object at all. Affects auth error cases (no-auth, invalid-token, expired-token). Happy path tests (200) pass schema validation.](user/genericerrormodel-schema-mismatch-on-401-responses-spec-decl.md) | NEW | 1 | Auth error cases for /user endpoint should NOT validate response body against GenericErrorModel schema on 401 - the schema is incorrect for this endpoint. Until spec is corrected or API is fixed, waive schema validation on 401 for /user tests, or mark as known contract drift. |

## Promotion candidates

_None. A finding is promotable once seen twice with a rule._
