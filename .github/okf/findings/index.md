---
type: bundle-index
title: appqe QE findings bundle
bundle_updated: 2026-10-22
okf_version: "0.1"
description: Typed cross-run QE findings. Status lifecycle promotes a repeated finding into a permanent rule.
---

# appqe QE findings

> OKF v0.1 - 3 finding(s) across 2 domain(s)

## Status lifecycle

`NEW` (seen once) -> `CONFIRMED` (seen 2+) -> `PROMOTED` (rule made permanent)

### articles-feed (1)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [The OpenAPI operation defines limit default 20 and offset default 0; the initial plan incorrectly described these defaults as undocumented. A fresh GET /articles/feed happy-path run also returned an unexpected status, with root cause unverified.](articles-feed/the-openapi-operation-defines-limit-default-20-and-offset-de.md) | NEW | 1 | Before drafting endpoint plans, inspect appqe openapi --operation <path> for defaults. Preserve documented status assertions and investigate feed response mismatches without weakening them. |

### profiles-username-follow-articles-feed (2)

| Finding | Status | Seen | Rule |
|---|---|---|---|
| [On api.realworld.show a user registered at runtime is visible only to itself: other users and anonymous callers get 404 {errors:{profile:[not found]}} on GET /profiles/{username}, POST /profiles/{username}/follow and that user's articles. Reproduced 3/3 runs and by curl in qe-9b897d0f13a3. Spec documents only 200/401 for follow.](profiles-username-follow-articles-feed/on-api-realworld-show-a-user-registered-at-runtime-is-visibl.md) | NEW | 1 | Any test needing reader-follows-author (feed, follow, profile-of-another-user) cannot use two fresh users on this target - either follow a pre-seeded public author or treat it as drift. Do not trust a passing empty-feed test as proof the feed respects follows. |
| [On api.realworld.show, POST /profiles/{username}/follow for a user registered moments earlier returns 404 {"errors":{"profile":["not found"]}}. All 4 follow-dependent GET /articles/feed tests failed in Arrange on run qe-1f6d82bfe691; the 401 and empty-feed cases passed.](profiles-username-follow-articles-feed/on-api-realworld-show-post-profiles-username-follow-for-a-us.md) | NEW | 1 | Treat a follow-precondition 404 as environment drift, not a test bug: keep the feed tests red, do not weaken or skip them, and raise it. Never count the empty-feed pass as proof the feed filters by follows. |

## Promotion candidates

_None. A finding is promotable once seen twice with a rule._
