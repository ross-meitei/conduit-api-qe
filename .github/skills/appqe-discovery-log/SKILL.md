---
name: appqe-discovery-log
description: 'Institutional QE memory - read before a run, append durable findings after. Carries auth quirks, flaky causes, and spec gotchas across sessions.'
metadata: {version: 1.0.0, access: authoring}
user-invocable: false
---
# Skill: Discovery log

Location: `.appqe/okf/findings/` (`.github/okf/findings/` on Copilot) - an Open
Knowledge Format bundle: one markdown doc per finding, filed under its domain,
plus a generated `index.md`. This is **conduit-api-qe**'s QE memory. Run state
lives in appqe; *knowledge* lives here.

## The loop

```bash
appqe discovery                 # the rules index - read this at the start of every run
appqe discovery add --area <endpoint/flow> --finding <what> \
                    --cause <why> --rule <what the next run should do>
```

The bundle has two layers on purpose. The **index** carries one line per lesson -
cheap enough to read on every run, and the confirmed rules are injected into the
session automatically. The **finding docs** hold the full story, read only when a
rule alone is not clear. A flat log gets expensive to read, so it stops being
read, and the loop dies.

Recording the same title twice bumps its `seen_count` rather than duplicating it,
and that count is the signal: `NEW` (seen once) is a hypothesis and stays out of
the session context, `CONFIRMED` (seen twice) is a rule runs must apply, and
`appqe discovery promote <slug>` makes a repeated rule permanent.

## Read it first

Before starting any run, read the index. A previous session may already have paid
for the answer - why an endpoint 401s, which flow is flaky and why, where the
spec disagrees with the implementation. This is the cheapest context available;
skipping it means re-investigating something already solved.

## Append when you learn something durable

Only findings a **future run** would act on:

- an endpoint needing a header the spec omits,
- a flow that is flaky, with the cause and the fix,
- a spec-vs-implementation gap (and which one is right),
- a fixture or data setup other tests should reuse,
- a guardrail block that turned out to be a genuine product bug.

Do **not** log run narration ("generated 12 tests"), anything already in the
spec, or a secret, token, or customer identifier. Telemetry
(`appqe telemetry summary`) covers what happened; this covers what it *means*.

## Entry format

`appqe discovery add` writes the doc and regenerates the index for you. Use it
rather than writing files by hand: the `discovery_update` step is proved by a
finding doc appearing on disk, and the frontmatter it writes is what the status
lifecycle counts.

```markdown
---
type: finding
title: 'POST /articles - 422 on valid payload'
domain: articles
status: CONFIRMED
seen_count: 2
first_seen: 2026-03-14
last_seen: 2026-04-02
rule: 'Treat "optional array" as omit-when-empty until the spec is fixed.'
okf_version: "0.1"
---

## Finding

Valid payload rejected when `tagList` is an empty array.

## Root cause

API requires the key absent, not empty; spec says optional.

## Resolution

Omit `tagList` when empty. Raised as QEBS-214.
```

The `rule` is the point of the entry - a sentence the next run can apply without
rereading the investigation. An entry without one is a diary, not memory, and
only a finding *with* a rule can ever be promoted or exported.

## It is shared, so record rather than edit

The bundle is committed and your teammates have their own copy. `appqe discovery
add` is what keeps that safe: one doc per finding means two people recording
different things never collide, and when two people record the *same* thing the
merge adds the sightings up rather than picking one - which is what turns two
independent reports into a `CONFIRMED` rule. That merge is wired per clone by
`appqe sync`, so a teammate who has not run it will get an ordinary conflict
instead; resolve it by keeping both descriptions and adding the counts. Hand-editing a doc's `seen_count`
or `status` breaks that arithmetic. `index.md` is generated and git-ignored;
never edit it, and never resolve a conflict in it by hand.

## Keep it usable

One finding per doc, filed under its area (endpoint or flow) - that is the
`--area` you pass. Stack-neutral and factual. When a finding is fixed upstream,
record the resolution rather than deleting the doc - knowing something *was*
broken explains an old workaround.

<!-- appqe:managed v0.14.11 | do-not-edit-manually | regenerate: appqe sync -->
