# TASK-006 acceptance-criteria results

Ticket: `tickets/TASK-006.md` (API building blocks: paging, sorting, validation and error mapping). Branch: `task/TASK-006-api-building-blocks`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (six owner decisions recorded in the ticket), `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. The numbers below come from my own runs in the main working tree after the hand-offs.

| Command | Result |
|---|---|
| `dotnet build` (src/backend) | 0 Warning(s), 0 Error(s) |
| `dotnet test --no-build` (run twice) | Both runs: Domain.Tests Passed 63, Api.Tests Passed 199, Application.Tests Passed 80, Failed 0 |
| `dotnet test --no-build --filter "AC=TASK-006.<n>"` | n=1: 10 Application + 1 Api; 2: 14 + 4; 3: 5 + 1; 4: 9 Api; 5: 12 Api; 6: 7 Api; 7: 3 Application + 5 Api; all passed |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | Domain 100%, Application 100%, Api 100%; `Combined line coverage: 100% (227 of 227 lines); threshold 90%`. `GATE PASSED.` |
| `src/backend/MiniWms.Api/App_Data` after all runs | does not exist |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | Defaults page 1 / 25; pageSize above 100 rejected with a field-level 400 | PASS | 11 tests (`PagedQueryValidationTests`; one through the real pipeline) | Tests passed | 2026-11-19 |
| 2 | `sort=name` / `-name` orders by an allowed field; unknown field rejected with a field-level 400 | PASS | 18 tests (`SortingTests`, SQLite translation on a throwaway entity and `Product`, API pipeline 400 on `sort`) | Tests passed | 2026-11-19 |
| 3 | List result has `items`, `page`, `pageSize`, `totalItems` | PASS | 6 tests (serialised property set and order, empty list, paging slices, SQLite page on `Product`) | Tests passed | 2026-11-19 |
| 4 | Invalid FluentValidation command gives 400 problem+json with camelCase `errors` | PASS | 9 tests (`ApiErrorMappingTests`: FluentValidation, `ValidationFailedException`, `PagedQueryValidator`) | Tests passed | 2026-11-19 |
| 5 | Domain exception gives 409 `conflict` with its message as `detail`; other exceptions still no `detail` | PASS | 12 tests (409 with detail; 500, 404, 401 without; property allow-list; detail only on conflict routes) | Tests passed | 2026-11-19 |
| 6 | Stale `version` gives a concurrency exception and a 409 `conflict` | PASS | 7 tests (`VersionedUpdateTests`: stale version stores nothing, current version increments, the direct-`Version` bypass is shown and closed by `UseClientVersion`; API 409 with the fixed text and no leaked exception text) | Tests passed | 2026-11-19 |
| 7 | All tests pass with AC traits | PASS | 8 trait-presence tests (3 Application, 5 Api) | 342 of 342 tests pass, twice | 2026-11-19 |

Totals: PASS 7, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 7 of 7 ACs have dedicated tests (100%).
- Pass: 342 of 342 tests pass (100%); 75 new tests (Application 34, Api 41) plus the TASK-005 Domain tests edited for `ValidationFailedException`.
- Coverage: 100% (gate 90%).

## Notes
- Domain: field rules (blank, too long, negative, invalid type) now throw `ValidationFailedException` (400); `DomainException` stays for business-rule conflicts (409). The seeder catches both.
- `detail` is kept only on 4xx responses a handler has flagged (an `HttpContext.Items` flag), because `ProblemDetailsContext.Exception` was not reliable in the customizer. BUG-001.6 and BUG-001.7 still pass unchanged; new tests cover the cases where `detail` is allowed.
- `ApplySort(null)` orders by `Id` (no business default order).
- FluentValidation 12.2.0 pinned in `Directory.Packages.props`.
- PR size: 30 files and about 1,250 changed lines.

## Review round (`/code-review`, 8 findings)
Fixed: the skip count is computed in `long` and clamped, so a huge page number cannot wrap into a wrong page (#1); an async `ToPagedResultAsync` for EF queries in Infrastructure (the Application helper stays EF-free) (#2); a space after the `-` in `sort` is ignored (#6); `DomainException` now documents that its message is sent to clients (#7). A `DateTimeOffset` sort is proven to translate on SQLite (#3, no code change needed). 4 new tests; after the fixes build 0 warnings, 63 + 199 + 80 tests pass twice, gate 100%.
Not changed: boxed selectors work for string, int and `DateTimeOffset` (tested), a typed selector can come when a module needs another type (#3); field keys such as `type` are the request field names in the API specs (#4); first-failure-only Domain guards are fine because FluentValidation validators aggregate the fields, and module command validators must run before the entities (#5, module tickets); a caller that forgets `UseClientVersion` skips the check, so module update use cases must call it, and each module's AC for a stale `version` must test that (#8).
