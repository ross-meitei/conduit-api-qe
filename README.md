# Conduit API QE (C# / xUnit)

API integration tests for the public **RealWorld "Conduit"** API
(`https://api.realworld.show/api`), driven by [appqe](../appqe). Mirrors a real
client engagement: **xUnit** + a **typed client generated from the OpenAPI spec
(NSwag)** + **Allure** reporting.

## Layout

```
appqe.yml                          appqe config (profile: openapi_xunit)
Conduit.Api.IntegrationTests.csproj  Allure.Xunit, NSwag client-gen, FluentAssertions, Bogus, coverlet
specs/openapi.json                 RealWorld OpenAPI spec (19 operations)
nswag.json                         NSwag config -> generated-client/ConduitApiClient.cs
allureConfig.json                  Allure output + links
coverlet.runsettings               coverage (cobertura), excludes the generated client
GlobalUsing.cs                     global usings (Xunit + Allure attributes)
env/.env.example                   BASE_URL + optional creds (copy to env/.env)
utils/                             DotEnv, ApiClientFactory, AuthTokenGenerator, TestDataFactory,
                                   AllureReportUtils, AllureHistoryFixture
tests/api/                         Users, Articles, Profiles, Tags API tests
```

## Client generation (OpenAPI -> typed client)

Tests call a typed client generated from `specs/openapi.json`, not raw HTTP.
NSwag runs at build via the `<OpenApiReference>` in the csproj, producing
`ConduitApiClient` (`ConduitApiClientNamespace`). `nswag.json` documents the same
config for manual runs (`nswag run nswag.json`). The generated client is
git-ignored and rebuilt.

## Run

```bash
cp env/.env.example env/.env        # edit BASE_URL / optional creds
dotnet test --settings coverlet.runsettings
```

Allure results land in **`bin/Debug/net9.0/allure-results/`** (Allure.Xunit writes
relative to the test binary, not the repo root), and **accumulate across runs**.
Generate a report from them with `appqe allure` (it finds the results, runs the
Allure CLI, and prints install steps if that CLI is missing):

```bash
rm -rf bin/Debug/net9.0/allure-results          # clean, so the report is just this run
dotnet test --settings coverlet.runsettings
appqe allure --serve                            # temp report; opens in a browser
# appqe allure                                  # or write .appqe/reports/allure/index.html
```

See **QE.md → "Generating the Allure report"** for the full workflow (persisting the
report, history, and why failures show as *Broken*).

## Prerequisites

| # | Requirement | Check |
|---|---|---|
| 1 | **.NET 9 SDK** | `dotnet --version` |
| 2 | **appqe on `PATH`** - the guardrail hooks shell out to it and **fail closed** | `appqe --version` |
| 3 | **`env/.env`** - copy from `env/.env.example` | `cat env/.env` |

`appqe doctor` verifies the wiring in one command and names the fix for anything
missing. See [`QE.md`](QE.md) for how to drive the agentic workflow itself.

## Checks

```bash
dotnet test --settings coverlet.runsettings   # the suite

appqe openapi                          # endpoint inventory (19 operations)
appqe lint tests utils                 # guardrails: secrets, prod URLs, TLS, weak assertions
appqe lint tests utils --strict        # + the assertion standards
appqe coverage --kind api --detail     # measured coverage, gaps in risk order
appqe coverage --kind api --check-baseline   # CI gate: fails on regression
appqe contract verify                  # the running API vs the spec
appqe doctor                           # is any of this actually wired?
```

All of it runs in CI via [`.github/workflows/api-tests.yml`](.github/workflows/api-tests.yml).

### Where things stand

- **Suite:** 10 passing, 1 quarantined (`CONDUIT-301`).
- **Guardrail lint:** clean.
- **Contract drift:** `0 findings, 0 breaking` - the spec and the running API agree.
- **Coverage:** measured, and gated on *regression* rather than the 80% target.

Coverage is **measured** from the spec and the test sources, never a number passed
on the command line. This README previously advertised
`coverage --covered 19 --total 19`, which reported 100% precisely because both
figures were typed in by the caller - a threshold gate whose inputs are asserted
cannot fail.

An operation counts as **covered** only with a happy path, the documented error
cases it warrants, and an auth case where it is secured. A single 200 test is
*partial*, and a quarantined test counts for nothing. Several operations - `PUT
/user`, `GET /articles/feed`, `PUT /articles/{slug}` and the three comments
operations among them - have no test at all. Closing that is tracked as
CONDUIT-304.

CI gates on `--check-baseline`: an absolute 80% gate would be red on every pull
request today and would simply be switched off, which enforces nothing. Raise the
bar deliberately with `appqe coverage --kind api --accept-baseline` and commit
the diff.

### The spec is corrected, not assumed

`specs/openapi.json` has been amended from verified evidence where it disagreed
with the running API - an undocumented 404 on profiles, `DELETE` returning 204
rather than 200, an undocumented `title` length limit, and a list projection that
omits a field the spec marks required. Each decision and its evidence is in the
discovery log (`appqe discovery`), so none of it can be quietly reversed to make
a test pass.

Guardrails allow the demo host and block production; no secret, token, or
production URL is hardcoded, and TLS verification is left at the platform default
(credentials come from env / a runtime login).
