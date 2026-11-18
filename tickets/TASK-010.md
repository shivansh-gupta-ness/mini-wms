---
id: TASK-010
type: task
title: Frontend test and coverage gate
status: draft
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-010.md, docs/reports/TASK-010/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-005-testing-coverage-and-ac-log.md]
parent: 
depends_on: [TASK-007]
---

## Description
Make the frontend tests produce coverage and enforce the 90% gate from ADR-005. Set `status: ready` after TASK-007 is merged.

## Acceptance criteria
1. Given `npm test`, when it runs once (not in watch mode), then it exits with the pass or fail status and prints the results.
2. Given the test command with coverage, when it runs, then a coverage summary is printed and written under a git-ignored folder.
3. Given coverage scope, when the report is built, then only `src/app` is counted and test files and generated code are excluded.
4. Given line coverage below 90%, when the gate runs, then it exits non-zero and names the shortfall; at or above 90% it exits zero.
5. Given a test whose name starts with an AC ID, when the tests run with a name filter on that ID, then only the tests for that AC run.
6. Given `src/frontend/REPO_CONTEXT.md`, when read, then it lists the test, coverage and gate commands.

## Out of scope
- Backend coverage (TASK-003).
- End-to-end tests.

## Notes and links
- Record AC results in `docs/reports/TASK-010/ac-results.md` and one row in `docs/reports/ac-log.md`.
