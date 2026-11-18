# ADR-001: Backend solution layering

Status: Proposed (accepted when merged)
Date: 2026-11-18
Owner: shivansh-gupta-ness

## Context
The backend is a .NET 10 Web API for the modules in `docs/specs/product.md`. We need a structure that keeps business rules testable without a database, and that lets a ticket's `allowed_paths` and the knowledge map point at one module without touching the others.

## Decision
One solution, `src/backend/MiniWms.sln`, with four source projects and three test projects:

| Project | Holds | May reference |
|---|---|---|
| `MiniWms.Domain` | Entities, value objects, enums, business rules, domain exceptions | nothing |
| `MiniWms.Application` | Use cases (commands and queries), DTOs, validators, interfaces for storage and time | Domain |
| `MiniWms.Infrastructure` | EF Core `DbContext`, configurations, migrations, repositories, password hashing, clock | Application, Domain |
| `MiniWms.Api` | Endpoints, request and response mapping, error handling, authentication setup, `Program.cs` | Application, Infrastructure (composition root only) |
| `MiniWms.Domain.Tests` | Unit tests of rules | Domain |
| `MiniWms.Application.Tests` | Unit tests of use cases with fakes | Application, Domain |
| `MiniWms.Api.Tests` | Integration tests: real API against a temporary SQLite file | Api |

Inside each project, code is grouped by **module folder first**, then by kind: `Locations/`, `Products/`, `Inventory/`, `Inbound/`, `Outbound/`, `Auth/`, plus `Common/`. Example: `src/backend/MiniWms.Application/Locations/CreateZone.cs`.

Rules:
- Dependencies point inward only (Api, Infrastructure, then Application, then Domain). A reference that breaks this is a build error caught by an architecture test in `MiniWms.Application.Tests`.
- Business rules live in Domain or Application, never in endpoints or EF configurations.
- A module may use another module's Application interfaces, never its Infrastructure types.
- Endpoints use ASP.NET Core minimal APIs, one `MapXxxEndpoints` extension per module.

## Consequences
- Knowledge-map paths for a module are `src/backend/**/Locations/**` and its tests, so a ticket's `allowed_paths` stay narrow and `pr-guard` can check them.
- Slightly more files than a single project; accepted for testability and for small, reviewable PRs.
- Cross-module features (for example Receive changes stock) go through Application interfaces, which keeps the Inventory module the only writer of stock.
