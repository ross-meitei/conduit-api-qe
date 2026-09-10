---
name: appqe-token-economy
description: 'Cut token spend on long QE runs without losing technical precision - compressed prose, staged data, no re-reads. Use on multi-phase runs, large specs, or when asked to be brief.'
metadata: {version: 1.0.0, access: read-only}
user-invocable: false
---
# Skill: Token economy

QE runs are long: a spec survey, dozens of generated tests, several resume
cycles. Cost comes from repetition, not from thinking. Compress the prose, never
the substance.

## Levels

Default **lite** on client work. `full` on long internal runs. Never `ultra` in
anything a client reads.

| Level | What changes |
|---|---|
| **lite** | No filler, no hedging, no pleasantries. Full sentences, professional register. |
| **full** | Drop articles where unambiguous. Fragments fine. Short synonyms. No tool-call narration, no decorative tables. |
| **ultra** | Abbreviate prose words only (req/res/config/auth). Arrows for causality. Internal use only. |

Never abbreviated at any level: endpoint paths, status codes, schema and field
names, test names, CLI commands, exact error strings, code.

## What actually saves tokens

Prose style is the smallest win. These are the large ones:

- **Read once.** A file you have read is in context. Re-reading it to "check"
  costs full price for zero new information.
- **Stage before delegating.** Read the data, inline the relevant slice into the
  delegated prompt. A subagent that fetches burns 2-5 extra calls.
- **Slice by role.** The endpoint inventory is enough to plan tests; the whole
  spec is not needed. Pass the slice, not the document.
- **Cite, don't restate.** Name the skill or instruction file. Reproducing its
  content in the reply pays for it twice.
- **Quote the decisive line.** One assertion diff or stack frame, not the run log.
- **Batch the resume.** Submit all completed action results in one
  `appqe resume` call, not one call per action.

## Always full clarity

Drop compression entirely for: guardrail blocks and security findings,
destructive or irreversible steps, approval-gate questions, and any point where
brevity creates ambiguity. Resume compression afterwards.

A compressed reply that has to be re-explained cost more than the verbose one.

<!-- appqe:managed v0.14.11 | do-not-edit-manually | regenerate: appqe sync -->
