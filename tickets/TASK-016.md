---
id: TASK-016
type: task
title: Raise the REPO_CONTEXT line limit to 500
status: done
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/MiniWms.Application.Tests/Structure/CoverageToolingTests.cs, src/backend/MiniWms.Application.Tests/Structure/SolutionStructureTests.cs, src/frontend/src/app/scaffold.spec.ts, src/frontend/src/app/tooling.spec.ts, src/frontend/src/app/proxy.spec.ts, src/frontend/src/app/features/locations/locations.guards.spec.ts, tickets/TASK-016.md, docs/reports/TASK-016/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-005-testing-coverage-and-ac-log.md]
parent: 
depends_on: []
---

## Description
The module context files are capped by tests: the backend file at under 80 lines (two tests), the frontend file at under 80 or at most 78 lines (four tests). Both files are at the cap, so every edit must be net zero lines. The owner raised the limit to at most 500 lines for each `REPO_CONTEXT.md` (2026-11-25). This ticket changes the six tests. If a file would pass 500 lines it is split into child `REPO_CONTEXT.md` files next to the code they describe, listed in the parent, so that a reader loads only the files it needs; this ticket adds the checks for that (no file is split now). The prose in the root `REPO_CONTEXT.md` and the loading rule for agents are protected and changes in a `setup/` PR that merges after this ticket.

## Acceptance criteria
1. Given `src/backend/REPO_CONTEXT.md`, when the backend tests run, then `CoverageToolingTests` and `SolutionStructureTests` accept a file of at most 500 lines and fail above that with a message that says to split the file into child `REPO_CONTEXT.md` files, and no test name or message mentions 80.
2. Given `src/frontend/REPO_CONTEXT.md`, when the frontend tests run, then the four limit assertions (`scaffold.spec.ts`, `tooling.spec.ts`, `proxy.spec.ts`, `locations.guards.spec.ts`) accept a file of at most 500 lines and fail above that with a message that says to split the file into child `REPO_CONTEXT.md` files, and no test title mentions 78 or 80 lines.
3. Given the changed tests, when read, then each keeps its existing AC id (`TASK-001.8`, `TASK-003.7`, `TASK-007.6`, `TASK-010.6`, `TASK-011.6`, `FEAT-001.27`) and nothing but the limit, its failure message, its name or title and the added `TASK-016.<n>` id changes; the only new tests are those of criterion 5.
4. Given the repository after the change, when `dotnet test src/backend`, `npm test` and both coverage gates run, then they pass, and `docs/reports/TASK-016/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output.
5. Given any `REPO_CONTEXT.md` under `src/backend` or `src/frontend` (skipping `bin`, `obj`, `node_modules` and other build output), when the tests of that side run, then a file of more than 500 lines fails, and a child file that its nearest parent `REPO_CONTEXT.md` does not list fails; the same check is proven on temporary sample trees (a valid tree passes, an over-long child fails, an unlisted child fails, a grandchild is checked against its nearest parent), because no child file exists yet.

## Out of scope
- The root `REPO_CONTEXT.md`, and the rule in the prompts, skills and agents that loads only the needed context files (protected; `setup/` PR).
- Splitting any file now: the largest has 78 lines.
- Growing the module files. 500 is a ceiling, not a target: these files are loaded into agent sessions and cost tokens.
- Old limits quoted in done tickets, past reports and `docs/learning-log.md` (historical records).
- `Claude Code Adoption Plan.md` ("under 200 lines"): general guidance, for the owner to decide.

## Notes and links
- Owner decisions (2026-11-25, on the scout's brief): the limit is 500 for each file; a file that would pass it becomes hierarchical (criteria 1 to 3 amended, criterion 5 added); the frontend tests keep counting the trailing newline, so the real maximum there is 499 lines plus the newline; `backend-platform` stays the component; only `test-writer` edits, because every changed file is a test. Both backend limit tests get `TASK-016.1` and all four frontend limit tests `TASK-016.2`.
- Places: `CoverageToolingTests.cs:222`, `SolutionStructureTests.cs:150`, `scaffold.spec.ts:134`, `tooling.spec.ts:84`, `proxy.spec.ts:52`, `locations.guards.spec.ts:103`.
- The frontend tests split the text on newlines, so a trailing newline counts as one more line.
- The ticket touches backend and frontend tests; like TASK-013 it uses the `backend-platform` component. Both components are low risk.

## Delivery
PR 58. All 5 acceptance criteria PASS (criteria 3 and 4 by diff review and real runs, no test; the backend skip of symlinks and junctions is untested); results in `docs/reports/TASK-016/ac-results.md` and the row in `docs/reports/ac-log.md`. The root limit text and the loading rule follow in the `setup/` PR.
