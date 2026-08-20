---
name: appqe-run-report
description: 'Close out a QE run - combine appqe telemetry with the actual QE outcome into a report a client can read. Use at the end of a run or when asked "what happened".'
metadata: {version: 1.0.0, access: research}
---
# Skill: Run report

A run is not finished when the last action returns. It is finished when someone
who was not watching can tell what changed and whether it is safe to ship.

## Gather

```bash
appqe telemetry summary <run_id>   # actions, failures, duration
appqe status <run_id>              # phase state, gates, completion
appqe coverage --covered N --total M   # against 80%
```

Telemetry is local, append-only JSONL stored beside the run state. Nothing leaves
the machine.

## Report

Lead with the outcome, not the process:

- **What was produced** - tests added/changed, by area, and where they live.
- **Coverage** - covered/total endpoints or flows against the threshold, and
  explicitly *which* are still uncovered. A percentage without the gap list is
  not actionable.
- **What failed** - failing tests separated into "product defect" and "test
  defect". Never merge those two; they go to different people.
- **Gates** - which approvals were given, by whom, and what they authorised.
- **Guardrail blocks** - each one, and whether it was a genuine finding.
- **Residual risk** - what is still untested and what could hide there.

## Label every number's provenance

The run summary carries a `provenance` map, one entry per completed phase:

| Label | Means |
|---|---|
| `measured` | appqe parsed an artifact and derived the number itself |
| `observed` | the agent submitted structured evidence appqe checked |
| `agent_asserted` | the phase completed on the agent's word alone |

Carry those labels into the report. A reader assumes a reported number was
measured unless told otherwise, and that assumption is how an untested change
ships. "10 tests passing (measured, TestResults/results.trx)" and "10 tests
passing (asserted)" are different claims and must not read alike.

If any phase is `agent_asserted`, say so in the first line alongside coverage -
not in a footnote.

## Rules

State the numbers plainly. If coverage is below
80%, say so in the first line rather than burying
it - a threshold quietly missed is how a gap reaches production.

Never report a test as passing that you did not see pass. "Generated" is not
"passing"; say which it is. If a run ended early, report it as incomplete and
give the `run_id` so it can be resumed.

Redact tokens, credentials, and customer identifiers from any quoted output.

<!-- appqe:managed v0.12.11 | do-not-edit-manually | regenerate: appqe sync -->
