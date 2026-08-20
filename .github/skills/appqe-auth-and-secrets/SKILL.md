---
name: appqe-auth-and-secrets
description: 'Handle authentication in tests without hardcoding secrets, and keep captured responses clean.'
metadata: {version: 1.0.0, access: read-only}
---
# Skill: Auth & secrets handling

Scheme: **Token JWT (Authorization: Token <jwt>)**.

## Where credentials come from

In priority order: a **secrets manager**, then **environment variables**, then a
local `.env` that is git-ignored. Never a literal in a test, a fixture, a config
file, or a committed `.env`.

Scheme for this project: **Token JWT (Authorization: Token <jwt>)**.

## Pattern

**The `.env` contract - names yes, values never.** What variables a source
needs: `env/.env.example` (safe to read; the scaffold writes it) or the setup
steps. Whether they are set: `appqe tools verify` reports set/MISSING per name -
no value ever printed, nothing executed. Getting values in: ask the user to
edit `env/.env`; you never read, write, cat or grep it (the guard blocks it).
Using them: run the client - it loads the file itself. The same rule covers
environment variables: reading `os.getenv` belongs to the *client process*;
you never `echo $SOME_TOKEN` or dump the environment - that is the file-read
exposure one indirection later, and the guard blocks it. In CI or shared
environments, credentials come from the platform's secret store (GitHub
Actions secrets, Vault, ...) into the process env - same client code, no file.

- Expose auth through **one** seam - a fixture, base test class, or client
  factory. A single place to change when the scheme does, and a single place to
  audit.
- **API**: acquire a token once per session, reuse it, refresh on expiry. Do not
  log in per test; do not paste a token between tests.
- **UI**: log in once, persist an authenticated **storage state**, reuse it. The
  login flow itself gets exactly one test.
- **Roles**: parameterise the fixture by role rather than duplicating tests, so
  a 403 case is a parameter and not a copy-paste.

## Never

- Commit a real credential, even a "test" one - test accounts on a shared
  environment are real accounts.
- Print a token in a log, an assertion message, or a report. Redact before
  output; the credential scanner enforces this.
- Reuse production credentials against a non-production target, or the reverse.
- Skip a 401/403 test because auth is "handled by the framework" - that is
  precisely the assertion that catches a broken auth config.

## Test data that needs a password

Many APIs require a password to register a user, so tests legitimately need one.
In priority order:

1. **Generate it** from the project's test-data seam (the conventions map or
   existing tests name it) - unique per run, no literal, parallel-safe. This is
   the right answer almost always; create a factory only where none exists.
2. **Read it from env** when the account must be stable across runs.
3. **Literal, marked** - only for genuinely synthetic data on a throwaway
   account, and only with an explicit marker on the line:

   ```
   password = "Test1234!"   // appqe: allow-secret
   ```

   The marker is greppable on purpose: a reviewer can find every one in a single
   search. A file full of them is a smell, not a solution.

Never use option 3 for anything that authenticates against a real account.

## If a guardrail blocks you

The block is the finding. A hardcoded secret that a hook caught is a secret that
would otherwise have reached the repository. Move it to env, and note in the
discovery log where it came from.

<!-- appqe:managed v0.12.11 | do-not-edit-manually | regenerate: appqe sync -->
