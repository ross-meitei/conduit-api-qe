---
type: finding
title: '3 contract-drift findings: (1) POST /users/login returns 401 not 422 for unknown email, (2) POST /users accepts duplicate emails (returns 201 not 422), (3) GenericErrorModel errors use field-named keys (errors.email, errors.username) not spec errors.body[]'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-08-10
last_seen: 2026-08-10
rule: 'Tests for /user 422 cases must expect contract drift: login unknown-email returns 401 (not 422), registration duplicate-email returns 201 (not 422), and error bodies use errors.<field> keys (not errors.body[]). Assert the spec and leave tests red as findings.'
okf_version: "0.1"
---

## Finding

3 contract-drift findings: (1) POST /users/login returns 401 not 422 for unknown email, (2) POST /users accepts duplicate emails (returns 201 not 422), (3) GenericErrorModel errors use field-named keys (errors.email, errors.username) not spec errors.body[]

## Root cause

Demo backend does not enforce email uniqueness at registration, returns 401 instead of 422 for login failures, and uses a different error response shape than spec documents

## Rule

> Tests for /user 422 cases must expect contract drift: login unknown-email returns 401 (not 422), registration duplicate-email returns 201 (not 422), and error bodies use errors.<field> keys (not errors.body[]). Assert the spec and leave tests red as findings.
