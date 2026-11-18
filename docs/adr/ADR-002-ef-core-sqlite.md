# ADR-002: EF Core with SQLite

Status: Proposed (accepted when merged)
Date: 2026-11-18
Owner: shivansh-gupta-ness

## Context
The app is never deployed (NFR-1) and runs on one developer machine. We need a real relational store with constraints and transactions, with no server to install. SQLite fits and is already available through NuGet.

## Decision
- **Provider:** `Microsoft.EntityFrameworkCore.Sqlite` (EF Core 10). The SQLite engine ships inside the package; no separate install is needed.
- **Database file:** `src/backend/MiniWms.Api/App_Data/miniwms.db`, git-ignored. The path comes from `ConnectionStrings:Default`, so tests can point at a temporary file.
- **Connection settings applied on every connection:** `PRAGMA foreign_keys = ON`, `PRAGMA journal_mode = WAL`, `PRAGMA busy_timeout = 5000`.
- **Migrations:** code-first, in `MiniWms.Infrastructure/Migrations`. Created with a **local** `dotnet-ef` tool pinned to a 10.x version in `.config/dotnet-tools.json` (the machine has a global 11.x, which must not be used). Migrations are applied on startup in the Development environment only.
- **Seeding:** the single warehouse (`MAIN`, `Main warehouse`) is created by the first migration or a start-up seeder, from configuration (LOC-1.1). A development Admin user is seeded only while `Auth:DevAdmin` is true (ADR-004).
- **Types:** quantities are `INTEGER` (whole numbers); timestamps are UTC, stored as ISO 8601 text; codes use `COLLATE NOCASE` unique indexes so SKU and codes compare case-insensitively (PRD-1.2).
- **Concurrency:** each mutable row has an integer `Version` concurrency token incremented on update (SQLite has no row-version type). A stale update returns 409.
- **Transactions:** every use case that changes stock runs in one transaction (NFR-4). Stock is changed only through the Inventory module (ADR-001).
- **Audit fields:** `createdAt/By`, `updatedAt/By` are set centrally in `SaveChanges`, not in each use case.
- **Tests:** integration tests create a temporary database file per test run and apply migrations; no in-memory provider substitutes, so constraints behave as in real use.

## Consequences
- One writer at a time in SQLite is fine for one developer and for tests; multi-machine concurrency is out of scope.
- `ALTER TABLE` limits in SQLite mean some migrations rebuild a table; EF Core handles this, but migrations must be reviewed.
- Moving to another database later would mean changing the provider, the `COLLATE NOCASE` indexes and the `Version` token.
