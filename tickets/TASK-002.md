---
id: TASK-002
type: task
title: EF Core SQLite persistence for Locations and Products
status: draft
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, .config/dotnet-tools.json, .gitignore, tickets/TASK-002.md, docs/reports/TASK-002/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-002-ef-core-sqlite.md, docs/specs/data-model.md, docs/adr/ADR-005-testing-coverage-and-ac-log.md]
parent: 
depends_on: [TASK-001]
---

## Description
Add EF Core 10 with SQLite to `MiniWms.Infrastructure` as ADR-002 describes, with the tables the first two modules need (Warehouse, Zone, Bin, Product), the first migration, seeding of the single warehouse, and the audit and version behaviour from the data model. Set `status: ready` after TASK-001 is merged.

## Acceptance criteria
1. Given a clean machine, when `dotnet tool restore` runs, then a local `dotnet-ef` tool of version 10.x is installed from `.config/dotnet-tools.json` (the global 11.x is not used).
2. Given an empty database path, when the migrations are applied, then tables `Warehouses`, `Zones`, `Bins` and `Products` exist with the columns, keys and constraints in `docs/specs/data-model.md`.
3. Given a fresh database, when the app starts, then exactly one warehouse with code `MAIN` and name `Main warehouse` exists, and starting again does not add a second.
4. Given two zones whose codes differ only by case, when the second is saved, then the database rejects it (case-insensitive unique index); the same holds for bin codes and product SKUs.
5. Given a zone, bin or product row, when it is created or updated, then `CreatedAt/By` and `UpdatedAt/By` are set centrally in `SaveChanges`, and `Version` is incremented on update.
6. Given a stale `Version`, when an update is saved, then a concurrency error is raised and no change is stored.
7. Given any connection, when it opens, then foreign keys are on (a bin pointing at a missing zone is rejected), the journal mode is WAL, and `busy_timeout` is 5000.
8. Given the integration tests, when they run, then each uses its own temporary database file with migrations applied and removes it afterwards, and all tests pass with `[Trait("AC", "TASK-002.<n>")]` on the tests that prove the ACs above.
9. Given `.gitignore`, when the API creates `src/backend/MiniWms.Api/App_Data/miniwms.db`, then git does not list it.

## Out of scope
- Any endpoint or use case for Locations or Products (module tickets).
- Tables for Inventory, Inbound, Outbound and Auth (added with their modules).
- Production seeding of users.

## Notes and links
- `docs/adr/ADR-002-ef-core-sqlite.md`, `docs/specs/data-model.md`.
- Prefer the real SQLite provider in tests over the EF in-memory provider.
- Record AC results in `docs/reports/TASK-002/ac-results.md` and one row in `docs/reports/ac-log.md`.
