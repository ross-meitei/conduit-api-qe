---
type: finding
title: 'specs/openapi.json amended from verified evidence: GET /profiles/{username} now documents 404; DELETE /articles/{slug} corrected 200 -> 204; NewArticle.title given maxLength 99; MultipleArticlesResponse now references a new ArticleSummary schema without the required ''body''.'
domain: spec-corrections
status: NEW
seen_count: 1
first_seen: 2026-07-22
last_seen: 2026-07-22
rule: 'The spec now matches observed behaviour for these four; re-run ''appqe contract verify'' after any backend deploy rather than assuming it still does. Do not reverse these to make a test pass - the evidence is recorded here.'
okf_version: "0.1"
---

## Finding

specs/openapi.json amended from verified evidence: GET /profiles/{username} now documents 404; DELETE /articles/{slug} corrected 200 -> 204; NewArticle.title given maxLength 99; MultipleArticlesResponse now references a new ArticleSummary schema without the required 'body'.

## Root cause

Four documentation gaps confirmed against the live API with 'appqe contract verify' and direct probes. In each case the spec was incomplete or wrong, not the implementation - so the spec was the side corrected.

## Rule

> The spec now matches observed behaviour for these four; re-run 'appqe contract verify' after any backend deploy rather than assuming it still does. Do not reverse these to make a test pass - the evidence is recorded here.
