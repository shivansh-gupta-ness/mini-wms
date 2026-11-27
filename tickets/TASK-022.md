---
id: TASK-022
type: task
title: Keep the duplicate-code error in one place
status: ready
owner: shivansh-gupta-ness
component: locations
risk: low
allowed_paths: [src/backend/MiniWms.Application/Locations/LocationRules.cs, src/backend/MiniWms.Infrastructure/Locations/LocationStore.cs, src/backend/MiniWms.Api.Tests/Locations/LocationErrorApiTests.cs, src/backend/REPO_CONTEXT.md, tickets/TASK-022.md, docs/reports/TASK-022/**, docs/reports/ac-log.md]
docs: [docs/specs/api/locations.yaml, docs/adr/ADR-001-solution-layering.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: []
---

## Description
A duplicate zone or bin code is refused in two places, each with its own copy of the error:
- `LocationRules.DuplicateCode()` (Application) for the normal check before saving.
- `LocationStore.SaveAsync` (Infrastructure) for the unique-index safety net, when a request loses a race. It builds `new ValidationFailedException("code", "Code is already used in the warehouse.")` inline.

The field name and message must stay identical, so the client sees the same 400 whichever path fires. Today nothing but a copy keeps them in step. Make `LocationStore` use the same source as the use cases. Infrastructure already references Application (ADR-001), so the existing factory can be used once it is visible to Infrastructure.

## Acceptance criteria
1. Given a duplicate zone or bin code (any case) caught by the use-case check, when `POST /api/v1/zones` or `POST /api/v1/bins` is called, then the response is unchanged: 400 validation problem, error under `code`, "Code is already used in the warehouse.". The existing duplicate tests in `ZoneApiTests.cs` and `BinApiTests.cs` pass unchanged.
2. Given a duplicate that slips past the check and hits the unique index (the race path), when it is saved, then the response is the same 400 with the same field and message: the existing race test in `LocationErrorApiTests.cs` passes unchanged.
3. Given the change, when the code is read:
   - the message text "Code is already used in the warehouse." appears once in production code
   - `LocationStore` builds its error from the same factory as the use cases
   - the Domain and Application layers gain no new references (ADR-001)
   - other unique-index failures, which are not a zone or bin code, still propagate as today
4. Given the tests, when they run, then `dotnet test src/backend` and the 90% coverage gate pass, and `docs/reports/TASK-022/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output. Add a `TASK-022.<n>` test only if a criterion is not already proven by an existing test.

## Out of scope
- The message text, the field name and the HTTP status (unchanged; the contract needs no edit).
- Other validation messages and the Domain guards.
- How the race is detected (the SQLite extended code 2067 and the column-name check stay as they are).

## Notes and links
- `LocationRules` is `internal` in `MiniWms.Application`. Options are to make `DuplicateCode()` (or a small class holding it) public, or to use `InternalsVisibleTo`. Choose in the plan; prefer the narrowest change.
- The race test is next to the comment "The unique index is the safety net behind the duplicate-code check" in `LocationErrorApiTests.cs`.
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).
