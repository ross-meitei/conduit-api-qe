---
type: finding
title: 'GET /articles omits the required ''body'' field from each item, while GET /articles/{slug} includes it. Schema validation against components.schemas.Article fails with ArrayItemNotValid on every list item.'
domain: articles
status: PROMOTED
seen_count: 1
first_seen: 2026-07-22
last_seen: 2026-07-22
rule: 'Validate GET /articles with ''body'' waived (SchemaValidator waiveRequired) and keep the single-article endpoint on the full schema. Raised as CONDUIT-302.'
okf_version: "0.1"
---

## Finding

GET /articles omits the required 'body' field from each item, while GET /articles/{slug} includes it. Schema validation against components.schemas.Article fails with ArrayItemNotValid on every list item.

## Root cause

The list endpoint returns a summary projection, but the spec reuses the full Article schema (body required) for MultipleArticlesResponse. The spec is wrong: a collection returning a summary needs its own schema.

## Rule

> Validate GET /articles with 'body' waived (SchemaValidator waiveRequired) and keep the single-article endpoint on the full schema. Raised as CONDUIT-302.
