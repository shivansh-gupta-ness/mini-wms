# TASK-002 acceptance-criteria results

Ticket: `tickets/TASK-002.md` (API host: health endpoint, error handling and CORS). Branch: `task/TASK-002-api-host-health-endpoint`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval, `change-implementer` (plan steps 1 to 4) and `test-writer` (step 5), each in its own isolated worktree. Their work was carried back with `worktree-apply.mjs`. The numbers below are from my own re-run in the main working tree after both hand-offs, not from the agents' summaries.

| Command | Result |
|---|---|
| `dotnet build src/backend` | Build succeeded. 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | `MiniWms.Api.Tests`: Passed 12, Failed 0. `MiniWms.Application.Tests`: Passed 14, Failed 0. `MiniWms.Domain.Tests`: no tests yet ("No test is available"). Exit code 0 |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | `GET /api/v1/health` with no session returns 200 and `{ "status": "ok" }` | PASS | `Health_returns_200_with_status_ok_json_and_no_auth` | Passed against the real host (WebApplicationFactory) | 2026-11-19 |
| 2 | Unknown route returns `application/problem+json`, status 404, no stack trace | PASS | `Unknown_route_returns_404_problem_json`, `Unknown_route_body_has_no_detail_and_no_stack_trace` | 2 tests passed | 2026-11-19 |
| 3 | Unhandled exception returns `application/problem+json`, status 500, generic title, no exception details | PASS | `Unhandled_exception_returns_500_problem_json_with_generic_title`, `Unhandled_exception_body_has_no_detail_and_no_exception_text` | 2 tests passed. The throwing route is a test-only route added after the host's pipeline, so the exception passes through the host's own error handler; the secret text of the exception never appears in the body | 2026-11-19 |
| 4 | A request from `http://localhost:4200` is allowed; any other origin is not | PASS | `Angular_dev_origin_is_allowed`, `Other_origin_is_not_allowed` | 2 tests passed: the allowed origin gets `Access-Control-Allow-Origin: http://localhost:4200`, another origin gets no such header | 2026-11-19 |
| 5 | Launch profile listens on `http://localhost:5080`; base path is `/api/v1` | PASS | `Http_launch_profile_listens_on_localhost_5080`, `Health_is_served_under_the_api_v1_base_path`, `Health_without_the_base_path_is_404` | 3 tests passed. The port is proven by reading `launchSettings.json`; the host was not started on the real port (a test host does not use launch settings) | 2026-11-19 |
| 6 | Endpoints register through a `MapXxxEndpoints` extension called from `Program.cs`; health lives in `Common/` | PASS | `HealthEndpoints_has_a_public_static_MapHealthEndpoints_extension`, `Program_calls_MapHealthEndpoints` | 2 tests passed (reflection on the extension method, text check of `Program.cs`) | 2026-11-19 |
| 7 | API tests start the real host, pass, and carry `[Trait("AC", "TASK-002.<n>")]` | PASS | the whole `MiniWms.Api.Tests` run | 12 of 12 passed on `WebApplicationFactory<Program>`; all 12 tests carry an AC trait (1+2+2+2+3+2 across ACs 1 to 6) | 2026-11-19 |

Totals: PASS 7, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 6 of 7 ACs have a dedicated test (85.7%). AC 7 is a property of the test run itself.
- Pass: 26 of 26 tests pass across the solution (12 new, 14 existing) (100%).
- Coverage: not measured (coverage tooling is TASK-003).

## Process notes (first ticket through the fixed agent flow)
- `ticket-scout`, `change-implementer` and `test-writer` all ran; the two editing agents ran in isolated worktrees accepted by the new `WorktreeCreate` hook (no path refusal). No hook blocked an edit or a command.
- `worktree-apply.mjs --remove` carried back exactly the agents' changes both times (5 files from the implementer, 3 from the test writer) and removed the worktrees and branches.
- The test writer's first build failed with 3 errors (two missing `using` lines in its own test files); it fixed them before reporting.
- The implementer pinned `Microsoft.AspNetCore.Mvc.Testing` 10.0.15 after finding that 10.0.16 did not restore.

## Open points for the owner
- CORS on error responses: the CORS middleware sits after the error handlers, so a 404 or 500 from the allowed origin may carry no `Access-Control-Allow-Origin` header and the browser would hide the error body. AC 4 only covers the health endpoint, so this is not a failure. Worth a ticket or a later change before the frontend shows API errors.
- `src/backend/MiniWms.Api/Common/.gitkeep` is now redundant because `Common/` has a real file. It was left in place.
- The port is proven from the launch profile only; nothing here started the API on port 5080.
