---
type: finding
title: 'The RealWorld demo target (api.realworld.show) does not enforce registration uniqueness: a repeated username+email registration returns 201 again, not the documented 422'
domain: post-users
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'Do not assert 422-on-duplicate against the demo target; the 422 contract is unobservable there. Pin observed 201 behavior or test uniqueness only against an enforcing environment'
okf_version: "0.1"
---

## Finding

The RealWorld demo target (api.realworld.show) does not enforce registration uniqueness: a repeated username+email registration returns 201 again, not the documented 422

## Root cause

Demo target behavior diverges from the spec (which documents 422 for an invalid/taken registration)

## Rule

> Do not assert 422-on-duplicate against the demo target; the 422 contract is unobservable there. Pin observed 201 behavior or test uniqueness only against an enforcing environment
