---
type: finding
title: 'The demo API does not enforce documented 422 validation on 4 cases: POST /users duplicate email (returns 201), POST /users missing username (returns 201-equivalent), POST /users/login missing password (returns non-422), PUT /user invalid email format (returns 200). The spec documents 422 for each.'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-08-10
last_seen: 2026-08-10
rule: 'Keep tests asserting documented 422; treat green-on-wrong-code as contract drift, not as a reason to soften assertions'
okf_version: "0.1"
---

## Finding

The demo API does not enforce documented 422 validation on 4 cases: POST /users duplicate email (returns 201), POST /users missing username (returns 201-equivalent), POST /users/login missing password (returns non-422), PUT /user invalid email format (returns 200). The spec documents 422 for each.

## Root cause

Server-side validation rules not implemented for these fields on the demo backend

## Rule

> Keep tests asserting documented 422; treat green-on-wrong-code as contract drift, not as a reason to soften assertions
