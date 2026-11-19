---
id: TASK-011
type: task
title: Frontend dev proxy and API health indicator
status: ready
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-011.md, docs/reports/TASK-011/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-006-angular-structure.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [TASK-002, TASK-008, TASK-009]
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
- Analysis (2026-11-19): stays `draft` until TASK-008 is merged (it needs the shell's top bar) and TASK-009 gives the API client; add TASK-009 to `depends_on` when readying, because the indicator should call `/api/v1/health` through that client.
- Analysis before readying (2026-11-19): TASK-002, TASK-008 and TASK-009 are merged, so `depends_on` now also lists TASK-009 and the indicator calls `/api/v1/health` through `ApiClient` (it prefixes the base path; do not hard-code a host). Facts: the API listens on `http://localhost:5080` (`launchSettings.json`), answers `{ "status": "ok" }` at `/api/v1/health`, and already allows the origin `http://localhost:4200` through CORS; the dev proxy (a `proxy.conf.json` set in the `serve` target of `angular.json`, forwarding `/api` to `http://localhost:5080`) makes the browser call same-origin, so AC 1 needs no CORS. The shell's top bar is in `src/app/core/layout/shell.*` (a fixed `Dev Admin` label sits there); add the indicator as its own small component in `core/layout/` and keep the `TASK-008` tests green. The indicator must show text (`API: ok` or `API: unavailable`), poll no more often than every 30 seconds, and must not trigger the global error snackbar: use `SKIP_ERROR_NOTIFICATION` from `core/errors/error.interceptor.ts` on the health request (a failing health check is shown by the indicator itself). `src/frontend/REPO_CONTEXT.md` is 74 lines and the guard allows 78, so the run-both-together instructions (AC 6) need a trim of the proxy line and one or two others. A real browser check of AC 1 to AC 3 is a manual step (start the API with `dotnet run --project src/backend/MiniWms.Api`, then `npm start`); mark it NOT RUN in the report unless it can be run. Tests use Vitest with `HttpTestingController` and fake timers for the 30-second rule; names start with `TASK-011.<n>`.
- Owner decisions (2026-11-19, on the scout's brief, recommended options taken): `HEALTH_POLL_INTERVAL_MS = 30_000` is exported from the indicator file in `core/layout/`; one 30-second `timer(0, 30000)` drives the check for both success and failure (the first check runs on load, a tick is skipped while a request is in flight, a 5-second timeout counts as unavailable); the text is exactly `API: ok` or `API: unavailable`, with nothing extra before the first answer; a 200 whose body is not `{ "status": "ok" }` shows `API: unavailable`; the indicator is a `role="status"` live region present from the start, the visible text carries the state (not only a colour); polling stops when the component is destroyed and continues while the tab is hidden; the health request sets `SKIP_ERROR_NOTIFICATION`; the browser checks of AC 1 to AC 3 are manual (owner) and are marked NOT RUN unless run; the four shell render specs get `provideHttpClient()` and `provideHttpClientTesting()` (test-only edit, no name changes). `src/frontend/REPO_CONTEXT.md` must have fewer than 80 lines counting the trailing newline (at most 77 to be safe).
