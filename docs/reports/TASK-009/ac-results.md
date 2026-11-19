# TASK-009 acceptance-criteria results

Ticket: `tickets/TASK-009.md` (Frontend HTTP layer: interceptors, ApiError and form error helper). Branch: `task/TASK-009-frontend-http-layer`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (decisions recorded in the ticket), `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. Numbers come from my own runs in the main working tree after the hand-offs.

| Command (in `src/frontend`) | Result |
|---|---|
| `npm run build` | Succeeded |
| `npm test` (run twice, then again after a comment-only edit) | Test Files 12 passed (12), Tests 129 passed (129) (48 existing, 81 new) |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | Requests through the API client get `/api/v1`; no API host hard-coded | PASS | 45 tests tagged `TASK-009.1` (`api-client.spec.ts`: five methods with and without a leading slash, query params, absolute and protocol-relative URLs throw and send nothing; `no-hardcoded-hosts.spec.ts`: no `http://`, `https://` or `localhost` in non-spec source under `src/app`; `API_BASE_PATH` is `/api/v1`) | Tests passed | 2026-11-19 |
| 2 | Token interceptor adds the bearer header; with no token none is added | PASS | 5 tests (`auth.interceptor.spec.ts`: header with token, none without, none after `clear()`, latest token wins, token never written to web storage) | Tests passed | 2026-11-19 |
| 3 | `application/problem+json` becomes a typed `ApiError` with status, title, detail and field errors | PASS | tests in `error.interceptor.spec.ts` (400 with errors and traceId, 409 keeps `detail`, 404 and 401 rethrown with no notifier call and no redirect, missing or malformed fields tolerated, HTML, array and null bodies give a generic `ApiError`, success passes through) | Tests passed | 2026-11-19 |
| 4 | 5xx shows a generic message with no technical detail (NFR-3) | PASS | tests in `error.interceptor.spec.ts` (500, 503 and an HTML 500 give the generic `userMessage` with no title, detail, traceId or planted secret; notifier called once; status 0 notifies generic; 4xx does not notify; `ErrorNotifier` opens a `MatSnackBar` with the exact message and a real snack bar shows it in the DOM) | Tests passed | 2026-11-19 |
| 5 | Form helper sets each server message on the matching control and shows it under it | PASS | 11 tests (`apply-server-errors.spec.ts`: message appears in a `mat-error` under the matching field, controls touched, unmatched and out-of-range keys returned, dotted `lines.0.qty` through a `FormArray`, several messages for one control, existing validator errors kept, the `server` error clears on the next edit) | Tests passed | 2026-11-19 |
| 6 | All tests pass; AC-proving test names start with `TASK-009.<n>` | PASS | 5 tests (`task-009-naming.spec.ts` scans the other new specs) | 129 of 129 tests pass | 2026-11-19 |

Totals: PASS 6, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 6 of 6 ACs have dedicated tests (100%).
- Pass: 129 of 129 frontend tests pass (100%); 81 new.
- Coverage: not measured; the frontend coverage gate is TASK-010.

## Notes
- The `app.config.ts` wiring (interceptor order `authInterceptor`, then `errorInterceptor`) has no test, because importing `appConfig` into a spec pulls in the router. The tests register the interceptors themselves in the same order.
- The implementer's doc comment said a `server` error does not clear itself. The tests show Angular clears it on the next edit (validators re-run), so the comment was corrected.
- `MatSnackBar` needed no animation providers in jsdom.
- 5xx: `detail` is dropped even if the body carries one; `traceId` is kept on the `ApiError` for logs and never shown. 401 is not redirected (session expiry belongs to the auth module).
- `src/frontend/REPO_CONTEXT.md` is 76 lines (limit 80).
- PR size: about 19 files and 900 changed lines.

## Review round (`/code-review`, 8 findings)
Fixed (10 new tests in `review-fixes.spec.ts`, one existing expectation updated):
- #1 the bearer token is added only to requests whose URL starts with `/api/v1/`, so it never reaches a third-party host;
- #3 `SKIP_ERROR_NOTIFICATION` (an `HttpContext` token) lets a page that shows its own error skip the global snackbar; the `ApiError` is still thrown;
- #4 an empty message list is skipped instead of setting a blank `server` error;
- #5 bracket keys such as `lines[0].qty` are read as `lines.0.qty`;
- #6 the API client refuses paths with backslashes, dot segments (`.`, `..`), `?` or `#`;
- #7 for a 4xx, `userMessage` is the backend's `detail` text when present, else the title (the existing 409 test now expects the detail).
Not changed: #2 a 4xx whose body is not JSON keeps the generic title, and a `blob` response type is not parsed (no such endpoint exists; revisit with the first download); #4 the several messages for one control stay joined with a space (one `mat-error`); #5 a disabled control gets the error without becoming invalid, and PascalCase keys are not folded (the backend sends camelCase); #8 is a wording issue in the agent rules (enforcement is root-only), fixed in a separate setup PR.
After the fixes: build succeeds, 129 of 129 tests pass twice.
