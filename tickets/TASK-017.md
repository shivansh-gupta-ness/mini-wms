---
id: TASK-017
type: task
title: Close the test gaps in the security header tests
status: done
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/MiniWms.Api.Tests/Host/SecurityHeadersTests.cs, tickets/TASK-017.md, docs/reports/TASK-017/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-003-api-conventions.md, docs/adr/ADR-005-testing-coverage-and-ac-log.md]
parent: 
depends_on: []
---

## Description
TASK-015 added `X-Content-Type-Options: nosniff` and `Cache-Control: no-store` to every API response. Reviews of its tests (`/independent-code-review` and `/test-critique`, 2026-11-25 and 2026-11-26) found no defect in the code, but found cases the tests do not prove. The important one: TASK-015 criterion 3 names the 413 made by an exception handler, and the only 413 header test sends a declared `Content-Length`, which `RequestBodyLimitMiddleware` answers itself, so the handler never runs. This ticket adds and strengthens tests only. No production code changes.

## Acceptance criteria
1. Given a chunked `POST /api/v1/zones` body over 65,536 bytes with no `Content-Length` (so `RequestBodyTooLargeExceptionHandler` writes the answer), when it is sent, then a test asserts status 413, media type `application/problem+json`, both headers, exactly the keys `status`, `title`, `traceId` and `type`, the type `error` and the title `Request body too large.`.
2. Given a host that runs in the `Production` environment, when `/api/v1/health` (200), an unknown path (404) and a throwing route (500) are requested, then tests assert the status and both headers on each, and that the environment really is `Production`; the 404 and the 500 have the problem body (types `not-found` and `error`, the 500 title `An unexpected error occurred.`, no `detail`, no exception text); and the host's database file does not exist after it starts, because only Development migrates and seeds.
3. Given Locations success responses, when `GET /api/v1/warehouse`, `GET /api/v1/zones`, `GET /api/v1/bins` and `GET /api/v1/locations/tree` (200), `POST /api/v1/zones` (201) and `PATCH /api/v1/zones/{id}` (200) are requested, then tests assert the status and both headers on each.
4. Given an endpoint that sets its own `X-Content-Type-Options` and `Cache-Control` values, when it answers, then a test asserts that the response has exactly one value of each, `nosniff` and `no-store`.
5. Given the existing `..._unchanged_body` tests, when they run, then they assert the exact body of each case: the 400 its title (`One or more validation errors occurred.`) and the keys under `errors`; the real 409 its title (`Conflict`) and its `detail` (`A zone with active bins cannot be deactivated.`); the status-code-page 409 its HTTP status; the declared-length 413 its media type, `type` and `status`.
6. Given the tests, when they run, then all pass; new tests carry `[Trait("AC", "TASK-017.<n>")]`; strengthened tests keep every existing `TASK-015` trait, gain their `TASK-017.<n>` trait and lose no assertion; only `SecurityHeadersTests.cs` changes under `src/`; `dotnet test src/backend` and the 90% coverage gate pass; and `docs/reports/TASK-017/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output.
7. Given criteria 1 to 5, when the matching production line is broken on purpose in a throwaway copy of the repository (the mutations listed below), then at least one test of that criterion fails, and the report names the failing tests; the working tree is never changed for this.

## Out of scope
- Any production code change. If a new test fails on the current code it has found a real defect: stop and ask the owner (a BUG ticket).
- `ApiFactory.cs` and the other test files. New hosts are private nested classes in `SecurityHeadersTests.cs`.
- The preflight `Access-Control-Allow-Methods` and `-Headers` values: existing CORS lines, a separate CORS test ticket if wanted.
- Test style: replacing the `switch` in `SendKindAsync`, merging theories, renaming or removing the tag-guard test (no added protection).
- `docs/specs/api/locations.yaml` does not declare 413 (found by `/spec-drift`): a separate ticket.
- Real Kestrel and browser checks.

## Notes and links
- Owner decisions (2026-11-26, on the scout's brief): (1) the 404 and both 500 `unchanged` tests stay as they are, so only the generic 500 pins its title there; (2) three mutations are added for criterion 7, each alone: (6) in the callback of `SecurityHeadersMiddleware`, return early for `GET` requests to `/api/v1/zones` (criterion 3); (7) the status-code-page 409 is sent with another HTTP status while its body still says 409 (criterion 5); (8) the declared-length 413 is sent with another media type, and separately with another `type` (criterion 5); (3) criteria 6 and 7 have no tagged tests, so the mapping is 5 of 7 (71.4%), by design; (4) the throwaway copy is a clone outside the repository, deleted afterwards; only `test-writer` edits, because the only source file is a test.
- Owner decisions (2026-11-26, after `/own-code-review`, `/independent-code-review` and `/test-critique`): criterion 2 gains the Production 404 and 500 bodies and the database check, criterion 3 gains `GET /api/v1/bins`; the test of a successful deactivate is not added (one more route of a global middleware); the endpoint-header test is renamed and the class summary names TASK-017. Mutations added for criterion 7, each alone: (9) `UseStatusCodePages` only in Development (criterion 2); (10) the fixed 500 title only in Development (criterion 2); (11) migrate and seed in every environment (criterion 2); (12) in the callback, return early for `/api/v1/bins` (criterion 3).
- Mutations for criterion 7, each alone: (1) in the `OnStarting` callback of `SecurityHeadersMiddleware`, return early when the status is 413 and the request has no `Content-Length`; (2) register the middleware in `Program.cs` only in Development; (3) in the callback, return early when the status is 201, and separately, skip `PATCH` requests and the paths `/api/v1/warehouse` and `/api/v1/locations`; (4) set `X-Content-Type-Options` with `TryAdd`, and separately with `Append`; (5) change the 400 title, the 409 title and the 409 detail text.
- Reuse: `CountingSourceStream` and the chunked request in `RequestBodyLimitTests.cs` (same assembly); `AssertBothHeaders`, `AssertProblem` and `EndpointCacheControlApiFactory` in `SecurityHeadersTests.cs`. `ApiFactory.ConfigureWebHost` is sealed: set the environment in `ConfigureTestHost`. `ThrowingApiFactory` is sealed, so the Production host needs its own throwing route.
- Every host in the current tests runs in Development: `WebApplicationFactory` defaults to it and no test sets `Production`. Migrations and the seed run only in Development (`Program.cs`), so a Production test must not touch the database. `/api/v1/health` does not (`HealthEndpoints.cs`).
- Follows `tickets/TASK-015.md` (PR 54). The throwaway copy for criterion 7 is a clone or worktree outside the working tree, made and deleted by the main session with the owner's approval of the plan.
- Tests use the in-process host with its temporary database, never `App_Data`.
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).

## Delivery
PR 63. All 7 acceptance criteria PASS (criteria 6 and 7 by diff review and real runs, no test; 18 of 18 mutations caught); results in `docs/reports/TASK-017/ac-results.md` and the row in `docs/reports/ac-log.md`. No production defect was found. `/code-review` was not run.
