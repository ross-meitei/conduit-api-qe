# SessionStart - inject the project's appqe QE context once per session.
$ctx = 'appqe QE session. Source of truth for API tests is the OpenAPI spec; verify ' +
  'endpoints/schemas/status codes against it (zero assumptions). Generated tests must ' +
  'not hardcode secrets or production URLs, must assert status + schema, and UI tests ' +
  'must use web-first assertions with no hard waits. Stop at approval gates.'
@{ hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $ctx } } | ConvertTo-Json -Depth 5
exit 0
