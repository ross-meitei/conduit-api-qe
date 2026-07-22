---
name: appqe-discovery-log
description: 'Institutional QE memory - read before a run, append durable findings after. Carries auth quirks, flaky causes, and spec gotchas across sessions.'
metadata: {version: 1.0.0, access: authoring}
---
# Skill: Discovery log

Location: `.appqe/discovery.md` (`.github/discovery.md` on Copilot). This is
**conduit-api-qe**'s QE memory. Run state lives in appqe; *knowledge* lives
here.

## The loop

```bash
appqe discovery                 # the rules index - read this at the start of every run
appqe discovery add --area <endpoint/flow> --finding <what> \
                    --cause <why> --rule <what the next run should do>
```

The log has two layers on purpose. **Rules in force** is an index - one
actionable line per lesson, cheap enough to read on every run and injected into
the session automatically. **Findings** below it hold the full story, read only
when a rule alone is not clear. A flat log gets expensive to read, so it stops
being read, and the loop dies.

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

`appqe discovery add` writes both layers for you - the rule into the index, the
finding below. Write entries by hand only when editing history.

```markdown
### 2026-03-14 - POST /articles - 422 on valid payload

**Finding**    Valid payload rejected when `tagList` is an empty array.
**Cause**      API requires the key absent, not empty; spec says optional.
**Resolution** Omit `tagList` when empty. Raised as CONDUIT-214.
**Future rule** Treat "optional array" as omit-when-empty until the spec is fixed.
```

The **future rule** is the point of the entry - a sentence the next run can apply
without rereading the investigation. An entry without one is a diary, not memory.

## Keep it usable

Newest first, grouped by area (endpoint or flow). Stack-neutral and factual. When
a finding is fixed upstream, mark it resolved with the date rather than deleting
it - knowing something *was* broken explains an old workaround.

<!-- appqe:managed | do-not-edit-manually -->
