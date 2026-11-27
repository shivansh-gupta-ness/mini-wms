---
id: TASK-018
type: task
title: Sort zones and bins by name without regard to case
status: done
owner: shivansh-gupta-ness
component: locations
risk: low
allowed_paths: [src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs, src/backend/MiniWms.Api.Tests/Locations/LocationListApiTests.cs, src/backend/REPO_CONTEXT.md, docs/specs/api/locations.yaml, tickets/TASK-018.md, docs/reports/TASK-018/**, docs/reports/ac-log.md]
docs: [docs/specs/api/locations.yaml, docs/adr/ADR-001-solution-layering.md, docs/adr/ADR-002-ef-core-sqlite.md]
parent: 
depends_on: []
---

## Description
`GET /api/v1/zones?sort=name` and `GET /api/v1/bins?sort=name` sort by case: the `Name` columns have no collation, so SQLite compares them as BINARY and "Zebra" comes before "apple". Sorting by `code` already ignores case (the `Code` columns are `NOCASE`), so the two sorts behave differently. Found by `/ef-verify` (2026-11-25 and 2026-11-26) and confirmed in the code: `ZoneConfiguration.cs` and `BinConfiguration.cs` give `Name` no collation, and the sort tests seed only capitalised names. The fix is in the EF Core query: order by `EF.Functions.Collate(Name, "NOCASE")`. No migration and no column change.

## Acceptance criteria
1. Given zones named with mixed case (for example "apple", "Banana", "cherry"), when `GET /api/v1/zones?sort=name` is called, then they come back in case-insensitive order ("apple", "Banana", "cherry"), and `sort=-name` gives the reverse; equal names keep the `Id` tie-break.
2. Given bins named with mixed case, when `GET /api/v1/bins?sort=name` and `sort=-name` are called, then the same holds for bins.
3. Given every other sort field (`code`, `type`, `isActive` for zones; `code`, `zone`, `isActive` for bins) and the default order, when the lists are called, then the results are unchanged: the existing sort tests pass without any change to their expected values.
4. Given the change, when it is read, then the case-insensitive order is done in the Infrastructure query with `EF.Functions.Collate` and translated to SQL (`ORDER BY ... COLLATE NOCASE`), not sorted in memory; the Application layer gets no EF Core reference (ADR-001), so the zone `name` sort is a special case in `LocationQueries.cs`, like the existing `type` sort; and no migration is added.
5. Given `docs/specs/api/locations.yaml`, when read, then the zones and bins list descriptions say that sorting by `name` ignores case (ASCII letters only, as `NOCASE` does); Spectral and `ac-coverage.mjs` still pass.
6. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-018.<n>")]` on those proving criteria 1, 2 and 4, and criterion 3 is proven by the existing sort tests passing with their expected values unchanged (they keep their `FEAT-001.11` trait); `dotnet test src/backend` and the 90% coverage gate pass; `src/backend/REPO_CONTEXT.md` mentions the name order if it describes sorting; and `docs/reports/TASK-018/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output.

## Out of scope
- Changing the `Name` column collation or any migration (would need an owner decision, ADR-002).
- Non-ASCII case folding (`NOCASE` folds ASCII only; documented in the contract).
- The search filter `q` (already case-insensitive for ASCII through `LIKE`).
- The location tree, products, and the other `/ef-verify` findings (the zone and bin race, the redundant `ORDER BY Id, Id`).

## Notes and links
- Owner decision (2026-11-26, `/start-ticket` step 6): `spec-drift-auditor` found 11 low-severity, doc-side drift rows in the `locations` component (path format wording, "ASCII only" for duplicate codes, two 409 descriptions, the singleton `/warehouse`, the ADR-003 error example, LOC-1.1 and Development, the timestamp wording, the `Zones.Type` length check, the undeclared 413, `Warehouse.code` maxLength). None concerns the name sort, and behaviour matches every LOC criterion. Continue with TASK-018; the rows are fixed in a separate `chore/` PR (row 8, the timestamp encoding, to be checked first).
- Owner decisions (2026-11-26, plan approval): criterion 4 is proven with a page-boundary test inside `allowed_paths`, not a `ToQueryString()` test; the existing sort tests get no `TASK-018.3` trait, their unchanged passing runs are the proof of criterion 3; when the review chain's `spec-drift-auditor` finds the same rows again, the decision above covers them and only new drift stops the ticket. `change-implementer` does steps 1 to 3 (query, contract, backend context file), `test-writer` step 4.
- Owner decisions (2026-11-26, after the review chain): a twelfth drift row is added to the first decision and fixed in the same `chore/` PR: the contract never states the order of the zone `type` sort (the code sorts Receiving, Storage, Shipping, and its comment says the contract wants enum order). Criterion 6 is reworded to match the plan decision on criterion 3 (`independent-code-review` flagged the conflict). No optional code tidy-ups.
- The sort tests are `Zones_sort_by_every_allowed_field_with_the_id_as_tie_break` and `Bins_sort_by_every_allowed_field_with_the_id_as_tie_break` in `LocationListApiTests.cs`; the seeds use capitalised names only, so the new tests need their own mixed-case seed.
- Zones sort through `ApplySort` (`Application/Common/Paging/QueryablePagingExtensions.cs`) with the allow-list in `Application/Locations/LocationRules.cs`; bins sort in the `switch` in `LocationQueries.cs`. The allow-list itself does not change.
- A test can prove the SQL (criterion 4) with `ToQueryString()` on the query, or by checking that the order is right on a page boundary with `pageSize` smaller than the result.
- Run `/ef-verify TASK-018` after the change; `/review-ticket` runs it automatically because a path under `MiniWms.Infrastructure` changes.
- `/start-ticket` now runs `spec-drift-auditor` on the `locations` component before the plan and stops on any drift. `docs/specs/api/locations.yaml` had drift rows in the last audit (for example the undeclared 413), so expect that stop and decide whether to fix first or record a decision to continue.
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).

## Delivery
PR 69. All 6 acceptance criteria PASS (criterion 3 by the existing sort tests, 5 and 6 by the diff and real runs); results in `docs/reports/TASK-018/ac-results.md` and the row in `docs/reports/ac-log.md`. Reverting the fix makes exactly the 6 new cases fail. The 12 doc-side drift rows in `locations` and the three `/code-review` findings are follow-ups.
