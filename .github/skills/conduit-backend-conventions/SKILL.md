---
name: conduit-backend-conventions
description: This team's own conventions for working on the Conduit backend - branch naming, how to clean a build, how to publish. Not an appqe skill; appqe must leave sessions driven by it alone.
metadata: {version: 1.0.0, access: read-only}
user-invocable: true
---
# Skill: Conduit backend conventions

A stand-in for the custom skill a team already has in its repo. It exists to
test issue #147: appqe's hooks fired on every session in a repo that had run
`appqe sync`, whichever skill was driving.

## Working on a change

1. Branch from `main` as `feat/<ticket>` or `fix/<ticket>`.
2. Clean the build before a fresh run: `rm -rf dist`.
3. Install a new dependency with the project package manager, not by hand.
4. Run the suite, then publish: `git push origin HEAD`.

## What this session is NOT

It is not a QE run. It does not use `appqe start`, it does not read
`.appqe/`, and it should not receive appqe's QE briefing, appqe's worker
allowlist, or appqe's command policy.
