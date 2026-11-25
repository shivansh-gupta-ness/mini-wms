---
id: TASK-016
type: task
title: Raise the REPO_CONTEXT line limit to 500
status: ready
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/MiniWms.Application.Tests/Structure/CoverageToolingTests.cs, src/backend/MiniWms.Application.Tests/Structure/SolutionStructureTests.cs, src/frontend/src/app/scaffold.spec.ts, src/frontend/src/app/tooling.spec.ts, src/frontend/src/app/proxy.spec.ts, src/frontend/src/app/features/locations/locations.guards.spec.ts, tickets/TASK-016.md, docs/reports/TASK-016/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-005-testing-coverage-and-ac-log.md]
parent: 
depends_on: []
---

## Description
The module context files are capped by tests: the backend file at under 80 lines (two tests), the frontend file at under 80 or at most 78 lines (four tests). Both files are at the cap, so every edit must be net zero lines. The owner raised the limit to at most 500 lines for each `REPO_CONTEXT.md` (2026-11-25). This ticket changes the six tests. The prose in the root `REPO_CONTEXT.md` is protected and changes in a `setup/` PR that merges after this ticket.

## Acceptance criteria
1. Given `src/backend/REPO_CONTEXT.md`, when the backend tests run, then `CoverageToolingTests` and `SolutionStructureTests` accept a file of at most 500 lines and fail above that, and no test name or message mentions 80.
2. Given `src/frontend/REPO_CONTEXT.md`, when the frontend tests run, then the four limit assertions (`scaffold.spec.ts`, `tooling.spec.ts`, `proxy.spec.ts`, `locations.guards.spec.ts`) accept a file of at most 500 lines and fail above that, and no test title mentions 78 or 80 lines.
3. Given the changed tests, when read, then each keeps its existing AC id (`TASK-001.8`, `TASK-003.7`, `TASK-007.6`, `TASK-010.6`, `TASK-011.6`, `FEAT-001.27`) and nothing but the limit, its name or title and the added `TASK-016.<n>` id changes.
4. Given the repository after the change, when `dotnet test src/backend`, `npm test` and both coverage gates run, then they pass, and `docs/reports/TASK-016/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output.

## Out of scope
- The root `REPO_CONTEXT.md` (protected; `setup/` PR).
- Growing the module files. 500 is a ceiling, not a target: these files are loaded into agent sessions and cost tokens.
- Old limits quoted in done tickets, past reports and `docs/learning-log.md` (historical records).
- `Claude Code Adoption Plan.md` ("under 200 lines"): general guidance, for the owner to decide.

## Notes and links
- Places: `CoverageToolingTests.cs:222`, `SolutionStructureTests.cs:150`, `scaffold.spec.ts:134`, `tooling.spec.ts:84`, `proxy.spec.ts:52`, `locations.guards.spec.ts:103`.
- The frontend tests split the text on newlines, so a trailing newline counts as one more line.
- The ticket touches backend and frontend tests; like TASK-013 it uses the `backend-platform` component. Both components are low risk.
