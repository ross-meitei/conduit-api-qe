---
type: finding
title: 'QEBS-81 retested 2026-07-24 (run qe-7cee40f932a4): NOT FIXED, still 200 instead of 422. A control run showed POST /users with an already-registered email also returns 201, so api.realworld.show enforces email uniqueness on neither the create nor the update path - QEBS-81''s original claim that registration enforces it was wrong and is corrected in comment 353341.'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'Before asserting a uniqueness contract on this target, run the control on the create path first - the demo backend enforces uniqueness nowhere, so a 422-on-duplicate expectation will fail on every endpoint, not just PUT /user. Retest QEBS-81 with the bug-retest workflow; do not re-diagnose it.'
okf_version: "0.1"
---

## Finding

QEBS-81 retested 2026-07-24 (run qe-7cee40f932a4): NOT FIXED, still 200 instead of 422. A control run showed POST /users with an already-registered email also returns 201, so api.realworld.show enforces email uniqueness on neither the create nor the update path - QEBS-81's original claim that registration enforces it was wrong and is corrected in comment 353341.

## Root cause

The demo backend has no uniqueness constraint on email at all. The original report inferred enforcement at registration without running the control case.

## Rule

> Before asserting a uniqueness contract on this target, run the control on the create path first - the demo backend enforces uniqueness nowhere, so a 422-on-duplicate expectation will fail on every endpoint, not just PUT /user. Retest QEBS-81 with the bug-retest workflow; do not re-diagnose it.
