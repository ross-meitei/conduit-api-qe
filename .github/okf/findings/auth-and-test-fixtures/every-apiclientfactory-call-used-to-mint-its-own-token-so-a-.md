---
type: finding
title: 'Every ApiClientFactory call used to mint its own token, so a test holding both a typed and a raw client acted as two different registered users - creating a resource as one and fetching it as the other, which 404s because this backend scopes resources to their owner.'
domain: auth-and-test-fixtures
status: NEW
seen_count: 1
first_seen: 2026-07-22
last_seen: 2026-07-22
rule: 'Acquire the session token once (ApiClientFactory.SessionToken) and share it across every client. Use CreateRawClientForToken only when a test deliberately needs a second identity.'
okf_version: "0.1"
---

## Finding

Every ApiClientFactory call used to mint its own token, so a test holding both a typed and a raw client acted as two different registered users - creating a resource as one and fetching it as the other, which 404s because this backend scopes resources to their owner.

## Root cause

Token acquisition sat inside the per-client builder instead of being acquired once per session.

## Rule

> Acquire the session token once (ApiClientFactory.SessionToken) and share it across every client. Use CreateRawClientForToken only when a test deliberately needs a second identity.
