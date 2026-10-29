---
type: finding
title: 'On api.realworld.show a user registered at runtime is visible only to itself: other users and anonymous callers get 404 {errors:{profile:[not found]}} on GET /profiles/{username}, POST /profiles/{username}/follow and that user''s articles. Reproduced 3/3 runs and by curl in qe-9b897d0f13a3. Spec documents only 200/401 for follow.'
domain: profiles-username-follow-articles-feed
status: NEW
seen_count: 1
first_seen: 2026-10-22
last_seen: 2026-10-22
rule: 'Any test needing reader-follows-author (feed, follow, profile-of-another-user) cannot use two fresh users on this target - either follow a pre-seeded public author or treat it as drift. Do not trust a passing empty-feed test as proof the feed respects follows.'
okf_version: "0.1"
---

## Finding

On api.realworld.show a user registered at runtime is visible only to itself: other users and anonymous callers get 404 {errors:{profile:[not found]}} on GET /profiles/{username}, POST /profiles/{username}/follow and that user's articles. Reproduced 3/3 runs and by curl in qe-9b897d0f13a3. Spec documents only 200/401 for follow.

## Root cause

Demo host isolates data per account (seeded users like johndoe are public). Undocumented 404 = contract drift.

## Rule

> Any test needing reader-follows-author (feed, follow, profile-of-another-user) cannot use two fresh users on this target - either follow a pre-seeded public author or treat it as drift. Do not trust a passing empty-feed test as proof the feed respects follows.
