---
id: TASK-021
type: task
title: Build the case-insensitive name order in one place
status: ready
owner: shivansh-gupta-ness
component: locations
risk: low
allowed_paths: [src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs, src/backend/MiniWms.Api.Tests/Locations/LocationListApiTests.cs, tickets/TASK-021.md, docs/reports/TASK-021/**, docs/reports/ac-log.md]
docs: [docs/specs/api/locations.yaml, docs/adr/ADR-001-solution-layering.md, docs/adr/ADR-002-ef-core-sqlite.md]
parent: 
depends_on: []
---

## Description
TASK-018 made `sort=name` ignore case for zones and bins by ordering on `EF.Functions.Collate(Name, "NOCASE")`. That expression and the `"NOCASE"` string are now written four times in `ListZonesAsync` and `ListBinsAsync` (`LocationQueries.cs`), once per direction and list. `own-code-review` on TASK-018 flagged the repetition; the owner left it for a follow-up. If one copy changes, for example to a different collation, the zones and bins could sort differently. Move the expression into one place in `LocationQueries.cs`. This is a pure tidy-up: no change in behaviour or SQL.

## Acceptance criteria
1. Given `sort=name` and `sort=-name` on `GET /api/v1/zones` and `GET /api/v1/bins`, when they are called, then the order is unchanged: the `TASK-018` tests (case-insensitive order, equal names keep the `Id` order, the page-boundary test) pass with their expected values unchanged.
2. Given every other sort field and the default order, when the lists are called, then the results are unchanged: the `FEAT-001.11` sort tests pass with their expected values unchanged.
3. Given the change, when `LocationQueries.cs` is read:
   - the collation name `"NOCASE"` appears once, and the name-order expression is defined once for zones and once for bins (or once for both)
   - the ordering is still translated to SQL (`ORDER BY ... COLLATE NOCASE`), not done in memory
   - the Application layer gets no EF Core reference (ADR-001)
   - no migration or contract change is made
4. Given the tests, when they run, then `dotnet test src/backend` and the 90% coverage gate pass, and `docs/reports/TASK-021/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output. No new test is needed: criteria 1 and 2 are proven by the existing tests passing unchanged.

## Out of scope
- The `Name` column collation, migrations, and non-ASCII case folding.
- The sort allow-list (`LocationSortFields`) and `ApplySort`.
- The default bins order (TASK-019) and the search length rule (TASK-020).

## Notes and links
- The four copies are in the zone `name` branch of `ListZonesAsync` and the `"name"` arm of the `switch` in `ListBinsAsync`.
- `ZoneTypeOrder` at the top of the class is an existing example of a shared order expression.
- If TASK-019 merges first, rebase on it: both tickets touch `ListBinsAsync`.
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).
