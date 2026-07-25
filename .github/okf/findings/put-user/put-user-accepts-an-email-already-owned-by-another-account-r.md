---
type: finding
title: 'PUT /user accepts an email already owned by another account: returns 200 + UserResponse where specs/openapi.json documents 422 + GenericErrorModel'
domain: put-user
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'Do not assert 422 on PUT /user duplicate email against api.realworld.show - the case is quarantined in tests/api/UserApiTests.cs; un-skip only after the product or the spec is corrected'
okf_version: "0.1"
---

## Finding

PUT /user accepts an email already owned by another account: returns 200 + UserResponse where specs/openapi.json documents 422 + GenericErrorModel

## Root cause

The demo backend does not enforce email uniqueness on update, so the documented 422 path is unreachable

## Rule

> Do not assert 422 on PUT /user duplicate email against api.realworld.show - the case is quarantined in tests/api/UserApiTests.cs; un-skip only after the product or the spec is corrected
