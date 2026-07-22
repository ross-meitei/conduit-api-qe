---
type: finding
title: 'POST /articles rejects a title of 100 characters or more with 422 {"errors":{"body":["title is a string of less than 100 chars"]}}. The spec declares no maxLength on Article.title.'
domain: articles
status: NEW
seen_count: 1
first_seen: 2026-07-22
last_seen: 2026-07-22
rule: 'Keep generated titles under 100 chars (TestDataFactory.MaxTitleLength), truncating the phrase and never the uniqueness suffix. Spec gap raised as CONDUIT-303.'
okf_version: "0.1"
---

## Finding

POST /articles rejects a title of 100 characters or more with 422 {"errors":{"body":["title is a string of less than 100 chars"]}}. The spec declares no maxLength on Article.title.

## Root cause

Undocumented API constraint. Bogus Hacker.Phrase() plus a uniqueness suffix routinely exceeded it, so every article-creating test failed on test data rather than on behaviour.

## Rule

> Keep generated titles under 100 chars (TestDataFactory.MaxTitleLength), truncating the phrase and never the uniqueness suffix. Spec gap raised as CONDUIT-303.
