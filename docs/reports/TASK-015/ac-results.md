# TASK-015 acceptance-criteria results

Ticket: `tickets/TASK-015.md` (Add nosniff and no-store headers to API responses). Branch: `task/TASK-015-nosniff-no-store-headers`. Date: 2026-11-23.
How it was run: `/start-ticket` flow with `ticket-scout` (it answered where the middleware must sit and proposed the plan), owner approval of the plan, then `change-implementer` (stage 1: the middleware and its registration) and `test-writer` (stage 2: one test file), each in an isolated worktree carried back by `worktree-apply.mjs`. The main session ran the commands and added the context lines.

Defects and notes found on the way: the first 409 setup the test-writer tried (a duplicate zone code) returned 400, because the API reports a duplicate code as a field error (ADR-003); it switched to a real 409 (deactivate a zone that has a bin). The production code passed every test on its first run. The stage 1 worktree folder could not be removed (`EPERM`; `worktree-apply` warned, exited 0 and left the folder for hand removal).

| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` (run twice) | Both runs: Domain 63, Application 199, Api 450 passed, 0 failed (Api was 430 before the ticket; 20 new tests) |
| `dotnet test src/backend/MiniWms.Api.Tests --no-build --filter "AC=TASK-015.n"` | n=1: 5 passed; n=2: 6; n=3: 7; n=4: 2; n=5: 4; 0 failed |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | Domain 100%, Application 97.6%, Api 95.2%; `Combined line coverage: 97.05% (657 of 677 lines); threshold 90%` `GATE PASSED.` |
| line count of `src/backend/REPO_CONTEXT.md` | 78 by `wc -l`; 79 by the earlier reports' method (`split` on line breaks, which counts the empty piece after the last newline). Under 80 either way |

| AC # | AC text (short) | Result | Evidence | Date |
|---|---|---|---|---|
| 1 | Any route (success, 404, 400) has `X-Content-Type-Options: nosniff` | PASS | 5 tests tagged `TASK-015.1`: a theory over `GET /api/v1/health`, a missing route and `POST /api/v1/zones` with `{}`, plus the error-body tests | 2026-11-23 |
| 2 | Any route (success, 404, 400) has `Cache-Control: no-store` | PASS | 6 tests tagged `TASK-015.2`: the same three responses, asserting both the parsed `NoStore` and the raw `no-store` value, and a route that sets its own `Cache-Control: public, max-age=3600`, which is replaced by `no-store` (added after `/code-review`; with `TryAdd` instead of overwrite this test and two 400 tests fail, because the framework sets its own `Cache-Control` on some problem responses) | 2026-11-23 |
| 3 | Problem responses (400, 409, 413, generic 500) keep both headers and an unchanged body | PASS | 7 tests tagged `TASK-015.3`: a real 400, 404, a real 409 and a handler-made 409, a 413 (65,537 bytes, body keys exactly `type`, `title`, `status`, `traceId`), the generic 500 and the Development 500 (no `detail`, no exception text); all carry both headers | 2026-11-23 |
| 4 | CORS preflight and existing CORS tests unchanged | PASS | 2 tests tagged `TASK-015.4`: an OPTIONS preflight from `http://localhost:4200` keeps `Access-Control-Allow-Origin` and has both headers; a GET with that origin too. The existing CORS tests (BUG-001) are untouched and pass in the full runs | 2026-11-23 |
| 5 | Tests tagged, no existing test changed, gate holds, `REPO_CONTEXT.md` mentions the headers (under 80 lines), report and ac-log row | PASS | The proof is: tagged tests exist for AC 1 to 4 (the per-criterion runs above, not the 4 `TASK-015.5` tests, which only guard that the tags stay; they prove no behaviour); Api tests 430 to 450 with no existing file edited; coverage 97.05%, gate passed; `REPO_CONTEXT.md` has a "Response headers" section (78 lines by `wc -l`); this report and the ac-log row | 2026-11-23 |

Totals: PASS 5, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: every AC has tagged tests (AC 5 checks it).
- Pass: 5 of 5 criteria; all 712 backend tests pass (Domain 63, Application 199, Api 450).
- Coverage: 97.05% combined (gate 90%).

## Notes
- The middleware registers `Response.OnStarting` and sets both headers with the indexer, so the exception handler and status-code pages cannot drop them; it is the first call in the pipeline, ahead of CORS, so a preflight carries them too.
- Not done, by design: HTTPS redirection, HSTS, CSP and config-driven CORS (out of scope); a one-line ADR-003 note about the headers (a later docs PR).
- Not run: a check in a real browser or against the running Kestrel host; the proof is in-process through the test server.
- **`/code-review` (4 nits), handled:** (1) overwriting `Cache-Control` is kept on purpose and now documented in the middleware comment and pinned by a test (a mutation to `TryAdd` made 3 tests fail); (2) `ReadRoot` now disposes its `JsonDocument` and returns a clone of the root; (3) the four `TASK-015.5` tests stay, as in TASK-014, but are no longer presented as the proof of AC 5 (see the AC 5 row); (4) the line count is stated for both methods above. The PR number goes in the ac-log row once the PR is open.
- The stage 1 worktree folder could not be removed at the time (`EPERM`); the owner deleted it by hand afterwards (`.claude/worktrees/` is empty).
