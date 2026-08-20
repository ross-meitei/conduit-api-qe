#!/usr/bin/env bash
# SubagentStart - constrain subagents to safe, read/test-only command categories.
_HOOKDIR="$(dirname "$0")"
. "$_HOOKDIR/appqe-resolve.sh"
APPQE_ROOT="$(appqe_project_root "$_HOOKDIR")"
payload="$(cat 2>/dev/null || true)"
# Containment: a foreign session's subagents follow their own project's rules.
appqe_event_is_foreign "$payload" "$APPQE_ROOT" && exit 0
# The briefing states the ENFORCED policy exactly (command_policy.py):
# plain curl/wget against non-prod is allowed there, and telling a subagent
# it is blocked either forgoes a legitimate API probe or - worse - teaches
# it the briefing misstates the rules when the call succeeds anyway.
CTX='Command allowlist: subagents may ONLY run, via the project package manager: '
CTX+='the API/UI test runners (in collect-only or against a non-production target), '
CTX+='the OpenAPI parse/validate helper, the linter/formatter, plain curl/wget/httpie '
CTX+='against non-production targets, and read-only git (log/diff/branch). Targets '
CTX+='must be non-production; writes to production are never allowed. BLOCKED: rm, '
CTX+='del, git push/reset, package installs, curl/wget piped to a shell or an '
CTX+='interpreter (python/node/ruby/perl) or with TLS '
CTX+='checks disabled, any destructive DB/filesystem operation, and any command '
CTX+='carrying a secret.'
printf '{"hookSpecificOutput":{"hookEventName":"SubagentStart","additionalContext":"%s"}}\n' "$CTX"
exit 0
