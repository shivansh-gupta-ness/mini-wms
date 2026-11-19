# TASK-014 acceptance-criteria results

Ticket: `tickets/TASK-014.md` (Limit the request body size and prove the Development 500). Branch: `task/TASK-014-request-size-limit`. Date: 2026-11-20.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval with the recommended decisions (recorded in the ticket), then `change-implementer` (production code), `test-writer` (tests) and a second `change-implementer` run for a defect the tests found, each in an isolated worktree carried back by `worktree-apply.mjs`. The main session ran the commands.

**Defect found by the new tests:** with `Accept: text/html` an unhandled exception returned a 500 with no body and no content type (ADR-003 wants the generic problem+json). Cause: `IProblemDetailsService` only uses a writer whose `CanWrite` accepts the request, and the default writer declines `text/html`. Fixed with `AlwaysJsonProblemDetailsWriter` (registered after the default writer, reuses the existing customizer). The test passes after the fix.

| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` (run twice) | Domain 63, Application 199, Api 430 passed, 0 failed, both runs (Api was 408 before the ticket; 22 new tests) |
| `dotnet test src/backend/MiniWms.Api.Tests --no-build --filter "AC=TASK-014.n"` | n=1: 12 passed; n=2: 3; n=3: 3; n=4: 1; n=5: 4; 0 failed |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | Domain 100%, Application 97.6%, Api 95%; `Combined line coverage: 97% (647 of 667 lines); threshold 90%` `GATE PASSED.` |
| line count of `src/backend/REPO_CONTEXT.md` (`split(/\r?\n/).length`) | 76 (under 80) |

| AC # | AC text (short) | Result | Evidence | Date |
|---|---|---|---|---|
| 1 | Body over 64 KB (declared or streamed) is 413 problem+json, no detail or stack trace, body not read to the end | PASS | 12 tests tagged `TASK-014.1`: declared and streamed bodies on POST/PATCH routes give 413 `application/problem+json` with exactly `type`, `title`, `status`, `traceId`; the server-side stream is not read past the limit; CORS header present on the 413. Real Kestrel behaviour: NOT RUN (manual: send a body over 64 KB to the running API; the in-process server does not enforce Kestrel's limit, so the limit is an app middleware) | 2026-11-20 |
| 2 | Body just under the limit still succeeds | PASS | 3 tests tagged `TASK-014.2`: exactly 65,536 bytes accepted with and without `Content-Length`; a normal small request works | 2026-11-20 |
| 3 | Development environment: unhandled exception is the generic 500, no developer page | PASS | 3 tests tagged `TASK-014.3` (the 500 test over several `Accept` values including `text/html`, and a test that the factory really runs as Development); after the writer fix all pass | 2026-11-20 |
| 4 | Existing API tests still pass unchanged | PASS | Api tests 408 before, 430 after (22 new); no existing test file edited, 0 failed | 2026-11-20 |
| 5 | Tests tagged, coverage gate holds, `REPO_CONTEXT.md` mentions the limit and stays under 80 lines | PASS | 4 tests tagged `TASK-014.5` check each AC has tagged tests; coverage 97% combined, gate passed; context file has a "Request size limit" section, 76 lines | 2026-11-20 |

Totals: PASS 5, FAIL 0, N/A 0, NOT RUN 0 (plus one manual check, real Kestrel 413, listed under AC 1 and not counted as an AC)

## Gates
- Mapping: every AC has tagged tests (AC 5 checks it).
- Pass: 5 of 5 criteria; all 692 backend tests pass.
- Coverage: 97% combined (gate 90%).

## Notes
- Owner decisions followed: limit 65,536 bytes inclusive, global middleware, no Kestrel limit set, 413 body with `type` `error`, title "Request body too large.", no `detail`, `errors` or limit text.
- The 22 tests include a few helper checks (factory is Development, chunked body has no `Content-Length`) that prove the tests test what they claim.
- ADR-003 gets a 413 row in a later docs PR (outside this ticket).
- Worktree removal worked cleanly this time.
