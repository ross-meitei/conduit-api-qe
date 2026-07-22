---
type: finding
title: 'BASE_URL carries a path segment (https://api.realworld.show/api). HttpClient resolved ''users'' and ''/users'' alike against a base without a trailing slash, dropping /api and 404ing every raw call - which is why AuthTokenGenerator could never register a user.'
domain: http-client-setup
status: NEW
seen_count: 1
first_seen: 2026-07-22
last_seen: 2026-07-22
rule: 'Build raw clients from ApiClientFactory.ResolveBaseAddress() (guaranteed trailing slash) and use relative request URIs with no leading slash.'
okf_version: "0.1"
---

## Finding

BASE_URL carries a path segment (https://api.realworld.show/api). HttpClient resolved 'users' and '/users' alike against a base without a trailing slash, dropping /api and 404ing every raw call - which is why AuthTokenGenerator could never register a user.

## Root cause

HttpClient.BaseAddress resolves by URI rules, not string concatenation: the last segment of a base with no trailing slash is treated as a file and replaced.

## Rule

> Build raw clients from ApiClientFactory.ResolveBaseAddress() (guaranteed trailing slash) and use relative request URIs with no leading slash.
