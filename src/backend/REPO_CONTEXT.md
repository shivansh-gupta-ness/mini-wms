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
| `MiniWms.Application.Tests` | use-case tests, architecture and repository-structure tests | Application, Domain |
| `MiniWms.Api.Tests` | integration tests against the real host | Api |

Each source project has `Common/`. `MiniWms.Domain` also has one folder per module: `Locations`, `Products`, `Inventory`, `Inbound`, `Outbound`, `Auth`.
The other projects get the same module folders when a module's first ticket needs them. Empty folders carry a `.gitkeep`.
Put new code in the module folder, not at the project root.

## Build, test, run
- Build: `dotnet build src/backend` (warnings are errors; must finish with 0 warnings).
- Test: `dotnet test src/backend`.
- Run the API: `dotnet run --project src/backend/MiniWms.Api` (host behaviour arrives in TASK-002; port 5080).
- The pinned SDK is in the root `global.json` (10.0.300). Tools may not be on PATH in agent shells: use `C:\Program Files\dotnet`.

## Rules (ADR-001)
- Business rules live in Domain or Application, never in endpoints or EF configurations.
- A module may use another module's Application interfaces, never its Infrastructure types.
- Only Api references Infrastructure, and only as the composition root.
- Package versions live only in `Directory.Packages.props`; `Directory.Build.props` sets nullable, implicit usings and warnings as errors.

## Tests
- xUnit. A test that proves an acceptance criterion carries `[Trait("AC", "<ticket or spec id>")]`, for example `TASK-001.2` or `LOC-1.1`.
- Repository-structure tests walk up from the test folder to the folder holding `global.json` to find the repo root.
- No coverage tooling yet (TASK-003); no database yet (TASK-004).
