---
id: TASK-004
type: task
title: EF Core SQLite persistence foundation
status: draft
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, .config/dotnet-tools.json, .gitignore, tickets/TASK-004.md, docs/reports/TASK-004/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-002-ef-core-sqlite.md, docs/specs/data-model.md]
parent: 
depends_on: [TASK-001, TASK-003]
---

## Description
Add EF Core 10 with SQLite to `MiniWms.Infrastructure` as ADR-002 describes: the `DbContext` base, connection settings, central audit and version handling, the local `dotnet-ef` tool and a temporary-database test fixture. No module tables yet (TASK-005). Set `status: ready` after TASK-001 and TASK-003 are merged.

## Acceptance criteria
1. Given `.config/dotnet-tools.json`, when `dotnet tool restore` runs, then a local `dotnet-ef` of version 10.x is installed (the global 11.x is not used).
2. Given any connection, when it opens, then foreign keys are on, the journal mode is WAL and `busy_timeout` is 5000.
3. Given an entity with audit fields, when it is created or updated, then `CreatedAt/By` and `UpdatedAt/By` are set centrally in `SaveChanges` from a clock and a current-user abstraction, and `Version` increases on update.
4. Given a stale `Version`, when an update is saved, then a concurrency exception is raised and nothing is stored.
5. Given `ConnectionStrings:Default`, when the API starts in Development, then the database file is created under `src/backend/MiniWms.Api/App_Data/` and pending migrations are applied.
6. Given `.gitignore`, when the database file exists, then git does not list it.
7. Given the test fixture, when an integration test runs, then it gets its own temporary database file with migrations applied, and the file is removed afterwards.
8. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-004.<n>")]` on those proving the ACs above.

## Out of scope
- Module tables and the first migration (TASK-005).
- Authentication.

## Notes and links
- Record AC results in `docs/reports/TASK-004/ac-results.md` and one row in `docs/reports/ac-log.md`.
