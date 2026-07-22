<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
# conduit-api-qe - QE standards

Repo-wide standards for **conduit-api-qe**. These apply to every request in
this repository, whether or not the appqe agent is selected.

## The spec is the source of truth

API behaviour is defined by `specs/openapi.json`. Assume nothing it does not
state. If the implementation disagrees with the spec, that is a finding to raise
- not a detail to quietly encode into a test.

## Tests

- **Arrange-Act-Assert**, one Act per test, one behaviour per test.
- Name for behaviour: `Endpoint_Condition_ExpectedOutcome`.
- Assert the **documented** status code, then the schema, then the fields the
  case is about. Never `assert status < 500`.
- Tests are independent and idempotent: each creates and cleans up its own data,
  passes in any order, and is safe to run in parallel.

API tests: **xUnit** (C#) in `tests/api`, run with
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

## Show progress in the task list, not in prose

Any request here that takes more than one action gets a task list, created with
your host's todo tool **before** the first action and updated **at every step**:
mark what finished as completed and the next step as in-progress in the same
call, and carry what you learned in the finished item's title.

Two failure modes, both common:

- **Creating it once.** The panel renders where the most recent update happened,
  so a list that is seeded and then left alone scrolls out of view and shows the
  user stale state for the rest of the run. If several actions have passed with
  no update, the panel is already behind you.
- **Printing it instead.** Writing the checklist into your reply as markdown does
  not create the panel - only the tool call does. A written copy is noise the
  user reads twice and it is stale immediately. Say in one sentence what you are
  starting; let the panel carry the rest.

## Working here

Read the discovery log before starting; append durable findings after. Stop at
approval gates and ask with concrete options. Coverage below
80% is reported in the first line of a summary,
never buried.
