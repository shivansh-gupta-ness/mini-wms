---
id: TASK-003
type: task
title: Backend test and coverage tooling with 90% gate
status: done
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, .config/dotnet-tools.json, tickets/TASK-003.md, docs/reports/TASK-003/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-005-testing-coverage-and-ac-log.md]
parent: 
depends_on: [TASK-001]
---

## Description
Add the coverage tooling ADR-005 requires: coverlet in the test projects, ReportGenerator as a local tool, one documented command that runs the tests with coverage and prints the numbers, and a gate that fails below 90% line coverage. Set `status: ready` after TASK-001 is merged.

## Acceptance criteria
1. Given `.config/dotnet-tools.json`, when `dotnet tool restore` runs, then ReportGenerator is installed from the local manifest.
2. Given the three test projects, when `dotnet test src/backend --collect:"XPlat Code Coverage"` runs, then a Cobertura coverage file is produced for each.
3. Given the coverage files, when the documented report command runs, then a text summary with per-assembly line coverage is written under `TestResults/` (git-ignored) and printed.
4. Given coverage scope, when the report is built, then migrations, `Program.cs` and generated code are excluded and only Domain, Application and Api are counted (ADR-005).
5. Given line coverage below 90%, when the gate command runs, then it exits non-zero and names the shortfall; at or above 90% it exits zero.
6. Given the ACs convention, when a test carries `[Trait("AC", "<id>")]`, then running `dotnet test --filter "AC=<id>"` runs exactly the tests for that AC.
7. Given `src/backend/REPO_CONTEXT.md`, when read, then the test, coverage and gate commands are listed.

## Out of scope
- Frontend coverage (TASK-010).
- A CI workflow (none exists by design).
- The AC-to-test mapping script (owner change in `scripts/`).

## Notes and links
- Record AC results in `docs/reports/TASK-003/ac-results.md` and one row in `docs/reports/ac-log.md`.
