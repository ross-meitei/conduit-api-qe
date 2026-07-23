# SubagentStart - constrain subagents to safe, read/test-only command categories.
$ctx = 'Command allowlist: subagents may ONLY run, via the project package manager: ' +
  'the API/UI test runners (collect-only or against a non-production target), the ' +
  'OpenAPI parse/validate helper, the linter/formatter, and read-only git ' +
  '(log/diff/branch). Targets must be non-production; writes to production are never ' +
  'allowed. BLOCKED: rm, del, git push/reset, package installs, curl/wget, any ' +
  'destructive DB/filesystem operation, and any command carrying a secret.'
@{ hookSpecificOutput = @{ hookEventName = 'SubagentStart'; additionalContext = $ctx } } | ConvertTo-Json -Depth 5
exit 0
