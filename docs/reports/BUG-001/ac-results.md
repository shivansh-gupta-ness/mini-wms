# BUG-001 acceptance-criteria results

Ticket: `tickets/BUG-001.md` (API error responses lose CORS headers and use generic problem types). Branch: `bug/BUG-001-api-error-responses-lose-cors`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval, then tests first: `test-writer` wrote the regression tests against the unfixed `Program.cs`; I applied them back and recorded the "before" run; `change-implementer` then fixed `Program.cs`; I applied that back and re-ran everything. Both editing agents ran in isolated worktrees. The numbers below are from my own runs in the main working tree, not from the agents' summaries.

## Before and after (AC 8)

BUG-001 tests: 36 tests (AC 1: 1, AC 2: 1, AC 3: 2, AC 4: 2, AC 5: 14, AC 6: 13, AC 7: 3).

| Run | Result |
|---|---|
| BEFORE the fix: `dotnet test src/backend --no-build --filter "AC~BUG-001"` on the unchanged `Program.cs` | Failed 10, Passed 26, Total 36 |
| Which failed | AC 5: 4 rows of `Error_type_follows_adr_003_by_status` (400 validation, 401 unauthorized, 403 forbidden, 409 conflict, each got `error`). AC 6: all 6 rows of `Error_body_has_a_non_empty_trace_id` (no `traceId` in the body) |
| Which passed before the fix | AC 1, 2, 3, 4 (CORS on 404 and 500, other origins, preflight) and AC 7, plus the `status`, `detail`, `instance` and generic-title checks of AC 5 and 6 |
| AFTER the fix: `dotnet test src/backend --no-build` | `MiniWms.Api.Tests`: Passed 46, Failed 0. `MiniWms.Application.Tests`: Passed 46, Failed 0. `MiniWms.Domain.Tests`: no tests yet. Exit code 0 |
| `dotnet build src/backend` | Build succeeded. 0 Warning(s), 0 Error(s) |
| Real coverage gate (`coverage.ps1`) | Exit 0. Domain n/a, Application n/a, Api 100%, combined 100% (5 of 5 lines), GATE PASSED |

## Per criterion

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | 404 from `http://localhost:4200` carries `Access-Control-Allow-Origin` and a problem+json body | PASS | `Unknown_route_with_angular_origin_is_404_problem_json_with_cors_header` | Passed before and after the fix. The defect described in the ticket did not reproduce (see finding below) | 2026-11-19 |
| 2 | 500 from that origin carries the header, a problem+json body and the generic title | PASS | `Unhandled_exception_with_angular_origin_is_500_problem_json_with_cors_header_and_generic_title` | Passed before and after the fix | 2026-11-19 |
| 3 | Error responses for any other origin carry no `Access-Control-Allow-Origin` | PASS | 2 tests (404 and 500 from `http://evil.example`) | Passed before and after; they are also the negative control that shows the header in AC 1 and 2 comes from the CORS policy | 2026-11-19 |
| 4 | Preflight from the Angular origin succeeds with allow origin, methods and headers; another origin gets no allow-origin | PASS | 2 tests | Passed before and after the fix | 2026-11-19 |
| 5 | Problem `type` by status (400 validation, 401 unauthorized, 403 forbidden, 404 not-found, 409 conflict, 500 and others error); `status` equals the HTTP status | PASS | `Error_type_follows_adr_003_by_status` (6 rows), `Error_status_property_equals_the_http_status` (7 rows incl. 418), `Status_without_an_adr_003_type_has_type_error` | 14 tests. Before the fix 4 of them failed; after the fix 14 of 14 pass | 2026-11-19 |
| 6 | Every error body has a non-empty `traceId`, no `detail`, no `instance`; 500 title stays generic | PASS | `Error_body_has_a_non_empty_trace_id` (6 rows), `Error_body_has_no_detail_and_no_instance` (6 rows), `Server_error_title_stays_generic` | 13 tests. Before the fix all 6 `traceId` rows failed; after the fix 13 of 13 pass | 2026-11-19 |
| 7 | "No stack trace" tests assert the body structure (allow-list of property names) and the absence of the exception's secret text, not substrings | PASS | `Unknown_route_body_has_only_allowed_properties`, `Unhandled_exception_body_has_only_allowed_properties`, `Unhandled_exception_body_does_not_contain_the_exception_secret_text` | 3 tests passed. The old substring checks (" at ", "Exception") were removed | 2026-11-19 |
| 8 | Before the fix the tests for AC 5 and AC 6 fail (the real defects) and the CORS tests for AC 1 to 4 pass as guards; after the fix all pass; result recorded (amended by the owner, see below) | PASS (amended AC, owner decision) | the before and after runs above | Before the fix AC 5 failed (4 rows) and AC 6 failed (6 rows); AC 1 to 4 passed. After the fix all 36 pass. **Original wording** required at least AC 1, 2 and 5 to fail before the fix. The before-run showed AC 1 and 2 pass (the CORS-on-errors defect, review finding 1, does not reproduce on .NET 10), so the owner amended AC 8 on 2026-11-19 to match | 2026-11-19 |
| 9 | Build has 0 warnings, every test passes, every AC-proving test is tagged `[Trait("AC", "BUG-001.<n>")]` | PASS | the whole run | Build 0 warnings and 0 errors; 92 tests pass (46 + 46), exit code 0; all 36 BUG-001 tests carry a `BUG-001.<n>` trait | 2026-11-19 |

Totals: PASS 9, FAIL 0, N/A 0, NOT RUN 0 (AC 8 passes only against its amended text; the original AC 8 would be FAIL, as recorded above)

## Gates
- Mapping: 7 of 9 ACs have dedicated tests (77.8%). AC 8 and AC 9 are proven by the before and after runs and the overall run.
- Pass: 92 of 92 tests pass across the solution after the fix (100%). 10 BUG-001 tests failed before the fix, as intended.
- Coverage: 100% line coverage, but only 5 coverable lines exist (all in `HealthEndpoints`; `Program.cs` is excluded), so the figure says little.

## Findings from this ticket
1. **Review finding 1 does not reproduce.** On .NET 10 the 404 and 500 `application/problem+json` responses keep their `Access-Control-Allow-Origin` header even with `UseCors` after the error handlers. AC 2 and AC 3 are a positive and a negative control (same throwing route; allowed origin gets the header, another origin does not), so the passing result is meaningful. The `UseCors` reordering was still made, as agreed in the ticket, as standard hardening; it did not change any test result. The ticket's description of observed behaviour item 1 is therefore inaccurate for this host.
2. **The real defects were findings 7 and 9 and the test gaps (8).** Problem types were all `error` outside 404 and 500, and the `traceId` was cleared.
3. **ADR-003 and the code now differ:** ADR-003 says every error body has `detail`; the code (and AC 6) drop it deliberately, and `traceId` is not in the ADR. Suggested chore: update ADR-003.

## Process notes
- Order: tests first (test-writer), "before" run recorded by me, then the fix (change-implementer), as AC 8 requires.
- Both editing agents ran in isolated worktrees accepted by the `WorktreeCreate` hook. The first implementer start failed because the hook command `node .claude/hooks/worktree-create.mjs` uses a path relative to the shell's current folder, and my shell was sitting inside `src/backend/...`; after returning to the repository root it started. The hook commands should use `$CLAUDE_PROJECT_DIR`.
- The `run-tests` hook ran in the main checkout (without the agent's uncommitted fix) when the implementer stopped, so the agent saw the 10 old failures even though its own worktree passed 46 of 46. The hook should run in the worktree it is checking.
- No hook blocked an edit or a command.
