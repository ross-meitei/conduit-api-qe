---
type: finding
title: 'Email validation not enforced by the API - PUT /user with invalid email returns 200 instead of documented 422'
domain: user-endpoints
status: NEW
seen_count: 1
first_seen: 2026-07-26
last_seen: 2026-07-26
rule: 'When testing /user PUT endpoint updates, test valid updates and auth cases; do not assume 422 for invalid email formats without verifying against the running API'
okf_version: "0.1"
---

## Finding

Email validation not enforced by the API - PUT /user with invalid email returns 200 instead of documented 422

## Root cause

The API does not perform email format validation on the /user PUT endpoint

## Rule

> When testing /user PUT endpoint updates, test valid updates and auth cases; do not assume 422 for invalid email formats without verifying against the running API
