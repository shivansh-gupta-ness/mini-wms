---
id: TASK-019
type: task
title: Order the default bins list by Id once
status: done
owner: shivansh-gupta-ness
component: locations
risk: low
allowed_paths: [src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs, src/backend/MiniWms.Api.Tests/Locations/LocationListApiTests.cs, tickets/TASK-019.md, docs/reports/TASK-019/**, docs/reports/ac-log.md]
docs: [docs/specs/api/locations.yaml, docs/adr/ADR-002-ef-core-sqlite.md]
parent: 
depends_on: []
---

## Description
With no `sort`, `GET /api/v1/bins` orders by `Bin.Id` twice. The `switch` in `LocationQueries.cs` (the `_` arm) already orders by `p.Bin.Id`, then the shared `.ThenBy(p => p.Bin.Id)` adds it again, so SQLite gets `ORDER BY "b"."Id", "b"."Id"`. The result is right but the second key is dead weight, and the zones list does not do this: `ApplySort` returns `OrderBy(Id)` alone when there is no sort. Found by `/ef-verify` (2026-11-25 and 2026-11-26) and confirmed in the code. The fix is in the query only: add the `Id` tie-break only after a real sort key.

## Acceptance criteria
1. Given no `sort` parameter, when `GET /api/v1/bins` is called, then bins come back in ascending `Id` order, page by page, as today: `Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest` passes with its expected values unchanged.
2. Given each allowed sort (`code`, `name`, `zone`, `isActive`, ascending and descending), when the list is called, then the order and the `Id` tie-break are unchanged: the existing bins sort, name-order and page-boundary tests pass with their expected values unchanged.
3. Given the change, when `LocationQueries.cs` is read, then the default bins order puts `Bin.Id` in the ORDER BY once, every allowed sort is still followed by the `Bin.Id` tie-break, the ordering stays in the SQL (no in-memory sort), and no migration or contract change is made.
4. Given the tests, when they run, then `dotnet test src/backend` and the 90% coverage gate pass, and `docs/reports/TASK-019/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output.

## Out of scope
- The zones, warehouse and tree queries (they already order by `Id` once).
- The sort allow-list, the contract and the `Name` collation.
- The other `/ef-verify` findings (the zone and bin create race).
- A new SQL-capture test helper (`ToQueryString()`); the owner has not decided on it yet.

## Notes and links
- The `switch` is in `ListBinsAsync` in `src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs`; the shared `.ThenBy(p => p.Bin.Id)` follows it.
- If proving criterion 3 needs more than reading the code, ask the owner before adding a test outside `LocationListApiTests.cs`.
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).
- Owner decisions (2026-11-27): no new tests and no new traits; criteria 1 and 2 are proven by the existing bins tests passing unchanged. After the review chain, the owner continued with all 9 drift rows that `spec-drift-auditor` found in `locations` (none from TASK-019; rows 1 to 8 are the TASK-018 rows waiting for their `chore/` PR, row 9 is the missing index table in the root context file, a `setup/` fix) and acted on no findings.

## Delivery
PR 72. All 4 acceptance criteria PASS (1 and 2 by the existing tests unchanged, 3 and 4 by the diff and real runs); 768 backend tests pass, coverage 97.05%. Results in `docs/reports/TASK-019/ac-results.md`, the plan in `ticket_plan.md`, the build and review output in `code_summary.md` and `review_findings.md`, and the row in `docs/reports/ac-log.md`. Follow-ups, not acted on: the tied-name test `test-critic` suggested (a `name` tie-break on `Code` would pass today), a SQL-text check for the order (waits on the `ToQueryString()` decision), and the 9 drift rows.
