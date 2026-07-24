---
type: finding
title: 'GET /user and PUT /user have zero tests; POST /users and POST /users/login are called only as unasserted fixtures (ProfilesApiTests.RegisterUserAsync, utils/AuthTokenGenerator.cs). UpdateUser has no required fields, so partial update is the default caller behaviour.'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'PUT /user tests must register a throwaway user per test - PUT mutates the session user, so a shared fixture lets the email/username/password cases contaminate each other. Do not count POST /users fixture calls as coverage.'
okf_version: "0.1"
---

## Finding

GET /user and PUT /user have zero tests; POST /users and POST /users/login are called only as unasserted fixtures (ProfilesApiTests.RegisterUserAsync, utils/AuthTokenGenerator.cs). UpdateUser has no required fields, so partial update is the default caller behaviour.

## Root cause

The suite grew around Articles/Comments/Profiles/Tags; the user/auth surface was only ever consumed as a fixture, never tested in its own right.

## Rule

> PUT /user tests must register a throwaway user per test - PUT mutates the session user, so a shared fixture lets the email/username/password cases contaminate each other. Do not count POST /users fixture calls as coverage.
