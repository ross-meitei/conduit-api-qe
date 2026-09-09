---
name: appqe-project-conventions
description: 'This project''s own test seams - the client factory, test-data builder, helpers and UI layout recorded in .appqe/conventions.yml. Build on these; never scaffold a parallel version.'
metadata: {version: 1.0.0, access: authoring}
user-invocable: false
---
# Skill: conduit-api-qe's test conventions



No conventions map is recorded for this project yet, so **assume nothing about
its structure**. Before the first generated test:

1. Read two existing tests end to end.
2. Read the utilities/helpers directory (whatever this project calls it).
3. Reuse what the suite already has - its client seam, its data builders, its
   fixtures. Never introduce a second way to do something the project already
   does.

Then record what you confirmed so the next run skips this archaeology:
`appqe conventions discover` (static scan), or `appqe conventions discover
--deep` for the worksheet you can answer from the code and submit with
`appqe conventions accept`.

<!-- appqe:managed v0.14.10 | do-not-edit-manually | regenerate: appqe sync -->
