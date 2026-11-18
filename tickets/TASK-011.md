---
id: TASK-011
type: task
title: Frontend dev proxy and API health indicator
status: draft
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-011.md, docs/reports/TASK-011/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-006-angular-structure.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [TASK-002, TASK-008]
---

## Description
Connect the Angular dev server to the local API: a dev proxy for `/api` and an API status indicator in the shell. This proves frontend and backend run together locally. Set `status: ready` after TASK-002 and TASK-008 are merged.

## Acceptance criteria
1. Given `npm start` and the API on `http://localhost:5080`, when the app calls `/api/v1/health`, then the dev proxy forwards it and the call succeeds without CORS errors.
2. Given the API answers `{ "status": "ok" }`, when the shell loads, then the top bar shows "API: ok" as text, not only a colour.
3. Given the API is not running, when the shell loads, then the top bar shows "API: unavailable" and the app stays usable.
4. Given the indicator fails, when it retries, then it retries no more than once every 30 seconds.
5. Given the tests and the build, when `npm test` and `npm run build` run, then they pass and each AC-proving test name starts with `TASK-011.<n>`.
6. Given `src/frontend/REPO_CONTEXT.md`, when read, then it explains how to run the API and the frontend together locally.

## Out of scope
- Real module screens.
- CORS configuration (TASK-002).

## Notes and links
- Record AC results in `docs/reports/TASK-011/ac-results.md` and one row in `docs/reports/ac-log.md`.
