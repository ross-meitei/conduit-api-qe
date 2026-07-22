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

Allure results land in `allure-results/`; generate a report with the Allure CLI.

## appqe checks (from the appqe repo)

```bash
appqe --project-root . openapi                # endpoint inventory (19 ops)
appqe --project-root . lint tests utils       # guardrails (blocking)
appqe --project-root . lint tests utils --strict   # + assertion standards (advisory)
appqe --project-root . coverage --kind api --detail  # measured endpoint coverage
```

Coverage is **measured** from the spec and the test sources - it is not a number
passed on the command line. The README previously advertised
`coverage --covered 19 --total 19`, which reported 100% because both figures were
typed in by the caller; a threshold gate whose inputs are asserted rather than
measured cannot fail.

### Where coverage actually stands

```
api coverage 0/19 = 0% (threshold 80%) [measured]
covered 0, partial 11, uncovered 8
```

An operation counts as **covered** only with a happy path, a documented error
case, and an auth case where it is secured. Eleven operations are exercised but
incomplete; eight - `PUT /user`, `GET /articles/feed`, `PUT /articles/{slug}`,
and the three comments operations among them - have no test at all. Closing this
is tracked as CONDUIT-304, and the CI coverage step is staged (reporting, not
blocking) until it is.

The suite itself is green: **10 passing, 1 quarantined** (`CONDUIT-301`, the
follow flow, which this backend cannot support - see the discovery log).

Guardrails allow the demo host and block production; no secret, token, or
production URL is hardcoded, and TLS verification is left at the platform
default (credentials come from env / a runtime login).
