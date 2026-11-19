# TASK-011 acceptance-criteria results

Ticket: `tickets/TASK-011.md` (Frontend dev proxy and API health indicator). Branch: `task/TASK-011-dev-proxy-api-status`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (decisions recorded in the ticket), `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. Numbers come from my own runs in the main working tree after the hand-offs. The API and the dev server were NOT started together, so the real-browser checks are not run.

| Command (in `src/frontend`) | Result |
|---|---|
| `npm run build` | Succeeded (no errors or warnings) |
| `npm run test:coverage` (run twice) | Both runs: Test Files 16 passed (16), Tests 169 passed (169) (141 existing, 28 new); `Line coverage: 98.59% (140 of 142 lines); threshold 90%.` `GATE PASSED.` |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | With `npm start` and the API on 5080, `/api/v1/health` is forwarded by the dev proxy without CORS errors | NOT RUN (the configuration is tested; the real forwarding is not) | tests (`TASK-011.1`, `proxy.spec.ts` and `api-status.spec.ts`): `proxy.conf.json` maps `/api` to `http://localhost:5080`, `angular.json` `serve` has `proxyConfig`, the proxy file is outside `src/app`, the health call goes to `/api/v1/health` | Config tests passed. Needs a manual check: `dotnet run --project src/backend/MiniWms.Api`, `npm start`, open `http://localhost:4200`, and confirm there is no CORS error in the browser console | 2026-11-19 |
| 2 | With `{ "status": "ok" }` the top bar shows "API: ok" as text | PASS (unit tests; not checked in a real browser) | tests (`TASK-011.2`: empty before the first answer, exactly `API: ok` after the answer, `role="status"` and `aria-live="polite"`, any other 200 body shows `API: unavailable`) | Tests passed | 2026-11-19 |
| 3 | With the API down the top bar shows "API: unavailable" and the app stays usable | PASS (unit tests; not checked in a real browser) | tests (`TASK-011.3`: network error, 500 and 503 show `API: unavailable`; the shell keeps its nav and banner with the indicator inside the banner; the global snackbar is not called; an unanswered request is unavailable at exactly 5,000 ms) | Tests passed | 2026-11-19 |
| 4 | A failing indicator retries no more than once every 30 seconds | PASS | tests (`TASK-011.4`, fake timers: nothing at 29,999 ms and one request at 30,000 ms; after a failure and after a timeout the retry is not earlier than 30,000 ms; a later success turns the text back to `API: ok`; destroying the component stops polling) | Tests passed | 2026-11-19 |
| 5 | `npm test` and `npm run build` pass; AC-proving test names start with `TASK-011.<n>` | PASS | tests (`TASK-011.5`: every title in the two new spec files starts with `TASK-011.`) | 169 of 169 tests pass; build succeeds | 2026-11-19 |
| 6 | `src/frontend/REPO_CONTEXT.md` explains how to run the API and the frontend together | PASS | tests (`TASK-011.6`: the run commands, the proxy, both ports and the `API: ok` text are present; fewer than 80 lines) | Tests passed; the file is 76 lines | 2026-11-19 |

Totals: PASS 5, FAIL 0, N/A 0, NOT RUN 1 (AC 1, and the real-browser part of AC 2 and AC 3)

## Gates
- Mapping: 6 of 6 ACs have dedicated tests (100%); AC 1 and the browser parts of AC 2 and 3 need the manual check.
- Pass: 169 of 169 frontend tests pass (100%); 28 new.
- Coverage: 98.59% line coverage of `src/app` (gate 90%).

## Notes
- The 5-second timeout always ends an unanswered request before the next 30-second tick, so the `exhaustMap` guard against stacking requests cannot be told apart in a test. The tests show the closest behaviour: no new request while the first is pending, it is cancelled at 5,000 ms, and the next one goes at 30,000 ms.
- The health request sets `SKIP_ERROR_NOTIFICATION`, so a down API does not open the global error snackbar (test with the real error interceptor and a fake notifier).
- Four shell specs (`shell`, `a11y`, `app.routes`, `skip-link`) got `provideHttpClient()` and `provideHttpClientTesting()` in their render helpers; no test was renamed.
- `src/frontend/REPO_CONTEXT.md` has a new "Run the API and the frontend together" section (76 lines); the proxy is dev-only and `ng build` does not use it.
- PR size: about 17 files and 460 changed lines.

## Review round (`/code-review`, 8 findings)
Fixed:
- #1 the comment in `api-base.ts` no longer says the proxy is a future ticket;
- #2 the `Not done yet` heading is back above the "Real module screens" bullet in `src/frontend/REPO_CONTEXT.md` (77 lines);
- #6 the proxy key is now `/api/`, so a path such as `/apis` or `/api-docs` is not forwarded (the proxy test and the doc text were updated).
Not changed (owner decisions or accepted limits):
- #3 polling continues while the tab is hidden (owner decision 8), and specs that render the shell make one unflushed health request, which is why four specs got the HTTP providers; none calls `verify()`;
- #4 the live region exists from the start so changes are announced (owner decision 7); a quieter variant (announce only "unavailable") is possible later;
- #5 a `{ "status": "degraded" }` answer and a network failure both show `API: unavailable` by design (owner decision 6);
- #6 the target stays `http://localhost:5080`, as the ticket states; an environment override is not needed yet;
- #7 the `SKIP_ERROR_NOTIFICATION` opt-out per caller is the design from TASK-009; the health request is the only caller today;
- #8 the proxy spec reads files through `process.cwd()` like the other tooling specs; it cannot prove the real forwarding, which is the manual check (AC 1).
After the fixes: build succeeds, 169 of 169 tests pass twice, gate passes at 98.59%.
