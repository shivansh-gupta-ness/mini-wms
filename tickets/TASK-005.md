---
id: TASK-005
type: task
title: Locations and Products schema, first migration and warehouse seed
status: done
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, tickets/TASK-005.md, docs/reports/TASK-005/**, docs/reports/ac-log.md]
docs: [docs/specs/data-model.md, docs/adr/ADR-002-ef-core-sqlite.md]
parent: 
depends_on: [TASK-004]
---

## Description
Add the entities, EF configurations and the first migration for the first two modules: Warehouse, Zone, Bin and Product, plus the single seeded warehouse. Set `status: ready` after TASK-004 is merged.

## Acceptance criteria
1. Given an empty database, when the migrations are applied, then tables for Warehouse, Zone, Bin and Product exist with the columns, lengths, keys and constraints in `docs/specs/data-model.md`.
2. Given a fresh database, when the app starts, then exactly one warehouse `MAIN` named `Main warehouse` exists, and starting again does not add a second.
3. Given two zone codes, bin codes or product SKUs that differ only by case, when the second is saved, then the database rejects it (case-insensitive unique index).
4. Given a bin that points at a missing zone, when it is saved, then the database rejects it (foreign key).
5. Given a zone whose type is not Receiving, Storage or Shipping, when it is saved, then it is rejected.
6. Given the entities, when business rules are tested, then the invariants for zone type, active flags and reorder level in the data model are enforced in Domain, with unit tests.
7. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-005.<n>")]` on those proving the ACs above.

8. Given `AppDbContext`, when the first migration is created with the local `dotnet-ef` (`dotnet tool run dotnet-ef migrations add`), then the migration and model snapshot are committed under `src/backend/MiniWms.Infrastructure/Migrations/`, and the model has no pending changes.

## Out of scope
- Endpoints or use cases for Locations and Products (module tickets).
- Tables for later modules.

## Notes and links
- TASK-004 stores `DateTimeOffset` as binary and sets audit fields and `Version` centrally; new entities derive from `AuditableEntity`. Tests use the temporary-database fixture in `MiniWms.Api.Tests/Persistence`, never `App_Data/miniwms.db`.
- Seeding runs at startup in Development only, like the migration, and must be idempotent.
- Keep the PR within the limits (2000 lines, 60 files); if the first migration is large, say so in the brief.
- Record AC results in `docs/reports/TASK-005/ac-results.md` and one row in `docs/reports/ac-log.md`.
- Owner decisions (2026-11-19, on the scout's brief): seeding is Development only like the migration; no database constraint for "exactly one warehouse" (seeder plus no endpoints); Warehouse `Code` max length 20; Domain enforces the stock-free rules now and takes stock-dependent facts (has bins, has active bins, total on hand) as parameters, the stock-based checks wait for the inventory module; max lengths are enforced with CHECK constraints (`length(col) <= n`) because SQLite ignores `HasMaxLength`; code and SKU unique indexes use NOCASE (ASCII only, accepted); Bin code unique across the single warehouse. `docs/specs/product.md` (LOC and PRD rules) is also a source.
