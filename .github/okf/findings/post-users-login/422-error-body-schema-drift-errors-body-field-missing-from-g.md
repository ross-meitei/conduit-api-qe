---
type: finding
title: '422 error body schema drift: errors.body field missing from GenericErrorModel response'
domain: post-users-login
status: NEW
seen_count: 1
first_seen: 2026-08-10
last_seen: 2026-08-10
rule: 'POST /users/login 422 body does not match GenericErrorModel; waive schema assertion on errors.body or expect test to fail'
okf_version: "0.1"
---

## Finding

422 error body schema drift: errors.body field missing from GenericErrorModel response

## Root cause

API returns 422 with a different error format than documented GenericErrorModel — missing errors.body array

## Rule

> POST /users/login 422 body does not match GenericErrorModel; waive schema assertion on errors.body or expect test to fail
