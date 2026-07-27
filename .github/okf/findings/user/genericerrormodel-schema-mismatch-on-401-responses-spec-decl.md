---
type: finding
title: 'GenericErrorModel schema mismatch on 401 responses. Spec declares errors.body as required, but 401 responses from /user GET and PUT endpoints return response body without errors object at all. Affects auth error cases (no-auth, invalid-token, expired-token). Happy path tests (200) pass schema validation.'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-07-27
last_seen: 2026-07-27
rule: 'Auth error cases for /user endpoint should NOT validate response body against GenericErrorModel schema on 401 - the schema is incorrect for this endpoint. Until spec is corrected or API is fixed, waive schema validation on 401 for /user tests, or mark as known contract drift.'
okf_version: "0.1"
---

## Finding

GenericErrorModel schema mismatch on 401 responses. Spec declares errors.body as required, but 401 responses from /user GET and PUT endpoints return response body without errors object at all. Affects auth error cases (no-auth, invalid-token, expired-token). Happy path tests (200) pass schema validation.

## Root cause

API implementation returns bare 401 status without documented error response body. Likely spec was never verified against running API for 401 paths, or API changed without spec update.

## Rule

> Auth error cases for /user endpoint should NOT validate response body against GenericErrorModel schema on 401 - the schema is incorrect for this endpoint. Until spec is corrected or API is fixed, waive schema validation on 401 for /user tests, or mark as known contract drift.
