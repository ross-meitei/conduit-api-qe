---
type: finding
title: 'The duplicate-email defect on PUT /user is tracked as QEBS-81 (https://slalom.atlassian.net/browse/QEBS-81), filed 2026-07-24.'
domain: user
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'Reference QEBS-81 when UpdateCurrentUser_DuplicateEmail_Returns422 fails - it is the tracked defect, not a new one. Retest it with the bug-retest workflow rather than re-diagnosing.'
okf_version: "0.1"
---

## Finding

The duplicate-email defect on PUT /user is tracked as QEBS-81 (https://slalom.atlassian.net/browse/QEBS-81), filed 2026-07-24.

## Root cause

utilities/jira_client.py is read-only (GET only), so the issue was created by a direct POST to /rest/api/3/issue using JIRA_EMAIL and JIRA_API_TOKEN from env/.env.

## Rule

> Reference QEBS-81 when UpdateCurrentUser_DuplicateEmail_Returns422 fails - it is the tracked defect, not a new one. Retest it with the bug-retest workflow rather than re-diagnosing.
