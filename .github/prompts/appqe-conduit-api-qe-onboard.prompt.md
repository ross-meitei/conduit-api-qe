---
description: 'Set up appqe in this project, or finish setting it up - the questions only you can answer, the models your org allows, then everything else one step at a time with your approval. Safe to run again.'
agent: agent
tools: ['read', 'search', 'edit', 'execute', 'agent', 'todo', 'vscode/askQuestions']
---
Set up appqe in **conduit-api-qe**, or finish setting it up.

You are doing this *for* the user. They should not have to read appqe's docs,
learn its vocabulary, or work out which command fixes which complaint.

**appqe does the work; you run the conversation.** Every change goes through an
appqe command: detecting the project, writing `appqe.yml`, recording models,
deploying, checking. To change one setting, run `appqe config set <key> <value>`
yourself, for example `appqe config set tracker.base_url <their Jira site>`.
It keeps the file's comments and refuses a value appqe would not accept. Never
edit `appqe.yml` with a file tool, and never ask the user to edit it: a value
only they know (a URL, a project key, a path) you ask for, then set it. Never
write model lists by hand, and never redo appqe's detection with your own
guesses. Where appqe found nothing, your judgement is welcome - say it is yours.

## Step 1 - where does this project stand?

```
appqe onboard --agent
```

It prints JSON. Its `state` says where the project stands - appqe decides, not
you:

- **`new` or `seeded`** (nothing yet, or the untouched starter `appqe init`
  wrote): go to step 2 with the questions it printed.
- **`configured`:** someone already set it up. Do not ask the setup questions
  and do not replace anything. Go to step 3. If doctor then shows nothing
  deployed for this agent, that is the normal next step after `appqe init` or a
  reset, not a problem: say so and offer the one command.
- **`elsewhere`:** this folder has no `appqe.yml`, but a folder inside it does
  (a monorepo). Do what its `next` says: ask the user which they mean, and never
  create a second setup without their yes.

**If the shell says `appqe` is not found,** appqe may be installed where this agent's
terminal cannot see it (editors often start without your login PATH). Try these in
order, and use the first one that works for every later command:

1. `"$APPQE_BIN" onboard --agent`, if the `APPQE_BIN` variable is set.
2. `~/.local/bin/appqe onboard --agent`, where `uv tool install` puts it by default.
3. `"$(uv tool dir --bin)/appqe" onboard --agent`, if `uv` itself is found.

On Windows PowerShell the same three are `& $env:APPQE_BIN onboard --agent`,
`& "$env:USERPROFILE\.local\bin\appqe.exe" onboard --agent` and
`& "$(uv tool dir --bin)\appqe.exe" onboard --agent`.

Never use `uvx appqe` or `uv tool run appqe`: if appqe is not installed, those download
whatever package of that name the public index has.

If none of them works, tell the user appqe is not installed for this account and give
them the install command: `uv tool install "appqe @ git+<the repo your team uses>"`.
Then ask them to restart the editor so its terminal picks up the new PATH. Do not
try to install it yourself.

## Step 2 - the questions only the user can answer

Each question comes with appqe's detected answer as the default. For each one
that applies (check its `when`):

- If appqe detected an answer, show it and ask the user to confirm or correct it.
  Do not ask open questions appqe has already answered.
- Ask as few rounds as you can: several questions in one `vscode/askQuestions` call is fine.
- **Coding agents** (`agents`): ask which coding agents their team uses in this
  project. More than one is normal, for example Claude Code and Copilot. appqe
  sets up every one they name.
- **Models** (`models_available`): you cannot read the model picker; only the user
  can. For each of Claude Code and Copilot that is in `agents`, list the models
  from `choices_by_host`, marking the ones `used_here`, and ask which ones their
  picker shows in that agent. Answer as an object keyed by agent, for example
  `{"claude": ["opus-5.5"], "copilot": ["sonnet-5"]}`. Leave out any they are
  unsure of. Skipping is fine: the coding agent then picks, and appqe says so.

Write the replies as one JSON object keyed by each question's `key` to the
`answers_file` it names (`.appqe/local/onboard-answers.json`, kept out of git).
Leave out, or leave blank, anything the user accepted as the default. Then run:

```
appqe onboard --answers .appqe/local/onboard-answers.json
```

That one command writes `appqe.yml`, records the models, wires what it can,
deploys and checks. Show the user its summary in plain words. If it says a step
was skipped, pass on the command it gives for later.

## Step 3 - finish anything still open

```
appqe doctor
```

It prints a line per item: `[OK]` done, `[--]` optional and not set up, `[!!]`
would stop the guardrails working. **If everything is `[OK]`:** say so in one
sentence and go to step 4.

Otherwise take `[!!]` first, then `[--]`, one at a time. For each, tell the user in
plain words what it means *for them*, propose the fix and wait. Never run more than
one fix per approval, and never batch them into "shall I fix everything?".

A role marked "not pinned" means appqe has no evidence which models this org allows.
Offer /appqe-conduit-api-qe-models, which records them.

### The items you will actually see, and what to do

**"the running agent matches the synced host"** - they are using a coding agent
this project is not set up for, so appqe's hooks and content for their agent are
not installed: the guard, the progress panel and the run updates do not fire.
Ask whether to add their agent. If yes: `appqe sync --host <the running agent>`.
It installs the content and adds the agent to the project's list, keeping the
others. Then they commit `appqe.yml` and the new folder for the rest of the team.

**"content is deployed for every listed agent"** - the project lists an agent
that has nothing deployed. `appqe sync`, then commit what it wrote.

**"hook scripts deployed" / "hook descriptors present" / "the appqe agent
contract is deployed"** - content is missing for this host. `appqe sync --host
<agent>`. When all of them are missing together, nothing has been deployed for
this agent yet, which is the normal state right after `appqe init` or a reset.
Say "appqe is configured here but not yet deployed for <agent>; `appqe sync
--host <agent>` deploys it", and ask once. Do not call it broken or a problem:
the guardrails are declared and start firing the moment the sync runs. Only
when SOME content is present and some missing is something actually wrong.

**"deployed content matches the installed CLI"** failing - the project holds
files from an older appqe. `appqe sync --prune`. Mention that `--prune` removes
files this version no longer ships.

**"conventions map recorded"** - appqe has not learned this suite's own
fixtures and helpers, so every run re-reads them into context. Offer
`appqe conventions discover`. Worth saying: this is the item that costs real
money per run, not just tidiness.

**"coverage baseline recorded"** - there is nothing for CI to compare against,
so a coverage regression cannot be caught. Offer
`appqe coverage --kind api --check-baseline` (use `--kind ui` for a UI
project), and say the file should be committed.

**"no target is configured"** - `api.description` and `ui.base_url` are both
unset. Only the user knows this one, so do not guess it. Ask where their API
description lives (an OpenAPI or Swagger spec, a Postman collection or a GraphQL
schema all work), or whether this is a UI project and what its base URL is.
No spec yet is fine: say so, leave `api.description` blank, and the first API
test run has the agent write `.appqe/contract.yml` from what the team has.
Otherwise set it, for example `appqe config set api.description docs/openapi.json`
or `appqe config set ui.base_url <their app's URL>`. If they do not
know, say what appqe can still do without it (generation runs and degrades;
coverage cannot be measured against a contract) and move on.

**A requirement source is not wired** - they have Jira, Confluence or GitHub
named in config with no client. `appqe tools add <source>`. Say that it also
needs a credential, and that appqe never stores one: it writes a `.env.example`
naming the variables and they fill in the real values.

**"N project skill(s) found, 0 bound to a step"** - their team's own skills are
not set up to be followed consistently. Do not handle this here. Say one
sentence and hand over:

> You also have a skill of your own that runs do not follow consistently yet.
> There is a command that sets that up - shall I run it after this?

Then invoke the `use-our-skills` command. Find its exact name in your own
command list: it is `appqe-use-our-skills`, or `appqe-<project>-use-our-skills`
on a host that scopes commands per project. Do not guess the name at the user;
look it up, or just run the steps that command describes yourself.

**"a skill was written by an older appqe"** - a leftover file appqe wrote under
a name it no longer uses. It is safe to delete and nothing puts it back. Show
the exact path, ask, and only then `rm -rf` it. Never delete a directory
without explicit approval, and never one outside the path the check named.

## Step 4 - show what changed

Re-run `appqe doctor`. Report in two or three sentences: what is now `[OK]`,
what is deliberately left, and what needs a decision from them later.

Then tell them what to commit, using the list onboarding printed (it starts
"commit appqe.yml"). Once that is committed, a teammate only installs appqe and
opens their coding agent: the commands, guardrails and model pins are already
there.

Then one offer, if and only if everything else is settled:

> One last thing - appqe arrives with its own opinions about how tests are
> written. If any of them do not match how your team works, I can change them
> so yours win. Worth doing now?

If yes, invoke the `our-standards` command (look its exact name up in your own
command list - it is `appqe-our-standards`, or `appqe-<project>-our-standards`
on a host that scopes commands per project). If no, stop - do not start a QE
run.

## Rules

- **One fix per approval.** No batching.
- **Never edit their tests, their skills, or any file appqe did not write.**
- **If appqe itself looks wrong** - a check that contradicts what you can see -
  show the user appqe's exact output, say what you expected, and suggest they
  report it to whoever maintains appqe for their team. Never read, debug or
  change appqe's own code, even if it is on disk: this project uses appqe, it
  does not develop it. Then carry on with the next item.
- **Do not use appqe's words with the user.** No "binding", no "phase", no
  "evidence contract". Say *which steps follow this*, *the check appqe runs
  before a step counts*, *the same way every time*.
- **If a fix fails, stop and show the error.** Do not try a different command
  to route around it; that is how a project ends up in a state nobody chose.
- An item the user declines is a decision, not a failure. Note it and move on.
- **Run again at any time.** Finished steps are kept, not redone; say which.


> ## Task panel - set this up first
>
> **Before you execute any step**, run the `progress.todo_call` line
> that `appqe start` returns. It is a complete `manage_todo_list` call
> with the whole run already written into it - run it as-is.
>
> **Never build the items yourself, and never build the panel out of
> `actions`.** `actions` is what you execute; `progress.todo_call` is
> what you display. They are different lengths on purpose - a 14-step
> run showing a 3-item panel is one batch's actions in the window where
> the run should be.
>
> Re-run `progress.todo_call` on every batch: it carries the current
> step as `in-progress` and settled ones as `completed`, and the tool
> replaces the list it is given, so the fresh line is always the whole
> truth. On the last batch run it once more, with everything done.
>
> If `manage_todo_list` is not in your tool list, it is not there - do
> not search for it. Say so in one line above the opener in
> your first reply - once for the whole run - and carry on. The status
> block in each reply is the step list either way. **The user must type
> `#todo` in their own message**; nothing a project file can do grants
> it, so naming that is the only useful thing you can say about it.

<!-- appqe:managed v0.20.0 h:2f50b42508e2 | do-not-edit-manually | regenerate: appqe sync -->
