# TASK-019 plan

Ticket: `tickets/TASK-019.md` (Order the default bins list by Id once)
Branch: `task/TASK-019-default-bins-order-id-once`
Date: 2026-11-27
Status: approved (2026-11-27; owner chose no new traits in step 2)

Doc and code disagreements: none (`ticket-scout` checked `locations.yaml`, ADR-002 and `src/backend/REPO_CONTEXT.md`).
Risk: low (ticket and knowledge map agree). Agents edit after approval, inside `allowed_paths`.

## Plan
1. **`change-implementer`: `src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs`, `ListBinsAsync`.**
   - Put `.ThenBy(p => p.Bin.Id)` on each named arm (`code`, `name`, `zone`, `isactive`).
   - Keep the `_` arm as `bins.OrderBy(p => p.Bin.Id)` alone.
   - Remove the shared `.ThenBy(p => p.Bin.Id)` after the `switch`.
   - This matches how `ListZonesAsync` and `ApplySort` already build the order. The ordering stays in the `IQueryable`, so it stays in the SQL.
   - No other file changes. No migration, no contract change, no Application change.
2. **`test-writer`: `src/backend/MiniWms.Api.Tests/Locations/LocationListApiTests.cs`.**
   - No new test and no change to expected values.
   - The proof for AC 1 and 2 is the existing tests passing unchanged:
     - `Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest`
     - `Without_a_sort_the_lists_come_back_in_id_order`
     - `Bins_sort_by_every_allowed_field_with_the_id_as_tie_break`
     - `Bins_sort_by_name_without_regard_to_case_and_equal_names_keep_the_id_order`
     - `Bins_name_order_holds_across_page_boundaries`
   - No new traits on those tests (owner decision, 2026-11-27, as on TASK-018).
3. **Checks:** run `dotnet test src/backend` and `src/backend/coverage.ps1` (90% gate). AC 3 is proven by reading the diff. A `ToQueryString()` test is out of scope (no owner decision yet), and `ef-query-verifier` in the review chain checks the query.
4. **Reports:** write `docs/reports/TASK-019/ac-results.md` from the real output and add one row to `docs/reports/ac-log.md`.
5. **Review chain:** after the build, run `own-code-review`, `independent-code-review`, `security-reviewer`, `test-critic`, `ef-query-verifier` (runs, because `MiniWms.Infrastructure` changes) and `spec-drift-auditor`. The chain saves `code_summary.md` and `review_findings.md` here.
