---
id: TASK-015
type: task
title: Add nosniff and no-store headers to API responses
status: done
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/MiniWms.Api/**, src/backend/MiniWms.Api.Tests/**, src/backend/REPO_CONTEXT.md, tickets/TASK-015.md, docs/reports/TASK-015/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: []
---

## Description
Phase 6 hardening left response headers open (security review, finding I2: "only needed before any deployment"). This is the smallest useful part of it and needs no deployment decision: every API response carries `X-Content-Type-Options: nosniff` (a browser must not guess the content type of a JSON or problem response) and `Cache-Control: no-store` (warehouse data must not be kept by a browser or proxy cache). One small middleware, no new package, no configuration.

## Acceptance criteria
1. Given any API route (a success, a 404 and a 400), when a request is sent, then the response has `X-Content-Type-Options: nosniff`.
2. Given any API route (a success, a 404 and a 400), when a request is sent, then the response has `Cache-Control: no-store`.
3. Given an error that the exception handlers turn into a problem response (400, 409, 413 and the generic 500), when it is returned, then both headers are still present and the problem body is unchanged.
4. Given the CORS preflight and the existing CORS tests, when they run, then they pass unchanged (the new headers do not remove or replace any CORS header).
5. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-015.<n>")]` on those proving criteria 1 to 4, no existing test needs its expectation changed, the coverage gate holds (90% over Domain, Application and Api), `src/backend/REPO_CONTEXT.md` mentions the headers and stays under 80 lines, and `docs/reports/TASK-015/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output.

## Out of scope
- HTTPS redirection, HSTS, `Content-Security-Policy` and config-driven CORS (finding I3 and the rest of I2: needed only before a deployment).
- The Angular dev server and the static files of the frontend.
- ADR-003 (a one-line note about the headers goes in a later docs PR).

## Notes and links
- Put the middleware next to `Common/RequestLimits` (for example `Common/Headers/`), registered early in `Program.cs` so that error and status-code responses also get the headers; mind the order with `UseCors` and the exception handler (headers set before `next()` are kept when a handler rewrites the response, but check how `UseExceptionHandler` clears headers and use `OnStarting` if needed).
- Tests use the in-process `ApiFactory` (temporary database, never `App_Data`).
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).
- Record AC results in `docs/reports/TASK-015/ac-results.md` and one row in `docs/reports/ac-log.md`.
- The ticket was rewritten on 2026-11-23 at the owner's request: it was a docs-only ticket (first-run result in the AC log), now it is this small code task. The first-run idea is dropped.

## Delivery
PR 54. All 5 acceptance criteria PASS (in-process proof; no real-browser or running-Kestrel check); results in `docs/reports/TASK-015/ac-results.md` and the row in `docs/reports/ac-log.md`.
