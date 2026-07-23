---
name: appqe-context-sources
description: 'Gather user stories, acceptance criteria and decisions from Jira/Confluence/SharePoint, and turn them into assertions - without inventing any.'
metadata: {version: 1.0.0, access: research}
---
# Skill: Requirements context

A spec tells you the **shape** of an endpoint. It does not tell you that a
duplicate email must return 409, that `bio` is optional in the schema but
mandatory to the business, or that the team agreed a 30-second retry window on a
wiki page two sprints ago. Tests written from the spec alone are correct and
shallow; that gap is what this skill closes.

No context sources are configured. Generate from the spec alone and say so - do not invent acceptance criteria or business rules.

## Reading the bindings

The line above lists every source configured for this project. A binding
starting `mcp:` is a tool your host already has - call it directly. Any other
value is a script in this project - run it. A source named as *not configured*
is **unavailable**: say so, and generate from the spec alone.

Run `appqe doctor` if a binding looks wrong; it reports any that does not
resolve.

appqe never calls these systems itself. You fetch; it only names the binding, so
no client credential passes through the framework.

## The rule that matters

**Quote, cite, then assert.** Every assertion traceable to a requirement carries
the reference in the test - the issue key, the page title. A reviewer must be
able to check the claim without asking you where it came from.

Never paraphrase an acceptance criterion into an assertion. "Should handle
duplicates gracefully" is not a status code; go back and find the one that is
stated, or record it as an open question in the test plan.

**An unavailable source is not an invitation to guess.** If Confluence is not
wired and the spec is silent on retry semantics, the plan says
*"retry behaviour undocumented - no test"*. An invented acceptance criterion is
worse than a missing test: it is a wrong expectation with a test defending it.

## What to pull

- **The story and its ACs** - the ones that become error and edge cases. Most
  are not in the spec.
- **Status codes named in the AC** - "returns 409 on a duplicate" is exactly the
  assertion the spec's generic `422` will not give you.
- **Business rules and limits** - lengths, ranges, permitted states, role
  behaviour. These are the edge cases worth writing.
- **Decisions and their dates** - a wiki page recording why an endpoint behaves
  unusually explains a test that would otherwise look wrong.
- **Linked defects** - a bug on this endpoint is a regression test waiting to be
  written.

Do **not** pull attachments, customer records, or screenshots of production data
into the repository. You need the rule, not the data.

## Handling what you find

1. **Contradicts the spec?** That is a contract finding, not a tie to break in
   silence. Record it in the discovery log and raise it - `appqe contract verify`
   settles which side the *implementation* agrees with.
2. **Ambiguous?** It goes in the test plan's *Open questions* and blocks
   generation for that case. A guess here becomes a suite-wide wrong assumption.
3. **Clear and testable?** Write the case, cite the reference, assert the exact
   documented code.

## Confidentiality

What you fetch is client material. Quote the minimum that justifies the
assertion; do not paste a whole ticket into a test file or a commit message.
Credentials come from the environment or the host's own auth - never from a
config file, and never inline in a test. The discovery log is engagement-local:
`appqe discovery export` redacts before anything travels.

<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
