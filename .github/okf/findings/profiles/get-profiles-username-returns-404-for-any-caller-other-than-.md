---
type: finding
title: 'GET /profiles/{username} returns 404 for any caller other than the profile''s owner, and 404 (not the documented 401) when unauthenticated. Verified against api.realworld.show: registering user A and fetching A''s profile with A''s token returns 200; fetching it with B''s token, or anonymously, returns 404.'
domain: profiles
status: NEW
seen_count: 1
first_seen: 2026-07-22
last_seen: 2026-07-22
rule: 'Fetch a profile as its own owner. Do not write cross-user profile or follow tests against this backend - POST /profiles/{username}/follow 404s for every other user, so the follow flow is untestable here (quarantined as CONDUIT-301).'
okf_version: "0.1"
---

## Finding

GET /profiles/{username} returns 404 for any caller other than the profile's owner, and 404 (not the documented 401) when unauthenticated. Verified against api.realworld.show: registering user A and fetching A's profile with A's token returns 200; fetching it with B's token, or anonymously, returns 404.

## Root cause

Backend scopes profile reads to the owner. The spec documents only 200 and 401 for this operation, so both the owner-only rule and the 404-instead-of-401 are undocumented.

## Rule

> Fetch a profile as its own owner. Do not write cross-user profile or follow tests against this backend - POST /profiles/{username}/follow 404s for every other user, so the follow flow is untestable here (quarantined as CONDUIT-301).
