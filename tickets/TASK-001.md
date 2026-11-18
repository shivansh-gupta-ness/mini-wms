---
id: TASK-001
type: task
title: Backend solution skeleton with health endpoint
status: ready
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, tickets/TASK-001.md, docs/reports/TASK-001/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-001-solution-layering.md, docs/adr/ADR-003-api-conventions.md, docs/adr/ADR-005-testing-coverage-and-ac-log.md]
parent: 
depends_on: []
---

## Description
Create the .NET 10 backend solution described in ADR-001: four source projects and three test projects, with the project references the ADR allows, a minimal API host and a health endpoint. No database yet (that is TASK-002). This is the first ticket through the agent harness, so keep the change small and report anything the guards got wrong.

## Acceptance criteria
1. Given the repository, when `dotnet build src/backend` runs, then `src/backend/MiniWms.sln` builds with zero warnings (warnings are errors) and contains `MiniWms.Domain`, `MiniWms.Application`, `MiniWms.Infrastructure`, `MiniWms.Api`, `MiniWms.Domain.Tests`, `MiniWms.Application.Tests` and `MiniWms.Api.Tests`.
2. Given the project references, when an architecture test inspects them, then Domain references nothing, Application references only Domain, Infrastructure references only Application and Domain, and Api references Application and Infrastructure only (ADR-001).
3. Given the API is running, when `GET /api/v1/health` is called with no session, then it returns 200 and JSON `{ "status": "ok" }`.
4. Given an unknown route, when it is called, then the response is `application/problem+json` with `status` 404 and no stack trace (ADR-003).
5. Given an unhandled exception in an endpoint, when it occurs, then the response is `application/problem+json` with `status` 500, a generic `title` and no exception details (NFR-3).
6. Given a request from `http://localhost:4200`, when it calls the health endpoint, then the response allows that origin and no other (ADR-003 CORS).
7. Given the API starts locally, when it is launched with its default profile, then it listens on `http://localhost:5080`.
8. Given the tests, when `dotnet test src/backend` runs, then every test passes, and each test that proves an AC above carries `[Trait("AC", "TASK-001.<n>")]`.
9. Given the new code, when a developer reads `src/backend/REPO_CONTEXT.md`, then it lists the solution layout, the build, test and run commands, and the conventions from ADR-001 and ADR-003 (under 80 lines).

## Out of scope
- EF Core, SQLite and migrations (TASK-002).
- Authentication (module AUT).
- Any module endpoint (Locations, Products and so on).
- CI workflows.

## Notes and links
- Layout, rules and module folders: `docs/adr/ADR-001-solution-layering.md`.
- Error format and CORS: `docs/adr/ADR-003-api-conventions.md`.
- Use minimal APIs and one `MapXxxEndpoints` extension per module; the health endpoint lives in `Common/`.
- Add a `Directory.Build.props` (nullable on, implicit usings, warnings as errors) and an `.editorconfig` under `src/backend`.
- Test and coverage tooling per ADR-005 (xUnit, coverlet.collector). Do not add other test libraries.
- Record AC results in `docs/reports/TASK-001/ac-results.md` and one row in `docs/reports/ac-log.md`.
