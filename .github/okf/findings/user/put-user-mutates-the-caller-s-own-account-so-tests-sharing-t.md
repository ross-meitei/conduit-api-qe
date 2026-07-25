---
type: finding
title: 'PUT /user mutates the caller''s own account, so tests sharing the session token corrupt each other'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'Every /user test must register its own throwaway user (TestDataFactory.NewUser + CreateRawClientForToken), never the shared session client'
okf_version: "0.1"
---

## Finding

PUT /user mutates the caller's own account, so tests sharing the session token corrupt each other

## Root cause

ApiClientFactory holds one session token for the whole run; a PUT /user test would rename or re-email that shared account

## Rule

> Every /user test must register its own throwaway user (TestDataFactory.NewUser + CreateRawClientForToken), never the shared session client
