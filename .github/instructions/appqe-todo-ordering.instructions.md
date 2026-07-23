---
name: appqe-todo-ordering
description: 'Hard ordering constraint - the task-list tool is the first tool call in every appqe response, on every request in conduit-api-qe'
applyTo: '**'
---
# Todo First - the ordering constraint

> Loads on every request in **conduit-api-qe**, agent selected or not. The
> appqe agent's §0 holds the full protocol; this file exists so the one rule that
> the panel depends on is in context even before the agent is.

**The FIRST tool call in every response is `todo`.** Create the task list
before you read a file, search, run a command, edit, or ask a question. No
exceptions - a one-step task still gets a list. This is a hard ordering
constraint, not a guideline.

**Do not write the list as prose.** A markdown `- [ ]` checklist in your reply is
not the task-list tool and renders no panel - it is the exact failure this rule
prevents. Call `todo`; let the panel carry the list.

## The sequence, every time

1. **`todo` first** - create the full list, mark the first item
   `in-progress` in the same call.
2. **Then** any other tool (`todo` is created before the first `read`,
   `search`, `execute`, or `edit`).
3. **Then** `vscode_askQuestions` if you need a decision - never before the list exists.
4. Advance the list at every step transition: mark an item `completed` and the
   next `in-progress` in one call, so the panel never sits frozen.

## Why this exists

Without an explicit constraint the model calls whatever tool fits first -
`vscode_askQuestions` or a `read` - and the list appears mid-run or not at all. The
panel is the user's only live view of a long QE run; a run whose panel shows up
late, or never, is a run the user cannot follow.

## The adjective trap

"Quick", "simple", "fast", "brief", "short" in a request do not suspend the rule.
A "quick coverage audit" is still a multi-step task and gets a full list. The
list is decided by the presence of a task, not by how the user described the pace.

---

**Enforced by** appqe's workspace todo hooks - `appqe-post-tool-todo-cadence`
(flags too many tool calls between list updates) and `appqe-stop-todo-terminalize`
(blocks a turn that ends with a dangling item). The first-call *ordering*, and the
panel itself, are not hook-checkable: they depend on the task-list toolset being in
the agent's `tools:` allowlist, which `appqe doctor` cannot validate - a wrong entry
silently withholds the tool and no panel ever appears.

<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
