---
id: FEAT-001
type: feature
title: Locations API: warehouse, zones, bins and the location tree
status: draft
owner: shivansh-gupta-ness
component: locations
risk: low
allowed_paths: [src/backend/**/Locations/**, src/backend/MiniWms.Api/Program.cs, src/backend/MiniWms.Infrastructure/DependencyInjection.cs, src/backend/MiniWms.Infrastructure/Persistence/**, src/backend/MiniWms.Application/Common/**, src/backend/MiniWms.Api.Tests/Host/**, src/backend/REPO_CONTEXT.md, docs/specs/api/locations.yaml, tickets/FEAT-001.md, docs/reports/FEAT-001/**, docs/reports/ac-log.md]
docs: [docs/specs/product.md, docs/specs/data-model.md, docs/specs/api/locations.yaml, docs/adr/ADR-001-solution-layering.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [TASK-005, TASK-006]
---

## Description
Build the backend of the Locations module (LOC-1 to LOC-3) on the foundation: the use cases, the endpoints in `docs/specs/api/locations.yaml` (warehouse, zones, bins, location tree) and their tests. The entities, the first migration and the seeded `MAIN` warehouse exist (TASK-005); paging, sorting, validation and error mapping exist (TASK-006). Set `status: ready` after the open decisions below are answered and TASK-005 and TASK-006 are merged.

## Acceptance criteria
1. (LOC-1.1) Given a fresh database, when I call `GET /api/v1/warehouse`, then I get the single warehouse (`id`, `code`, `name`, `isActive`, `version`); and no route can create or delete a warehouse.
2. (LOC-2.1, warehouse) Given the warehouse, when I `PATCH /api/v1/warehouse` with a new name and the current `version`, then the name is saved and the code is unchanged; a blank name or one over 100 characters returns 400 with the field under `name`; a stale `version` returns 409 `conflict`.
3. (LOC-1.2) Given a unique code, a name and a type of Receiving, Storage or Shipping, when I `POST /api/v1/zones`, then I get 201 with a `Location` header and the zone is saved as Active.
4. (LOC-1.4, zone) Given a zone request with a missing code, name or type, an invalid type, or a code already used in the warehouse (compared without regard to case), then it is rejected with a field-level message keyed by the field and nothing is saved.
5. (LOC-1.3) Given an Active zone, when I `POST /api/v1/bins` with a code unique across the warehouse and a name, then I get 201 and the bin is saved as Active under that zone and returned with its `path` (`<zone code>/<bin code>`) and `zoneCode`.
6. (LOC-1.4, bin) Given a bin request with a missing code, name or zone, an unknown or Inactive zone, or a code already used in the warehouse (compared without regard to case), then it is rejected with a field-level message and nothing is saved.
7. (LOC-2.1, zone and bin) Given a zone or bin, when I `PATCH` it with a new name and the current `version`, then the name is saved and the code is unchanged; a zone's type changes only while it has no bins, otherwise 409 with a message; a stale `version` returns 409.
8. (LOC-2.3) Given a bin that holds no stock, when I `POST /api/v1/bins/{id}/deactivate` with the current `version`, then it is Inactive and is still returned by id and in the list, and it is excluded when the list is filtered with `isActive=true`, so it cannot be chosen for new stock.
9. (LOC-2.2) Given a bin that holds stock, when I try to deactivate it, then it is rejected with 409 and a message naming the quantity on hand; the stock figure comes from the stock query abstraction described in the notes.
10. (LOC-2.4) Given a zone with any Active bin, when I try to deactivate it, then it is rejected with 409; a zone with no Active bin is deactivated.
11. (LOC-3.1) Given locations exist, when I call `GET /api/v1/zones` or `GET /api/v1/bins`, then I get a page (`items`, `page`, `pageSize`, `totalItems`; default 25, maximum 100) sortable by one allowed field (zones: `code`, `name`, `type`, `isActive`; bins: `code`, `name`, `zone`, `isActive`) and filterable by `isActive`, `type` (zones) and `zoneId` (bins); and `GET /api/v1/locations/tree` returns each zone with its bins.
12. (LOC-3.2) Given I pass `q`, when I list zones or bins, then only those whose code or name contains the text (without regard to case) are returned.
13. (ADR-003) Given any of these endpoints, when a request fails, then the response is `application/problem+json` as built in TASK-006 (400 with `errors`, 404 `not-found`, 409 `conflict`); every update sends the client `version` through the versioned-update helper so a stale version is detected.
14. Given the contract `docs/specs/api/locations.yaml`, when the Spectral lint from TASK-012 runs, then it still reports nothing; the contract is changed only where this ticket's decisions below say so, and the change is listed in the PR.
15. Given the tests, when they run, then all pass with `[Trait("AC", "FEAT-001.<n>")]` on those proving the ACs above, the gates are met (mapping 100, pass 100, coverage 90 on Domain, Application and Api), and `src/backend/REPO_CONTEXT.md` describes the module and the stock query.

## Out of scope
- Role checks ("Given I am an Admin"): there is no authentication yet (ADR-004, the Auth module); the endpoints are open and stamp the development user until then. Mark the role part of each Admin AC N/A with this reason.
- The real stock quantities (Inventory module): the stock query returns zero until Inventory exists.
- The frontend (FEAT-002).

## Notes and links
- Spec: `docs/specs/product.md` section 7 (LOC-1 to LOC-3), `docs/specs/data-model.md`, `docs/specs/api/locations.yaml`. Domain rules and entities already exist in `MiniWms.Domain/Locations` (TASK-005): `Zone.ChangeType(type, hasBins)`, `Zone.Deactivate(hasActiveBins)`, `Bin.Deactivate(totalOnHand)`; field rules throw `ValidationFailedException` (400), business conflicts throw `DomainException` (409).
- **Open owner decision 1 (duplicate codes: 400 or 409).** `product.md` (LOC-1.4) and `ui.md` say a duplicate code is "rejected with a field-level message", but `locations.yaml` says a duplicate code returns 409 (which the UI shows as a banner). Recommended: 400 with the message under `code`, found by a check in the use case (the unique index stays as the safety net); change the contract descriptions and responses of `createZone` and `createBin` accordingly (the contract file is in `allowed_paths`). Alternative: keep 409 and update the UI spec.
- **Open owner decision 2 (stock query).** LOC-2.2 needs the quantity on hand of a bin, which belongs to Inventory (ADR-001). Recommended: an `IStockQuery` in `MiniWms.Application/Common` (`BinOnHand(binId)`, `ProductOnHand(productId)`), registered with a default implementation that returns 0 until the Inventory module replaces it; tests use a fake that returns stock. FEAT-003 reuses it for products. Alternative: leave LOC-2.2 as NOT RUN until Inventory exists.
- **Open owner decision 3 (Admin role).** Recommended: no role check now (see Out of scope); the Auth module adds it later with its own ACs.
- Layering (ADR-001): use cases and DTOs in `MiniWms.Application/Locations`, EF queries in `MiniWms.Infrastructure`, endpoints in `MiniWms.Api/Locations` registered with a `MapLocationsEndpoints` extension called from `Program.cs`; Application never references EF Core. Reuse `PagedQuery`, `SortFields`, `ToPagedResultAsync`, `UseClientVersion`, the validators and the error handlers from TASK-006; keep the middleware order and the problem-details customizer unchanged.
- Tests: unit tests for the use cases in `MiniWms.Application.Tests`, API tests through `ApiFactory` (temporary database, never `App_Data`) in `MiniWms.Api.Tests`. The `list`/`tree` queries must be proven on SQLite, not only in memory.
- Slicing: if the PR would grow past about 4000 lines, split warehouse and zones from bins.
- The feature pipeline appends a `## Delivery` section to this file, files reports in `docs/reports/FEAT-001/` and appends a row to `docs/reports/ac-log.md`, so all three stay in `allowed_paths`.
