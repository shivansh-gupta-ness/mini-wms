# Backend: repository context

.NET 10 Web API for Mini WMS. Decisions: `docs/adr/ADR-001` (layering), `ADR-002` (EF Core + SQLite), `ADR-003` (API conventions), `ADR-005` (testing).
The root `REPO_CONTEXT.md` covers the whole repository; this file covers `src/backend` only.

## Layout
`src/backend/MiniWms.sln` holds seven projects. Dependencies point inward only.

| Project | Holds | May reference |
|---|---|---|
| `MiniWms.Domain` | entities, rules, domain exceptions | nothing |
| `MiniWms.Application` | use cases, DTOs, validators, interfaces | Domain |
| `MiniWms.Infrastructure` | EF Core, SQLite, repositories, clock | Application, Domain |
| `MiniWms.Api` | endpoints, error handling, composition root (`Program.cs`) | Application, Infrastructure |
| `MiniWms.Domain.Tests` | unit tests of rules | Domain |
| `MiniWms.Application.Tests` | use-case tests, project-reference and Domain-package tests | Application, Domain |
| `MiniWms.Api.Tests` | integration tests against the real host | Api |

Each source project has `Common/`. `MiniWms.Domain` also has one folder per module: `Locations`, `Products`, `Inventory`, `Inbound`, `Outbound`, `Auth`.
The other projects get the same module folders when a module's first ticket needs them. Empty folders carry a `.gitkeep`.
Put new code in the module folder, not at the project root.

## Build, test, run
- Build: `dotnet build src/backend` (warnings are errors; must finish with 0 warnings).
- Test: `dotnet test src/backend`.
- Coverage (ADR-005; scope Domain, Application, Api; excludes `Program.cs`, migrations, generated code; settings in `coverage.runsettings`):
  - `dotnet tool restore` installs ReportGenerator from `.config/dotnet-tools.json`.
  - `dotnet test src/backend --collect:"XPlat Code Coverage"` writes a Cobertura file per test project.
  - `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` runs tests, writes the text summary under `src/backend/TestResults/coverage-report` and gates at 90% line coverage (exit 1 below).
  - `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1 -Cobertura <path>` skips the tests; report and gate only. A relative path resolves against your current folder.
  - Why `-ExecutionPolicy Bypass`: the Windows default policy blocks scripts; the flag applies to that one run only.
  - `dotnet test src/backend --filter "AC=<id>"` runs the tests of one acceptance criterion.
- Run the API: `dotnet run --project src/backend/MiniWms.Api` (http://localhost:5080).
- Health is `GET /api/v1/health`. CORS allows `http://localhost:4200` only and is applied before the error handlers.
- The pinned SDK is in the root `global.json` (10.0.300). Tools may not be on PATH in agent shells: use `C:\Program Files\dotnet`.

## Rules (ADR-001)
- Business rules live in Domain or Application, never in endpoints or EF configurations.
- A module may use another module's Application interfaces, never its Infrastructure types.
- Only Api references Infrastructure, and only as the composition root.
- Package versions live only in `Directory.Packages.props`; `Directory.Build.props` sets nullable, implicit usings and warnings as errors.

## Tests
- xUnit. A test that proves an acceptance criterion carries `[Trait("AC", "<ticket id>.<n>")]`, for example `[Trait("AC", "FEAT-001.3")]` (spec ids such as LOC-1.1 are not used on tests).
- Repository-structure tests walk up from the test folder to the folder holding `global.json` to find the repo root.
- Coverage tooling: see Build, test, run.

## Persistence (ADR-002)
- `MiniWms.Infrastructure/Persistence`: `AppDbContext` (audit fields and `Version` set in `SaveChanges` from `IClock` and `ICurrentUser`), `SqliteConnectionInterceptor` (foreign keys on, WAL, busy timeout 5000 on every open), design-time factory. Wiring: `AddInfrastructure(configuration, contentRoot)`.
- Domain entities derive from `MiniWms.Domain.Common.AuditableEntity`. The current user is the fixed `dev-admin` until Auth exists.
- Connection string `ConnectionStrings:Default` (`appsettings.json`) is `Data Source=App_Data/miniwms.db`, relative to the Api content root. Development start-up creates the folder and runs `Database.Migrate()`. `App_Data/`, `*.db`, `*.db-wal`, `*.db-shm` are git-ignored.
- Locations and Products tables (Warehouses, Zones, Bins, Products) are in migration `InitialLocationsProducts`. Entity configurations live in `Persistence/Configurations` (applied by `ApplyConfigurationsFromAssembly`). SQLite ignores `HasMaxLength`, so every max length also has a `CHECK (length(col) <= n)`; Code and Sku columns are `NOCASE` with unique indexes (ASCII only). `DateTimeOffset` columns are stored as binary UTC ticks (INTEGER).
- Seed: in Development, right after `Migrate()`, `WarehouseSeeder` adds the warehouse from `Seed:Warehouse:Code` / `Name` (default `MAIN` / `Main warehouse`) only if no warehouse exists at all.
- Domain rules that need stock facts take them as parameters (`hasBins`, `hasActiveBins`, `totalOnHand`); the stock-based checks are wired in the inventory module.
- Test fixture: `TemporaryDatabase` applies the migrations through `AppDbContext` and creates only the test-only `Items` table itself. Never re-create tables that a migration owns; a new migration needs no fixture change.
- `Migrations/.editorconfig` marks generated code so the build's style rules skip it.
- Migrations go in `MiniWms.Infrastructure/Migrations`. Use the local tool, never the global 11.x: `dotnet tool restore`, then `dotnet tool run dotnet-ef migrations add <Name> --project src/backend/MiniWms.Infrastructure` (same prefix for `database update`, `dbcontext info`).

## API building blocks (TASK-006, ADR-003)
- Errors: `ValidationFailedException` (Domain; field rules: blank, too long, negative, invalid type; carries camelCase field names) and FluentValidation's `ValidationException` -> 400 with `errors`. `DomainException` (business-rule conflicts) and `DbUpdateConcurrencyException` -> 409 `conflict` with `detail`. Handlers are `IExceptionHandler`s in `Api/Common/Errors`, registered in `Program.cs`; anything else stays a generic 500.
- `detail` is kept by the problem-details customizer only when one of our handlers marks the response and the status is 4xx. Never use it for 401/403/404, 5xx or unexpected exceptions.
- Lists: `Application/Common/Paging` (`PagedQuery` page 1, size 25, max 100; `PagedResult<T>` = items, page, pageSize, totalItems), `Common/Sorting` (`sort=field` or `-field`, one field, per-module `SortFields<T>` allow-list, `Id` tie-breaker, unknown field is a field-level 400), `Common/Validation/PagedQueryValidator<T>`. `ApplySort` + `ToPagedResult` work on any `IQueryable`.
- Updates: apply the client's version with `db.UseClientVersion(entity, version)` (`Infrastructure/Persistence/VersionedUpdate`) before `SaveChanges`; `ApplyAudit` overwrites a directly set `Version`.

## Locations module (FEAT-001, contract `docs/specs/api/locations.yaml`)
- `Application/Locations`: one file per use case (command, validator, handler), DTOs, `ILocationStore` (write side) and `ILocationQueries` (read side, DTOs). `Infrastructure/Locations`: `LocationStore`, `LocationQueries`, `ZeroStockQuery`. `Api/Locations`: `AddLocations()` and `MapLocationsEndpoints()` (called from `Program.cs`), under `/api/v1`.
- Routes: `/warehouse` (GET, PATCH; POST and DELETE are 405), `/zones` and `/bins` (GET list, POST 201 with `Location`), `/zones/{id}` and `/bins/{id}` (GET, PATCH), `/zones/{id}/deactivate` and `/bins/{id}/deactivate` (POST `{version}`), `/locations/tree` (unpaged). Unknown id is 404; a non-numeric id matches no route (404).
- Endpoints read the body and the query string themselves, so bad JSON or a bad `page`, `pageSize`, `zoneId`, `isActive` is a field-level 400, not ASP.NET's bare 400.
- `IStockQuery` (`Application/Common`; `BinOnHandAsync`, `ProductOnHandAsync`) is registered with `TryAdd` as `ZeroStockQuery` (always 0) until Inventory registers the real one; tests replace it with a fake.
- A duplicate zone or bin code (case-insensitive, NOCASE column) is a 400 under `code`, found by the use-case check; the unique index is the safety net and `LocationStore.SaveAsync` maps its violation to the same 400.
