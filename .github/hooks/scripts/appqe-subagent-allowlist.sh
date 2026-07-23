#!/usr/bin/env bash
# SubagentStart - constrain subagents to safe, read/test-only command categories.
CTX='Command allowlist: subagents may ONLY run, via the project package manager: '
CTX+='the API/UI test runners (in collect-only or against a non-production target), '
CTX+='the OpenAPI parse/validate helper, the linter/formatter, and read-only git '
CTX+='(log/diff/branch). Targets must be non-production; writes to production are '
CTX+='never allowed. BLOCKED: rm, del, git push/reset, package installs, curl/wget, '
CTX+='any destructive DB/filesystem operation, and any command carrying a secret.'
printf '{"hookSpecificOutput":{"hookEventName":"SubagentStart","additionalContext":"%s"}}\n' "$CTX"
exit 0
