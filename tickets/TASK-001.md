---
id: TASK-001
type: task
title: Backend solution skeleton and build configuration
status: done
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, global.json, tickets/TASK-001.md, docs/reports/TASK-001/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-001-solution-layering.md]
parent: 
depends_on: []
---

## Description
Create the .NET 10 solution described in ADR-001: four source projects and three test projects with the allowed references, shared build settings and a short REPO_CONTEXT. No host behaviour yet (TASK-002). This is the first ticket through the agent harness, so keep it small and report anything the guards got wrong.

## Acceptance criteria
1. Given the repository, when `dotnet build src/backend` runs, then `src/backend/MiniWms.sln` builds with zero warnings (warnings are errors) and contains `MiniWms.Domain`, `MiniWms.Application`, `MiniWms.Infrastructure`, `MiniWms.Api`, `MiniWms.Domain.Tests`, `MiniWms.Application.Tests` and `MiniWms.Api.Tests`.
2. Given the project references, when an architecture test inspects them, then Domain references nothing, Application references only Domain, Infrastructure references only Application and Domain, and Api references Application and Infrastructure only (ADR-001).
3. Given `global.json` at the repository root, when `dotnet --version` runs inside the repo, then the pinned .NET 10 SDK is used.
4. Given `src/backend/Directory.Build.props` and `.editorconfig`, when any project is built, then nullable reference types and implicit usings are on and warnings are errors.
5. Given `src/backend/Directory.Packages.props`, when a package is referenced, then its version comes from this central file, never from a project file.
6. Given the module folders in ADR-001, when a developer opens each source project, then `Common/` exists and the module folders `Locations`, `Products`, `Inventory`, `Inbound`, `Outbound` and `Auth` exist as empty placeholders where a project needs them.
7. Given `dotnet test src/backend`, when it runs, then every test passes and each test that proves an AC above carries `[Trait("AC", "TASK-001.<n>")]`.
8. Given `src/backend/REPO_CONTEXT.md`, when read, then it lists the solution layout, the build, test and run commands and the ADR-001 rules (under 80 lines).

## Out of scope
- The API host, endpoints and error handling (TASK-002).
- Coverage tooling (TASK-003).
- EF Core and SQLite (TASK-004).

## Notes and links
- Record AC results in `docs/reports/TASK-001/ac-results.md` and one row in `docs/reports/ac-log.md`.

## Delivery
Delivered in PR 8. All 8 criteria PASS; results in `docs/reports/TASK-001/ac-results.md`, the row in `docs/reports/ac-log.md`.
