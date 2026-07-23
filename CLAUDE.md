# CLAUDE.md — conduit-api-qe

API integration tests for the public RealWorld **Conduit** API
(`https://api.realworld.show/api`). C# / **xUnit** + a **typed NSwag client
generated from the OpenAPI spec** + **Allure** reporting. API-only. Historically
driven by the `appqe` orchestrator through a GitHub Copilot custom agent; this
workspace is now driven through **Claude Code** — the appqe CLI is still used for
its read-only QE checks (inventory, lint, coverage, contract, doctor).

## Commands

```bash
# Build (NSwag regenerates the typed client at build via <OpenApiReference>)
dotnet build

# Run the suite (coverage settings exclude the generated client)
dotnet test --settings coverlet.runsettings

# Run one test / class
dotnet test --settings coverlet.runsettings --filter "FullyQualifiedName~Users"

# Regenerate the typed client manually (normally automatic at build)
nswag run nswag.json     # -> generated-client/ConduitApiClient.cs (git-ignored)
```

### appqe QE checks (read-only — safe to run anytime)

```bash
appqe openapi                                # endpoint inventory (19 operations)
appqe lint tests utils                       # guardrails: secrets, prod URLs, TLS, weak assertions
appqe lint tests utils --strict              # + assertion standards
appqe coverage --kind api --detail           # measured coverage, gaps in risk order
appqe coverage --kind api --check-baseline   # CI gate: fails on regression
appqe contract verify                        # running API vs the spec
appqe discovery                              # spec-correction evidence log
appqe doctor                                 # is the wiring intact?
```

Prereqs: **.NET 9 SDK**, `appqe` on `PATH` (hooks fail closed without it), and
`env/.env` copied from `env/.env.example`.

## Layout

- `specs/openapi.json` — RealWorld OpenAPI spec (19 operations); **corrected from
  verified evidence** where it disagreed with the running API. Don't silently
  revert corrections to make a test pass — evidence lives in `appqe discovery`.
- `nswag.json` — client-gen config → `ConduitApiClient` (`ConduitApiClientNamespace`).
- `tests/api/` — Users, Articles, Profiles, Tags API tests.
- `utils/` — DotEnv, ApiClientFactory, AuthTokenGenerator, TestDataFactory, Allure helpers.
- `appqe.yml` — QE config (profile `openapi_xunit`, tracker `CONDUIT`).
- `coverlet.runsettings` — cobertura coverage, excludes the generated client.

## Conventions

- Tests call the **typed generated client**, never raw HTTP.
- An operation is **covered** only with a happy path, its documented error cases,
  and an auth case where secured. A single 200 test is *partial*; a quarantined
  test counts for nothing.
- Coverage is **measured** from the spec + sources — never a number passed on the
  command line. CI gates on `--check-baseline` (regression), not an absolute %.
  Raise the bar deliberately with `appqe coverage --kind api --accept-baseline`.
- Guardrails allow the demo host (`api.realworld.show`) and **block production**.
  No secret, token, or prod URL is hardcoded; creds come from `env/.env` / login.
- Bug tracker is Jira project **CONDUIT** (`conduit-qe.atlassian.net`).

### Current state (keep this honest when it changes)

- Suite: 10 passing, 1 quarantined (`CONDUIT-301`).
- Contract drift: 0 findings. Lint: clean.
- Untested operations tracked as **CONDUIT-304**: `PUT /user`,
  `GET /articles/feed`, `PUT /articles/{slug}`, and the three comments operations.

## Workflow & session tracking

- **Use the todo list (TodoWrite) as the live task view.** This is the native
  Claude Code equivalent of the "task-list panel" — keep exactly one item
  `in_progress`, mark items `completed` as you finish. It renders in the Claude
  Code UI; the old Copilot `chat.todoListTool` panel does not apply here.
- **Write a session log** per working session to
  `.claude/sessions/YYYY-MM-DD-<short-slug>.md`: symptom/goal, what you found,
  root cause, what changed, and open next steps. Mirror the existing style in
  `.appqe/session-logs/`.
- Approval gates: **no gate is auto-approved** (`gates: {}` in `appqe.yml`).
  Stop and confirm before generating/finalizing tests or editing the spec.
- Commit or push only when asked.
