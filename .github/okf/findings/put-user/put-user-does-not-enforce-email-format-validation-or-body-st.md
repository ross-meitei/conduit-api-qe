---
type: finding
title: 'PUT /user does not enforce email format validation or body structure validation — returns 200 for malformed email and missing user wrapper where spec documents 422'
domain: put-user
status: NEW
seen_count: 1
first_seen: 2026-08-10
last_seen: 2026-08-10
rule: 'Tests asserting 422 for invalid email format and malformed body on PUT /user stay red as contract drift findings. Do not weaken to accept 200.'
okf_version: "0.1"
---

## Finding

PUT /user does not enforce email format validation or body structure validation — returns 200 for malformed email and missing user wrapper where spec documents 422

## Root cause

API is permissive: no server-side validation on email format or request body structure

## Rule

> Tests asserting 422 for invalid email format and malformed body on PUT /user stay red as contract drift findings. Do not weaken to accept 200.
