---
type: finding
title: 'On api.realworld.show, POST /profiles/{username}/follow for a user registered moments earlier returns 404 {"errors":{"profile":["not found"]}}. All 4 follow-dependent GET /articles/feed tests failed in Arrange on run qe-1f6d82bfe691; the 401 and empty-feed cases passed.'
domain: profiles-username-follow-articles-feed
status: NEW
seen_count: 1
first_seen: 2026-10-22
last_seen: 2026-10-22
rule: 'Treat a follow-precondition 404 as environment drift, not a test bug: keep the feed tests red, do not weaken or skip them, and raise it. Never count the empty-feed pass as proof the feed filters by follows.'
okf_version: "0.1"
---

## Finding

On api.realworld.show, POST /profiles/{username}/follow for a user registered moments earlier returns 404 {"errors":{"profile":["not found"]}}. All 4 follow-dependent GET /articles/feed tests failed in Arrange on run qe-1f6d82bfe691; the 401 and empty-feed cases passed.

## Root cause

The demo host does not resolve a freshly registered profile for another user's follow (replication/visibility lag or per-tenant isolation).

## Rule

> Treat a follow-precondition 404 as environment drift, not a test bug: keep the feed tests red, do not weaken or skip them, and raise it. Never count the empty-feed pass as proof the feed filters by follows.
