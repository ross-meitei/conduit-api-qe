# Conduit API QE (C# / xUnit)

API integration tests for the public **RealWorld "Conduit"** API
(`https://api.realworld.show/api`). Mirrors a real client engagement: **xUnit** +
a **typed client generated from the OpenAPI spec (NSwag)** + **Allure** reporting.

## Layout

```
Conduit.Api.IntegrationTests.csproj  Allure.Xunit, NSwag client-gen, FluentAssertions, Bogus, coverlet
specs/openapi.json                 RealWorld OpenAPI spec (19 operations), drives client generation
specs/openapi.yaml                 the same spec in YAML, for reading
nswag.json                         NSwag config -> generated-client/ConduitApiClient.cs
allureConfig.json                  Allure output + links
coverlet.runsettings               coverage (cobertura), excludes the generated client
GlobalUsing.cs                     global usings (Xunit + Allure attributes)
env/.env.example                   BASE_URL + optional creds (copy to env/.env)
utils/                             DotEnv, ApiClientFactory, AuthTokenGenerator, TestDataFactory,
                                   TestUserRegistrar, SchemaValidator, AllureReportUtils,
                                   AllureHistoryFixture
tests/                             API tests (UserApiTests.cs)
```

## Client generation (OpenAPI -> typed client)

Tests call a typed client generated from `specs/openapi.json`, not raw HTTP.
NSwag runs at build via the `<OpenApiReference>` in the csproj, producing
`ConduitApiClient` (`ConduitApiClientNamespace`). `nswag.json` documents the same
config for manual runs (`nswag run nswag.json`). The generated client is
git-ignored and rebuilt.

## Prerequisites

| # | Requirement | Check |
|---|---|---|
| 1 | **.NET 9 SDK** | `dotnet --version` |
| 2 | **`env/.env`** - copy from `env/.env.example` | `cat env/.env` |
| 3 | **Allure CLI** (optional, for HTML reports) | `allure --version` |

## Run

```bash
cp env/.env.example env/.env        # edit BASE_URL / optional creds
dotnet test --settings coverlet.runsettings
```

## Allure report

Allure results land in **`bin/Debug/net9.0/allure-results/`** (Allure.Xunit writes
relative to the test binary, not the repo root), and **accumulate across runs**, so
clear them first if you want a report of just the latest run:

```bash
rm -rf bin/Debug/net9.0/allure-results
dotnet test --settings coverlet.runsettings
allure serve bin/Debug/net9.0/allure-results                               # temp report in a browser
# allure generate bin/Debug/net9.0/allure-results -o allure-report --clean  # or write a static report
```

Guardrails: the tests target the public demo host only. No secret, token, or
production URL is hardcoded, and TLS verification is left at the platform default
(credentials come from env / a runtime login).
