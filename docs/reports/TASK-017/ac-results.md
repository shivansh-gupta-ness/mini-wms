# TASK-017 acceptance-criteria results

Ticket: `tickets/TASK-017.md` (Close the test gaps in the security header tests). Branch: `task/TASK-017-security-header-test-gaps`. Date: 2026-11-26.
How it was run: `/start-ticket` flow with `ticket-scout` (first run on opus), owner approval of the plan with four decisions, then `test-writer` twice (the tests, then a fix round after the reviews, for which criteria 2 and 3 were amended with the owner's approval), each in an isolated worktree carried back by `worktree-apply.mjs`. `change-implementer` did not run: the only source file is a test. The numbers below are from the main session's own runs, not from the agent's summaries. No production defect was found: every new test passed on the current code.

| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | Domain 63, Application 235, Api 464 passed, 0 failed (Api was 450; 14 new cases) |
| `dotnet test src/backend/MiniWms.Api.Tests --no-build --filter "AC=TASK-017.n"` | n=1: 1 passed; n=2: 7; n=3: 6; n=4: 1; n=5: 4; 0 failed |
| the same with `AC~TASK-015` | 20 passed, as before the ticket |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | `Combined line coverage: 97.05% (657 of 677 lines); threshold 90%` `GATE PASSED.` |

| AC # | AC text (short) | Result | Evidence | Date |
|---|---|---|---|---|
| 1 | Chunked 413 from the exception handler: status, media type, both headers, exact keys, type and title | PASS | `A_chunked_413_without_Content_Length_has_both_headers_and_the_handler_body`; it also proves that the server saw no `Content-Length` and `chunked` | 2026-11-26 |
| 2 | Production host: 200, 404 and 500 with both headers, environment proven, 404 and 500 bodies, no database file | PASS | 7 cases: the environment, the missing database file, a theory over health, an unknown path and a throwing route, the 500 body and the 404 body | 2026-11-26 |
| 3 | Locations 200, 201 and PATCH 200 with both headers | PASS | 6 cases: a theory over warehouse, zones, bins and tree, the zone POST and the zone PATCH | 2026-11-26 |
| 4 | An endpoint's own `X-Content-Type-Options` and `Cache-Control` are replaced, one value each | PASS | `Header_values_set_by_an_endpoint_are_replaced_by_nosniff_and_no_store` (strengthened and renamed): the route also sets `sniff` | 2026-11-26 |
| 5 | Exact bodies in the listed `unchanged_body` tests | PASS | 4 strengthened tests: the 400, the real 409, the status-code-page 409 and the declared-length 413 | 2026-11-26 |
| 6 | Traits, no assertion lost, only one source file, suite and gate, report and log row | PASS (by diff review and real runs, no test) | In the test file only two lines are replaced (the class summary and one method name), no assertion; the command table above; this file and the row in `docs/reports/ac-log.md` | 2026-11-26 |
| 7 | Each gap proven closed by a mutation in a throwaway copy | PASS (by real runs, no test) | The table below | 2026-11-26 |

Totals: PASS 7, FAIL 0, N/A 0, NOT RUN 0

## Mutations (criterion 7)
Run in a clone outside the repository (in the session's temporary folder); the working tree was not changed for this. The command guard blocked the main session from deleting the clone, so the owner deletes it by hand. Control: the final 34 `SecurityHeadersTests` cases pass there unmodified. Each change was made alone, built, run and restored. "Old" is the `SecurityHeadersTests` class as it was before this ticket (20 cases): it passed under every one of the 18 changes. Files are under `src/backend/`; "callback" is the start of the `OnStarting` callback in `MiniWms.Api/Common/Headers/SecurityHeadersMiddleware.cs` (line 13).

| # | Change | Edited at | New tests that fail |
|---|---|---|---|
| 1 | Return early on 413 with no `Content-Length` | callback | `A_chunked_413_without_Content_Length_has_both_headers_and_the_handler_body` (AC 1) |
| 2 | Middleware only in Development | `MiniWms.Api/Program.cs:84` | `A_Production_response_has_the_status_and_both_headers`, all 3 cases, and `The_Production_404_has_both_headers_and_the_problem_body` (AC 2) |
| 3a | Return early on 201 | callback | `A_201_zone_POST_has_both_headers` (AC 3) |
| 3b | Return early on PATCH | callback | `A_200_zone_PATCH_has_both_headers` (AC 3) |
| 3c | Return early for `/api/v1/warehouse` and `/api/v1/locations` | callback | `A_200_Locations_GET_has_both_headers`, warehouse and tree (AC 3) |
| 4a | `nosniff` set with `TryAdd` | `SecurityHeadersMiddleware.cs:15` | `Header_values_set_by_an_endpoint_are_replaced_by_nosniff_and_no_store` (AC 4) |
| 4b | `nosniff` set with `Append` | `SecurityHeadersMiddleware.cs:15` | the same test (AC 4) |
| 5a | 400 title changed | `MiniWms.Api/Common/Errors/ProblemResponses.cs:27` | `A_400_validation_problem_has_both_headers_and_the_unchanged_body` (AC 5) |
| 5b | 409 title changed | `ProblemResponses.cs:39` | `A_409_conflict_problem_has_both_headers_and_the_unchanged_body` (AC 5) |
| 5c | 409 detail changed | `MiniWms.Domain/Locations/Zone.cs:57` | the same test (AC 5) |
| 6 | Return early for `GET /api/v1/zones` | callback | `A_200_Locations_GET_has_both_headers`, zones (AC 3) |
| 7 | Status-code-page 409 sent as HTTP 400 | `Program.cs`, problem customizer, after `problem.Instance = null`, only when no detail is allowed | `A_status_code_page_409_has_both_headers_and_the_unchanged_body` (AC 5) |
| 8a | Declared-length 413 sent as `application/json` | `MiniWms.Api/Common/RequestLimits/RequestBodyLimitMiddleware.cs:18`, declared-length branch, so the chunked 413 is not affected | `A_413_problem_has_both_headers_and_the_unchanged_body` (AC 5) |
| 8b | Declared-length 413 with type `conflict` | `Program.cs`, problem customizer, only when the request has a `Content-Length`, so the chunked 413 is not affected | the same test (AC 5) |
| 9 | `UseStatusCodePages` only in Development | `Program.cs:89` | `The_Production_404_has_both_headers_and_the_problem_body` (AC 2) |
| 10 | Fixed 500 title only in Development | `Program.cs:48` | `The_Production_500_has_the_generic_problem_body_without_the_exception_text` (AC 2) |
| 11 | Migrate and seed in every environment | `Program.cs:69` | `The_Production_host_does_not_create_the_database_file` (AC 2) |
| 12 | Return early for `/api/v1/bins` | callback | `A_200_Locations_GET_has_both_headers`, bins (AC 3) |

Limit: only the `SecurityHeadersTests` class was run against the mutations. Other test classes may already catch some of them (for example the texts of error bodies); the table does not claim that the whole old suite missed them.

## Gates
- Mapping: 5 of 7 criteria have tagged tests (71.4%); criteria 6 and 7 are proven by diff review and real runs (owner decision).
- Pass: 7 of 7 criteria; all 762 backend tests pass.
- Coverage: 97.05% combined (gate 90%). The ticket changes no production code.

## Review rounds
- `/own-code-review`: pass, 6 minor findings. The Production 500 body test was not in the plan (kept and strengthened, now part of criterion 2); the edit locations, two sentences of this report and the class summary are fixed. Open: the clone is not deleted yet.
- `/independent-code-review`: approve, 1 minor finding (the same Production 500 test). Fixed.
- `/test-critique`: ready, no `must` row, 5 `should` rows. Four added with the owner's approval; the successful deactivate (its M5) is not added: one more route of a global middleware. The renamed test closes its naming finding.
- `/security-review`: no findings. It read the working tree, because nothing was committed.
- Not run: `/code-review`.
- The reviews saw the first round; the fix round was reviewed by the main session only.

## Notes
- Owner decision: the 404 and both 500 `unchanged` tests of the Development hosts stay as they were.
- The frontend was not run: no frontend file changes.
- Not run: a check in a real browser or against the running Kestrel host; the proof is in-process through the test server.
