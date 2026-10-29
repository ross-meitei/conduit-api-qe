---
type: finding
title: 'The OpenAPI operation defines limit default 20 and offset default 0; the initial plan incorrectly described these defaults as undocumented. A fresh GET /articles/feed happy-path run also returned an unexpected status, with root cause unverified.'
domain: articles-feed
status: NEW
seen_count: 1
first_seen: 2026-10-20
last_seen: 2026-10-20
rule: 'Before drafting endpoint plans, inspect appqe openapi --operation <path> for defaults. Preserve documented status assertions and investigate feed response mismatches without weakening them.'
okf_version: "0.1"
---

## Finding

The OpenAPI operation defines limit default 20 and offset default 0; the initial plan incorrectly described these defaults as undocumented. A fresh GET /articles/feed happy-path run also returned an unexpected status, with root cause unverified.

## Root cause

The high-level operation inventory omitted parameter defaults; the post-generation test run exposed a feed response mismatch without establishing why.

## Rule

> Before drafting endpoint plans, inspect appqe openapi --operation <path> for defaults. Preserve documented status assertions and investigate feed response mismatches without weakening them.
