---
id: TASK-014
type: task
title: Limit the request body size and prove the Development 500
status: ready
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/MiniWms.Api/**, src/backend/MiniWms.Api.Tests/**, src/backend/REPO_CONTEXT.md, tickets/TASK-014.md, docs/reports/TASK-014/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: []
---

## Description
Phase 6 hardening, part 2 (security review 2026-11-20, finding L5 and the follow-up for I7). The API reads a request body with `ReadFromJsonAsync` under the framework default of 30 MB and fully parses it before the length validators run, so a huge `name` costs memory and time before the 400. Set a small request size limit, make an oversize body return the standard problem response, and add the missing Development-environment test that an unhandled error still returns the generic 500.

## Acceptance criteria
1. Given any API route that reads a body, when the request body is larger than 64 KB (declared by `Content-Length` or streamed), then the response is 413 `application/problem+json` with the standard fields (`type`, `title`, `status`, `traceId`), no `detail` and no stack trace, and the body is not read to the end.
2. Given a valid request whose body is just under the limit (for example a zone with a 100-character name and padding fields within the 64 KB), when it is sent, then it still succeeds as before.
3. Given the host running in the Development environment, when an unhandled exception is thrown by a route, then the response is the generic 500 problem (`An unexpected error occurred.`) with no exception message, stack trace or `detail`, proving that the developer exception page never shows.
4. Given the existing API tests, when they run with the new limit, then all of them still pass and none needs its expectation changed.
5. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-014.<n>")]` on those proving the ACs above, the coverage gate holds (90% over Domain, Application and Api), and `src/backend/REPO_CONTEXT.md` mentions the limit and stays under 80 lines.

## Out of scope
- Security headers, HTTPS redirection and config-driven CORS (findings I2 and I3: only needed before any deployment).
- Authentication and request-rate limits.
- The harness and CI findings (a separate owner-led `setup/` PR).

## Notes and links
- **Decision 1 (limit):** recommended 64 KB for every route (the largest legitimate body is a few hundred bytes); a global Kestrel `MaxRequestBodySize` plus the per-request feature check. Alternative: per-endpoint `RequestSizeLimit` metadata on each `MapXxx` (more code, and a new endpoint could forget it). Recommended: global.
- **Decision 2 (mapping):** an oversize body raises `BadHttpRequestException` (413) from the request body reader; the existing problem-details setup must turn it into problem+json with `type` `error` and status 413 without echoing any text. Check how it behaves today (the review could not verify it) before changing the handlers; keep the middleware order and the problem-details customizer behaviour for every other status.
- Tests can use the in-process `ApiFactory` (temporary database, never `App_Data`); a streamed body without a `Content-Length` header must be covered too, because the limit then triggers while reading.
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).
- Record AC results in `docs/reports/TASK-014/ac-results.md` and one row in `docs/reports/ac-log.md`.
