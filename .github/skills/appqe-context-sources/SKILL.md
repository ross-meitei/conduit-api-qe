---
name: appqe-context-sources
description: 'Gather stories, acceptance criteria and decisions from the project''s requirement sources (any system wired in appqe.yml) and turn them into assertions - without inventing any.'
metadata: {version: 1.0.0, access: research}
---
# Skill: Requirements context

A spec tells you the **shape** of an endpoint. It does not tell you that a
duplicate email must return 409, that `bio` is optional in the schema but
mandatory to the business, or that the team agreed a 30-second retry window on a
wiki page two sprints ago. Tests written from the spec alone are correct and
shallow; that gap is what this skill closes.

Context sources available: confluence: `utils/confluence_client.py`; jira: `utils/jira_client.py`. Not configured (do not guess at their contents): sharepoint. Quote what you find and cite the source; never paraphrase an acceptance criterion into an assertion without the reference. If this run needs one that is not configured, wire it first (`appqe scaffold --tool sharepoint`), then re-run.

## Reading the bindings

The line above lists every source configured for this project. A binding
starting `mcp:` is a tool your host already has - call it directly. Any other
value is a script in this project - run it. A source named as *not configured*
is **unavailable**: say so, and generate from the spec alone.

Run `appqe doctor` if a binding looks wrong; it reports any that does not
resolve.

appqe never calls these systems itself. You fetch; it only names the binding, so
no client credential passes through the framework.

## Setting a source up (asked for, not yet wired)

Whatever the system - Jira, Azure DevOps, GitLab, Notion, an in-house tracker -
your job is the same fetch-digest-cite loop; the per-system steps arrive **in the
action inputs**, not here. Two rules bound the setup work:

- **Wiring needs the user's approval, never your initiative.** appqe raises the
  `tool_setup` gate (wire now / skip this run / never - `binding: skip` records a
  "never", so do not re-suggest it). "Wire now" is also the approval to add the
  binding line to appqe.yml.
- **appqe verifies; claims bounce.** It re-reads appqe.yml and checks the binding
  resolves before the run continues - run the client once for real and include the
  output.

`appqe scaffold --tool <name>` works for **any** name - catalog conventions, the
generic `TODO(agent)` skeleton, or MCP directions where a REST client would be a
lie. For the dedicated **Jira** and **Confluence** starters:

1. **Scaffold the client**: `appqe scaffold --tool jira` or
   `appqe scaffold --tool confluence` writes `utilities/<name>_client.py` and
   prints the remaining steps. `--overwrite` refreshes an existing one.
2. **Name the non-secret config once, in `appqe.yml`.** Shared and committed,
   never a per-developer setting:
   - Jira: `tracker.base_url` (host) and `tracker.project_key` (default project).
   - Confluence: usually nothing - the client derives its `/wiki` host from
     `tracker.base_url`. Add a `confluence:` block (`base_url`, `space_key`) only
     when it lives elsewhere or to scope searches to a default space.
3. **Credentials go in `env/.env`** (git-ignored), never in `appqe.yml` - the
   pre-tool guard blocks a literal secret in an edit regardless. Jira needs
   `JIRA_EMAIL` and `JIRA_API_TOKEN`; **the same pair authenticates Confluence**.
   `CONFLUENCE_EMAIL` / `CONFLUENCE_API_TOKEN` override them only for a separate
   account.
4. **Discover, do not declare.** Whatever varies per instance the client works
   out itself - the Jira AC field id is found by name from instance metadata.
   Never hardcode `customfield_XXXXX`; `JIRA_AC_FIELD` forces one as a last
   resort, not as setup.
5. **Verify before relying on it**: run the client against one real item -
   `uv run python utilities/jira_client.py <issue-key>` - and confirm content
   comes back. A source that "should work" but was never called once is how a run
   quotes an empty acceptance criterion.

A system appqe ships no starter for is wired the same way at the config level:
point `tools.<name>` at your own script or an `mcp:` tool - nothing to scaffold,
nothing more to declare.

### Fetching from Confluence

The scaffolded client takes a page three ways, and prints JSON with the page's
`title`, `space`, `url` and flattened `body`:

```bash
uv run python utilities/confluence_client.py 123456              # a page by id
uv run python utilities/confluence_client.py --title "Retry semantics"
uv run python utilities/confluence_client.py --cql "text ~ 'idempotency'"
```

`--title` and a bare `--cql` are scoped to the default space when one is set, so
a search does not sweep the whole site. Cite the page by **title and url** in the
test the way you cite a Jira issue by key - the storage-format body is flattened
to text, so quote the rule, not the markup around it.

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

## How to search - you rarely get an issue key

The request is almost always *"generate tests for /user"*, not *"for QEBS-123"*.
So **search** the sources; do not wait to be handed a ticket, and do not stop at
"no directly-linked story". The `context_gather` step hands you a `search` block
with **ready-to-run `queries`** - one per available source. Run each verbatim,
**before** you draft the plan, then fetch the promising hits:

```bash
# straight from search.queries - just run them
uv run python utilities/jira_client.py --jql "text ~ 'user' AND type in (Story, Bug)"
uv run python utilities/confluence_client.py --title "user"
```

Then fetch a promising Jira hit by key (`jira_client.py PROJ-123`), or vary the
query (`--cql "text ~ 'current user'"`) to widen a thin result.

**An error is not an empty result.** The clients take flags, so
`jira_client.py search user` exits with a usage error - which a run once
reported as "searched - no results" while the story sat one query away. A client
exiting non-zero is a FAILED step with the command and its error, never "nothing
found". Name every query run, verbatim, with hit counts (`queried`); appqe
rejects a search that cannot say what it searched.

The `sources` list is authoritative. **Never report a source as "not configured"
when it is listed available** - a run once said "no Jira configured" for a wired
Jira and never searched. The clients **load `env/.env` themselves**; a
*"Missing environment variable(s)"* exit is a credentials gap to fill, **not** an
unreachable tracker - do not report it as one.

**"No linked story" is not "no context".** A feature with no ticket may still
have a Confluence page; a bug on the endpoint (in scope via `bug_key`, or found
by the search) is a regression test waiting to be written. Conclude there is
nothing only after a query came back empty - and record *what you searched for*
in the step's summary, so the next run does not repeat a dead end.

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

<!-- appqe:managed v0.12.1 | do-not-edit-manually | regenerate: appqe sync -->
