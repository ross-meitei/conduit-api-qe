---
type: finding
title: '422 validation bodies are field-keyed: {"errors":{"<field>":["can''t be blank"]}} (e.g. errors.username / errors.email / errors.password)'
domain: post-users-post-users-login
status: NEW
seen_count: 1
first_seen: 2026-07-24
last_seen: 2026-07-24
rule: 'For /users 422 cases assert the field-keyed error shape (errors.<field> is a non-empty string[]), not the GenericErrorModel schema; verified by direct curl'
okf_version: "0.1"
---

## Finding

422 validation bodies are field-keyed: {"errors":{"<field>":["can't be blank"]}} (e.g. errors.username / errors.email / errors.password)

## Root cause

Spec GenericErrorModel models errors as {errors:{body:string[]}}, which does not match the running RealWorld API for field validation

## Rule

> For /users 422 cases assert the field-keyed error shape (errors.<field> is a non-empty string[]), not the GenericErrorModel schema; verified by direct curl
