---
type: finding
title: 'DELETE /articles/{slug} returns 204 No Content; the spec documents 200. The NSwag-generated client throws ApiException on the typed call because 204 is not in its expected set.'
domain: articles
status: NEW
seen_count: 1
first_seen: 2026-07-22
last_seen: 2026-07-22
rule: 'Delete via a raw HTTP call and assert 204, not the typed client. Do not ''fix'' this by widening the assertion - the spec is the side that needs updating.'
okf_version: "0.1"
---

## Finding

DELETE /articles/{slug} returns 204 No Content; the spec documents 200. The NSwag-generated client throws ApiException on the typed call because 204 is not in its expected set.

## Root cause

Spec/implementation drift on the success code.

## Rule

> Delete via a raw HTTP call and assert 204, not the typed client. Do not 'fix' this by widening the assertion - the spec is the side that needs updating.
