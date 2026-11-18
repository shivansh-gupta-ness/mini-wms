---
id: TASK-002
type: task
title: API host: health endpoint, error handling and CORS
status: ready
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, tickets/TASK-002.md, docs/reports/TASK-002/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [TASK-001]
---

## Description
Make `MiniWms.Api` a running minimal-API host: a health endpoint, the standard error format, a local port and CORS for the Angular dev server. Set `status: ready` after TASK-001 is merged.

## Acceptance criteria
1. Given the API is running, when `GET /api/v1/health` is called with no session, then it returns 200 and JSON `{ "status": "ok" }`.
2. Given an unknown route, when it is called, then the response is `application/problem+json` with `status` 404 and no stack trace.
3. Given an unhandled exception in an endpoint, when it occurs, then the response is `application/problem+json` with `status` 500, a generic `title` and no exception details (NFR-3).
4. Given a request from `http://localhost:4200`, when it calls the health endpoint, then the response allows that origin; a request from any other origin is not allowed.
5. Given the default launch profile, when the API starts, then it listens on `http://localhost:5080` and the base path is `/api/v1`.
6. Given the host, when an endpoint module is added, then it registers through a `MapXxxEndpoints` extension called from `Program.cs` (one per module; health lives in `Common/`).
7. Given the API tests, when they run, then they start the real host and every test passes, with `[Trait("AC", "TASK-002.<n>")]` on those proving the ACs above.

## Out of scope
- Authentication and authorization (module AUT).
- Database access (TASK-004).
- Any module endpoint.

## Notes and links
- Record AC results in `docs/reports/TASK-002/ac-results.md` and one row in `docs/reports/ac-log.md`.
