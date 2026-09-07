---
name: appqe-workflow-routing
description: 'Route QE work in **conduit-api-qe** through appqe rather than editing tests directly'
applyTo: '**'
---
# QE work runs through appqe

This project uses appqe for test generation and QE analysis. Recognise the
intent yourself - the person asking does not have to name a command, pick a
prompt, or select an agent, and will usually not know those exist.

## When the request is QE work, this is your first action

```
appqe start "<their words, verbatim>"
```

**If you are not already running as appqe's agent, read its contract first:**
`.github/agents/appqe-agent.agent.md`. One file read, at the start of a run, and only then -
it is how the run knows to print a status block per batch, keep the task list in
step, stop dead at gates, and hand delegated steps to the worker they belong to.

That line exists because routing works. A request now reaches appqe without
anyone selecting an agent, which means the run happens wherever the request
landed - and the contract is loaded only when the agent is *selected*. Every
appqe surface that a plain session was missing traces back to this: the task
panel showing a different run, the narration the contract forbids, worker steps
run inline. Read it once and none of that applies.

**Before you read a single file.** Not after inspecting the spec, not after
reading the existing suite, not after drafting your own plan. Each of those is a
phase OF the run: `appqe start` returns the phase list, and discovery reads the
spec and the suite once, at the right tier, recording what it found as evidence.
Doing it first means doing it twice and throwing the first away.

Observed on a live run, which is why this paragraph exists. Given *"generate api
tests for user endpoint"* the agent recognised the intent, wrote **"Route test
gen to appqe"** as item one of a four-item checklist, ticked it, and spent the
next two items reading the spec and 894 lines of the existing test class by
hand - run not started, appqe pencilled in as step four. **Routing is not a box
to tick before doing the work yourself. The command is the routing**, the run
brings its own checklist, and "hand it to the appqe agent later" is not routing
either.

## The procedure is already written down - do not reinvent it

This project deploys a vetted entry point for each common workflow. Read the one
that matches and follow it: each is rendered for THIS project, so it names the
actual stack, the actual spec path and the gates to stop at, and it is kept in
step with appqe by `appqe sync`.

- `/appqe-audit-coverage` - Audit API/UI test coverage against the spec and app, and report prioritized gaps.
- `/appqe-contract-check` - Check existing tests against the current OpenAPI spec for contract drift.
- `/appqe-generate-api-tests` - Generate API tests from the OpenAPI spec into the project's stack.
- `/appqe-investigate` - Investigate a failing API or UI test and deliver a root-cause verdict.

Two things about using one as the agent rather than as the person:

- **Pass the user's words, not the prompt's.** A prompt carries a generic
  request so a person can fire it bare. Following it literally for *"generate api
  tests for user endpoint"* would start a run for the whole spec and lose the
  scope the user asked for.
- **The prompt names the mode; you do not.** It may say `--mode`, because
  choosing that prompt IS the choice. Arriving there from a guess is not the
  same act - if you inferred the workflow from what was said, drop `--mode` and
  let the router decide.

## What counts as QE work

Every workflow appqe has, and the kind of thing a person says to mean it. The
wording below is illustrative - people phrase these a hundred ways, and the
router matches on intent rather than on these exact words.

| What the person says | Workflow |
|---|---|
| "generate api tests for /orders", "tests from the openapi spec" | `api_generation` |
| "write e2e tests for checkout", "playwright tests for the login page" | `ui_generation` |
| "load test /search", "how does it hold at 500 users", a soak or stress test | `perf_generation` |
| "why is this test failing", "root cause this", "triage the flake" | `investigation` |
| "retest BUG-123", "verify the fix landed" | `bug_retest` |
| "what is untested", "audit coverage", "where are the gaps" | `audit` |
| "has the API drifted from the spec", "check contract drift" | `contract_check` |
| "add test ids", "these selectors are brittle", "instrument the page" | `instrumentation` |
| "monitor checkout in production", "a synthetic check for login" | `monitor` |
| "did the replatform keep the behaviour", "validate the migration" | `migration_validation` |
| "port the cypress suite to playwright", "migrate off selenium" | `framework_migration` |

**Do not pick the mode.** Hand the request over as the person phrased it -
appqe routes it itself, deterministically, and reports which workflow it chose
and how confident it was. The Workflow column is there so a reader can audit that
choice - not so you can make it. If the routing is plainly wrong, say so and
re-run with `--mode`; if you cannot tell, start the run and let the routing
report answer it.

## What NOT to do

- **Do not write or edit test files directly** for a request in that table.
  That is what the workflow exists to do, with a plan the user approves and
  evidence for each step. A hand-written test is not covered by any of it.
- **Do not answer from reading the code** when the request asks what is tested
  or what is covered. `appqe coverage` measures it; an estimate reads the same
  as a measurement and is not one.
- **Do not skip the run because the change looks small.** One test is a short
  run, not a reason to bypass the gates.
- **Do not explore first**, per the section above. If you genuinely cannot tell
  whether a request is QE work, start the run - the routing report answers that
  faster than reading does.

## When it is not QE work

Application code, infrastructure, docs, a build failure, a question about how
something works - answer normally. This applies to test artifacts and QE
analysis, not to everything in a test-bearing repo.

## If the user asked for something appqe cannot do

Say so plainly and do the work directly. The point is that QE work is not
bypassed by accident, not that every request is forced through a workflow.

---

**Enforced by**, in order of how late they bite:

1. **Nothing, at the moment you are read.** appqe cannot see a request that was
   never given to it. This instruction being loaded and followed is the only
   thing between "add some tests" and a hand-written spec, and that is a weaker
   guarantee than the rest of appqe offers.
2. **The pre-tool guard**, where this host has one: a NEW test file written with
   no run open is refused, twice, naming this command. Bounded on purpose -
   appqe cannot see the request, so it cannot prove a deliberate hand-written
   test is not what was asked for.
3. **`appqe scaffold --pre-commit`**, if the project installed it: staged test
   files that no run produced are rejected at commit time. `--no-verify` goes
   through by design.

None of the three makes bypass impossible, and more forceful wording here would
not either. Start the run.

<!-- appqe:managed v0.14.8 | do-not-edit-manually | regenerate: appqe sync -->
