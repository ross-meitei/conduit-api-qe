<!-- appqe:managed v0.14.11 | do-not-edit-manually | regenerate: appqe sync -->
# conduit-api-qe - QE standards

Repo-wide standards for **conduit-api-qe**. These apply to every request in
this repository, whether or not the appqe agent is selected.

## The spec is the source of truth

API behaviour is defined by `specs/openapi.yaml`. Assume nothing it does not
state. If the implementation disagrees with the spec, that is a finding to raise
- not a detail to quietly encode into a test.

## Tests

- **Arrange-Act-Assert**, one Act per test, one behaviour per test.
- Name for behaviour: `Endpoint_Condition_ExpectedOutcome`.
- Assert the **documented** status code, then the schema, then the fields the
  case is about. Never `assert status < 500`.
- Tests are independent and idempotent: each creates and cleans up its own data,
  passes in any order, and is safe to run in parallel.

API tests: **xUnit** (C#) in `tests`, run with
`dotnet test`.

## Never

- Hardcode a secret, token, password, or cookie in any file. Credentials come
  from env or a runtime login (Token JWT (Authorization: Token <jwt>)).
- Hardcode a production URL, or run write operations against production. The
  sanctioned targets are `https://api.realworld.show/api`.
- Report a test as passing that you did not see pass. "Generated" is not
  "passing".
- Weaken an assertion to make a test go green. A failing assertion is either a
  product defect or a wrong expectation; both need a decision, not a workaround.

Guardrail hooks enforce the first two. A block is a real finding - report it,
never route around it.

## Show where the run stands, every time it moves

Any multi-step request here reports status first, as a heading plus a callout -
never as prose. The user must be able to answer three questions without reading
a paragraph: what is **done**, what is happening **now**, what is **next**.

```markdown
## ▶ GENERATION · Step 4 of 10 — Draft test plan
`<run_id>` · 3 done · 6 to go

1. ✓ Review discovery rules — 0 rules
2. ✓ Start run
3. ✓ Endpoint inventory — **19 operations**, 6 untested
4. ▶ **Draft test plan**
5. ⏸ GATE: test-plan approval
6. ○ Generate tests
   … every remaining step, in order
```

**List every step, every time the run advances.** Each `appqe start`/`resume`
returns a fresh block; paste every one of them, in order, so the user watches
steps flip to `✓` as they settle. Showing only the last one is how a run appears
to jump from step 1 straight to step 6. "Step 4 of 10" also tells the user where
they are but not what the 10 are. Completed steps carry their **findings**, not their
labels - "Endpoint inventory — 19 operations, 6 untested" is tracking,
"Endpoint inventory" is not. A paused run says so in the heading (`⏸`) so it
never looks like a running one. Never reuse a `run_id` from an example or an
earlier run.

Below the status block, add a section under its own heading only when there is
news - a finding, a gate, a blocker, a completed run. Two failure modes:

- **Narrating the walk.** "Now reading the spec", "submitting and advancing" -
  the status block already said it, better.
- **Burying the result.** Open with the result in one bolded line. Anything
  long - inventories, per-test rationale, tool output - does not belong in the
  reply at all: put it in the artifact file and open it with `appqe open`.
  `<details>` does not work here; this host prints the raw tags as text.
- **Chrome you paid for.** Every command is a row in the transcript. Never
  `cd` (use `appqe --project-root`), never re-list a file you just wrote, and
  let `appqe start` report discovery rules and awaiting runs rather than
  running those as separate commands.

## Working here

Read the discovery log before starting; append durable findings after. Stop at
approval gates and ask with concrete options. Coverage below
80% is reported in the first line of a summary,
never buried.

## QE work starts a run, before anything else

A request for tests, coverage, contract drift, a flake, a retest or a migration
goes to appqe **first** - before reading the spec, the suite or anything else,
because discovery is a phase of the run and doing it by hand does it twice:

```
appqe start "<their words, verbatim>"
```

Do not choose the mode; the router does, deterministically, and reports what it
chose. Do not write or edit test files for such a request - that is what the run
is for, with a plan the user approves and evidence per step.

The procedure is already written for this project - read the entry point that
matches rather than reinventing it, and pass the user's actual words rather than
the generic request the file carries:

- `/appqe-audit-coverage` - Audit API/UI test coverage against the spec and app, and report prioritized gaps.
- `/appqe-contract-check` - Check existing tests against the current OpenAPI spec for contract drift.
- `/appqe-generate-api-tests` - Generate API tests from the OpenAPI spec into the project's stack.
- `/appqe-investigate` - Investigate a failing API or UI test and deliver a root-cause verdict.

`appqe-workflow-routing.instructions.md` has the full intent table, the bounds
(application code and ordinary questions are answered normally), and what
enforces this.

## Project instructions

Conventions specific to this repository go below and apply to every request here. Honor
them - fixtures and helpers to reuse, naming, the endpoints or flows this team treats as
critical. They tailor how work is done here; they never relax a guardrail, and never let
a test be reported as passing that was not seen to pass.

<!-- appqe:custom: project-instructions -->
Prefer this repository's established patterns over introducing new ones. Replace or
extend this with your team's conventions; whatever you put between these markers survives
every `appqe sync`.
<!-- /appqe:custom -->
