---
id: TASK-010
type: task
title: Frontend test and coverage gate
status: done
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
- Analysis before readying (2026-11-19): TASK-007 is merged. The runner is Vitest 5 with jsdom via `@angular/build:unit-test`, and `npm test` already runs once headless (AC 1 is mostly met; keep the exit-code behaviour and prove it). Coverage likely needs a new dev dependency (`@vitest/coverage-v8`, a test library: allowed, it grows the lockfile); check which provider the builder supports before choosing. AC 5 (filter by AC id) should use the builder's filter option (check `ng test --help` for `--filter`) and be proven with real runs, as TASK-003 did for the backend. The gate script follows the backend one in spirit (`src/backend/coverage.ps1`): a small Node script in `src/frontend` that reads the summary and exits non-zero below 90% naming the shortfall, with tests for pass and fail cases using fixture summaries. Only `src/app` counts; spec files and generated code are excluded. Coverage output folders must be git-ignored.
- Owner decisions (2026-11-19, on the scout's brief, recommended options taken): the npm registry may be used for this ticket only, to install `@vitest/coverage-v8` at the exact version of the installed Vitest (5.1.1) as a dev dependency, with the lockfile committed in the same PR; the gate is a small Node script `src/frontend/scripts/coverage-gate.mjs` (the root `scripts/` folder is protected) that reads the `json-summary` coverage report, with fixture tests run by `node --test` as `npm run test:gate`, separate from `npm test`; scope is everything under `src/app` except spec files and declaration files (counts `app.config.ts`, the routes and the placeholder pages; if coverage is below 90%, add tests, do not exclude code); `src/frontend/REPO_CONTEXT.md` is trimmed to stay under 80 lines (the guard stays); the name filter is `npm test -- --filter "\bTASK-010\.1\b"` style and is proven with real runs.

## Delivery
Delivered in PR 29. All 6 criteria PASS; results in `docs/reports/TASK-010/ac-results.md`, the row in `docs/reports/ac-log.md`.
