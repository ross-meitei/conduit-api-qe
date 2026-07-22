<!-- appqe:managed | do-not-edit-manually -->
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

## Working here

Read the discovery log before starting; append durable findings after. Stop at
approval gates and ask with concrete options. Coverage below
80% is reported in the first line of a summary,
never buried.
