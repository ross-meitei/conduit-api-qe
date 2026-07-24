---
description: 'Investigate a failing API or UI test and deliver a root-cause verdict.'
agent: appqe-agent
---
Investigate the failing test for **conduit-api-qe**.

Drive it through the orchestrator: `appqe start "investigate <paste the failure>"`,
then execute each phase and `appqe resume` with results.

- Read the discovery log first - the failure may already be explained.
- Reproduce and capture the exact error (status/body for API; trace/screenshot for
  UI). Redact secrets.
- Triage: **test defect** vs **app defect** vs **environment/flake** (see the
  flaky-triage skill).
- Verify the root cause with evidence; run a challenge step to disprove it.
- Deliver a verdict + recommended fix; update the discovery log.

<!-- appqe:managed v0.1.0 | do-not-edit-manually | regenerate: appqe sync -->
