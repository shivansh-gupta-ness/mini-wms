---
id: TASK-004
type: task
title: Frontend dev proxy and API health indicator
status: draft
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-004.md, docs/reports/TASK-004/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-006-angular-structure.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [TASK-001, TASK-003]
---

## Description
Connect the Angular dev server to the local API: a dev proxy for `/api` and a small API status indicator in the shell that calls `GET /api/v1/health`. This proves the frontend and backend run together locally. Set `status: ready` after TASK-001 and TASK-003 are merged.

## Acceptance criteria
1. Given `npm start` and the API running on `http://localhost:5080`, when the app calls `/api/v1/health`, then the dev proxy forwards it and the call succeeds without CORS errors.
2. Given the API answers `{ "status": "ok" }`, when the shell loads, then the top bar shows "API: ok" with a text label, not only a colour.
3. Given the API is not running, when the shell loads, then the top bar shows "API: unavailable", and the app stays usable.
4. Given the indicator, when it fails twice, then it retries no more than once every 30 seconds.
5. Given `npm test` and `npm run build`, when they run, then all tests pass, the build succeeds, and each AC-proving test starts with its ID (`TASK-004.<n>`).
6. Given `src/frontend/REPO_CONTEXT.md`, when read, then it describes how to run the API and the frontend together locally.

## Out of scope
- Real module screens.
- CORS configuration (done in TASK-001).
- Authentication.

## Notes and links
- Proxy: `src/frontend/proxy.conf.json` and the `start` script. No hard-coded API host in application code (ADR-006).
- Record AC results in `docs/reports/TASK-004/ac-results.md` and one row in `docs/reports/ac-log.md`.
