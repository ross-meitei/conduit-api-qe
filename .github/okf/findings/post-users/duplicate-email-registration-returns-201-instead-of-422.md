---
type: finding
title: 'Duplicate email registration returns 201 instead of 422'
domain: post-users
status: NEW
seen_count: 1
first_seen: 2026-08-10
last_seen: 2026-08-10
rule: 'Expect POST /users with duplicate email to return 201 until fixed; test is intentionally red'
okf_version: "0.1"
---

## Finding

Duplicate email registration returns 201 instead of 422

## Root cause

API does not enforce unique email on POST /users — same class as QEBS-81 on PUT /user

## Rule

> Expect POST /users with duplicate email to return 201 until fixed; test is intentionally red
