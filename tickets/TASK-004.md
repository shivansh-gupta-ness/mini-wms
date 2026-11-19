---
id: TASK-004
type: task
title: EF Core SQLite persistence foundation
status: done
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
1. Given the existing `.config/dotnet-tools.json` (created by TASK-003 with ReportGenerator; keep that entry), when `dotnet tool restore` runs, then a local `dotnet-ef` of version 10.x is installed from it as well (the global 11.x is not used).
2. Given any connection, when it opens, then foreign keys are on, the journal mode is WAL and `busy_timeout` is 5000.
3. Given an entity with audit fields, when it is created or updated, then `CreatedAt/By` and `UpdatedAt/By` are set centrally in `SaveChanges` from a clock and a current-user abstraction, and `Version` increases on update.
4. Given a stale `Version`, when an update is saved, then a concurrency exception is raised and nothing is stored.
5. Given `ConnectionStrings:Default`, when the API starts in Development, then the database file is created under `src/backend/MiniWms.Api/App_Data/` and pending migrations are applied.
6. Given `.gitignore`, when the database file and its WAL sidecar files (`*.db-wal`, `*.db-shm`) exist, then git lists none of them.
7. Given the test fixture, when an integration test runs, then it gets its own temporary database file (unique name per test) with migrations applied, and the file and its sidecar files are removed afterwards. *(AC 6 and 7 amended on 2026-11-19 with the owner's approval of the scout's findings: WAL mode creates sidecar files, and the database is per test, not per test run.)*
8. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-004.<n>")]` on those proving the ACs above.
9. Given the existing API tests (`MiniWms.Api.Tests`, which start the real host in Development), when the host now creates and migrates a database at startup, then those tests use a temporary database file through a `ConnectionStrings:Default` override in the test factory, never `App_Data/miniwms.db`, and all existing tests still pass when run in parallel.

## Out of scope
- Module tables and the first migration (TASK-005).
- Authentication.

## Notes and links
- Analysis before readying (2026-11-19): AC 1 and AC 9 were added or sharpened because TASK-003 already created the tool manifest, and because the existing 46 Api tests would otherwise all start touching the real database file once the host migrates at startup (parallel runs would contend for it).
- Coverage: `coverage.runsettings` counts only Domain, Application and Api (ADR-005), so the new Infrastructure code is tested but not part of the 90% gate. Whether to add Infrastructure to the gate is an ADR-005 decision for the owner, not part of this ticket.
- `src/backend/MiniWms.Api/Program.cs` (changed by BUG-001) is where the host registers persistence; keep its middleware order (`UseCors` first) and the problem-details setup unchanged.
- Run the coverage gate and tests with the commands in `src/backend/REPO_CONTEXT.md` (the PowerShell script needs `-ExecutionPolicy Bypass` on this machine).
- Record AC results in `docs/reports/TASK-004/ac-results.md` and one row in `docs/reports/ac-log.md`.
