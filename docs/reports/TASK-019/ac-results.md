# TASK-019 acceptance-criteria results

Ticket: `tickets/TASK-019.md` (Order the default bins list by Id once). Branch: `task/TASK-019-default-bins-order-id-once`. Date: 2026-11-27.
How it was run: `/start-ticket` with `ticket-scout`, then the owner approved the plan (`ticket_plan.md`; no new traits). `change-implementer` changed `ListBinsAsync` in `LocationQueries.cs` in an isolated worktree, carried back by `worktree-apply.mjs`. `test-writer` ran the proving tests and the gate and changed nothing, as the plan said. The numbers below are from the main session's own runs.

| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | Domain 63, Application 235, Api 470 passed, 0 failed (unchanged counts: no test added or removed) |
| `dotnet test src/backend/MiniWms.Api.Tests --no-build --filter` on the five bins default, sort, name-order and page-boundary tests (by name) | 13 passed, 0 failed |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | `Combined line coverage: 97.05% (657 of 677 lines); threshold 90%` `GATE PASSED.` |

| AC # | AC text (short) | Result | Evidence | Date |
|---|---|---|---|---|
| 1 | No `sort`: bins in ascending `Id` order, page by page, as today | PASS (by the existing tests, no new test) | `Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest` and `Without_a_sort_the_lists_come_back_in_id_order` pass with their expected values unchanged | 2026-11-27 |
| 2 | Every allowed sort, both directions: order and `Id` tie-break unchanged | PASS (by the existing tests, no new test) | `Bins_sort_by_every_allowed_field_with_the_id_as_tie_break`, `Bins_sort_by_name_without_regard_to_case_and_equal_names_keep_the_id_order` and `Bins_name_order_holds_across_page_boundaries` pass with their expected values unchanged | 2026-11-27 |
| 3 | Default order has `Bin.Id` once; every allowed sort keeps the `Id` tie-break; order in SQL; no migration or contract change | PASS (by the diff, no test) | The diff: each named arm ends in `.ThenBy(p => p.Bin.Id)`, the `_` arm is `OrderBy(p => p.Bin.Id)` alone, the shared `.ThenBy` after the `switch` is removed, and everything stays on the `IQueryable` before `ToPagedResultAsync`. Only `LocationQueries.cs` changed among the code files | 2026-11-27 |
| 4 | Suite and gate pass; this file and the log row | PASS (by real runs, no test) | The command table; this file and the row in `docs/reports/ac-log.md` | 2026-11-27 |

Totals: PASS 4, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 0 of 4 criteria have `TASK-019` tagged tests. Criteria 1 and 2 are proven by the existing tests passing unchanged, and 3 and 4 by the diff and real runs (owner decision: no new tests, no new traits).
- Pass: 4 of 4 criteria; all 768 backend tests pass.
- Coverage: 97.05% combined (gate 90%). `LocationQueries.cs` is in Infrastructure, which the gate does not measure (ADR-005).

## Notes
- The SQL text (one `"b"."Id"` in the default `ORDER BY`) was not captured. A `ToQueryString()` test is out of scope (no owner decision yet). `ef-query-verifier` in the review chain checks the query by reading it.
- `test-writer` reasoned that the tie-break tests might not fail if a per-arm `ThenBy` were dropped: SQLite often returns tied rows in rowid order, which here matches `Id` order. This was not checked by experiment.
