---
name: appqe-plain-language
description: 'Write the text a client actually reads - bug verdicts, root-cause write-ups, coverage findings, experience findings, phase summaries - so a busy non-specialist gets it on first reading. Applies ISO 24495-1 plain language principles. Load it before writing any finding, comment or summary that leaves the repo.'
metadata: {version: 1.0.0, access: read-only}
user-invocable: false
---
# Skill: plain language for anything a client reads

appqe produces text for people: a bug verdict posted to a tracker, a root-cause
write-up, a coverage gap, an experience finding, the summary at the end of a run.
That text is a deliverable. It gets read by a product owner, a developer who did
not run the tests, or a client stakeholder who was not in the room.

ISO 24495-1:2023 defines plain language by reader outcome. Text is plain when the
intended reader can get what they need (**relevant**), find it (**findable**),
understand it on first reading (**understandable**), and act on it (**usable**).

## When to load this

Before writing anything that leaves the repo or lands in front of a person:

- a bug verdict or tracker comment
- a root-cause narrative
- a coverage or experience finding
- a discovery-log entry the next run will read
- the phase summary a reviewer sees

Not for test code, assertions, selectors or identifiers. Those follow the
project's conventions, and a machine reads them.

## Procedure

1. **Name the reader in one line before you write.** "The developer who owns
   `/user`" and "the product owner deciding whether to ship" want different
   text. Every keep-or-cut decision follows from this.
2. **Front-load the finding.** First sentence states what is wrong, or what was
   decided. Evidence and reasoning come after.
3. **Apply the techniques** in `references/english-techniques.md`.
4. **Run the self-check** at the bottom of that file.

## The four principles, applied to QE findings

| Principle | For appqe's output, this means |
|---|---|
| Relevant | The reader gets what they need to act. Cut the process detail: which step ran, which model, how many retries. That lives in the telemetry. |
| Findable | The verdict is in the first line. One finding per paragraph. A list when there are three or more parallel items. A table when the reader will compare values. |
| Understandable | Common words, short sentences, explicit actors. Name the endpoint, the flow, the status code. Never leave an abbreviation unglossed on first use. |
| Usable | Say who does what next. "AC7 expects 422; the API returns 200" beats "there appears to be a discrepancy in validation behaviour". |

## Never trade precision for plainness

A number, a condition, a scope qualifier or an honest hedge stays, always. If
plain wording would lose a fact, keep the fact and put the qualifier in its own
short sentence. A finding that reads well and overstates what was proven is worse
than a clumsy one that is exact.

The same rule covers evidence. Do not soften "the test failed" into "the test did
not fully pass", and do not harden "0 results, query ran" into "no such records
exist".

## Attribution

Derived from the `iso-24495-skill` project by Dustin Yuchen Teng, MIT licensed,
adapted here for appqe's reader-facing artifacts. The upstream project also ships
a Traditional Chinese technique layer, which is not vendored here.

Unofficial, and not affiliated with or endorsed by ISO. The ISO 24495-1 text is a
licensed document and is not reproduced. Every quantitative tripwire in the
technique file is a working proxy, not a clause of the standard.

<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
