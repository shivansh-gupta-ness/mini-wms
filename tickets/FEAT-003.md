---
id: FEAT-003
type: feature
title: Products API: catalogue, search and deactivation
status: draft
owner: shivansh-gupta-ness
component: products
risk: low
allowed_paths: [src/backend/**/Products/**, src/backend/MiniWms.Api/Program.cs, src/backend/MiniWms.Infrastructure/DependencyInjection.cs, src/backend/MiniWms.Infrastructure/Persistence/**, src/backend/MiniWms.Application/Common/**, src/backend/REPO_CONTEXT.md, docs/specs/api/products.yaml, tickets/FEAT-003.md, docs/reports/FEAT-003/**, docs/reports/ac-log.md]
docs: [docs/specs/product.md, docs/specs/data-model.md, docs/specs/api/products.yaml, docs/adr/ADR-001-solution-layering.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [FEAT-001]
---

## Description
Build the backend of the Products module (PRD-1 to PRD-3): the use cases, the endpoints in `docs/specs/api/products.yaml` and their tests. It follows the pattern FEAT-001 sets (module folders, endpoint registration, the stock query). Set `status: ready` after FEAT-001 is merged and the open decisions below are answered.

## Acceptance criteria
1. (PRD-1.1) Given a unique SKU, a name, a unit of measure and a reorder level of zero or more, when I `POST /api/v1/products`, then I get 201 with a `Location` header and the product is saved as Active.
2. (PRD-1.2) Given a SKU that already exists (compared without regard to case), when I create another product with it, then it is rejected with a field-level message under `sku` and nothing is saved.
3. (PRD-1.3) Given a missing SKU, name or unit of measure, a SKU, name or unit longer than its maximum (40, 150 and 20), or a negative reorder level, then the product is rejected with a field-level message for each problem and nothing is saved.
4. (PRD-2.1) Given a product, when I `PATCH /api/v1/products/{id}` with a new name, unit of measure or reorder level and the current `version`, then the change is saved and the SKU is unchanged; a request that tries to change the SKU is rejected; a stale `version` returns 409 `conflict`; a missing product returns 404.
5. (PRD-2.2) Given a product with stock on hand, when I try to deactivate it, then it is rejected with 409 and a message giving the total on hand (from the stock query).
6. (PRD-2.3) Given a product with no stock, when I `POST /api/v1/products/{id}/deactivate` with the current `version`, then it is Inactive, is still returned by id and in the list with its history, and the list can exclude it with `isActive=true`.
7. (PRD-3.1) Given products exist, when I call `GET /api/v1/products`, then I get a page (`items`, `page`, `pageSize`, `totalItems`; default 25, maximum 100) sortable by `sku`, `name` or `isActive`, and filterable by `isActive`.
8. (PRD-3.2) Given I pass `q`, then only products whose SKU or name contains the text (without regard to case) are returned.
9. (PRD-3.3) Given a product, when I call `GET /api/v1/products/{id}`, then I get its details and the total on hand across all bins (zero until Inventory exists).
10. (ADR-003) Given any of these endpoints, when a request fails, then the response is `application/problem+json` as built in TASK-006, and every update sends the client `version` through the versioned-update helper.
11. Given the contract `docs/specs/api/products.yaml`, when the Spectral lint from TASK-012 runs, then it still reports nothing; the contract is changed only where this ticket's decisions say so, and the change is listed in the PR.
12. Given the tests, when they run, then all pass with `[Trait("AC", "FEAT-003.<n>")]` on those proving the ACs above, the gates are met (mapping 100, pass 100, coverage 90), and `src/backend/REPO_CONTEXT.md` describes the module.

## Out of scope
- Role checks (no authentication yet; see FEAT-001).
- Real stock quantities and the Low stock flag (Inventory module).
- Blocking Inactive products on new receipts and orders (PRD-2.3 second half): that belongs to the Inbound and Outbound modules; here the product exposes `isActive` and the list filter.
- The frontend (FEAT-004).

## Notes and links
- Spec: `docs/specs/product.md` section 7 (PRD-1 to PRD-3), `docs/specs/data-model.md`, `docs/specs/api/products.yaml`. `Product.Create`, `Rename`/update and `Deactivate(totalOnHand)` already exist in `MiniWms.Domain/Products` (TASK-005).
- **Open owner decision 1 (duplicate SKU: 400 or 409).** Same question as FEAT-001: `product.md` (PRD-1.2) says a field-level message, `products.yaml` says 409. Recommended: decide once for both modules (400 under `sku`), and change `createProduct` in the contract. **Decision 2** and **3**: the stock query and the Admin role follow FEAT-001.
- Reuse from FEAT-001: the endpoint registration pattern, the `IStockQuery` abstraction, the list, sort and paging helpers, the versioned update. Sort fields: `sku`, `name`, `isActive`.
- The feature pipeline appends a `## Delivery` section to this file, files reports in `docs/reports/FEAT-003/` and appends a row to `docs/reports/ac-log.md`, so all three stay in `allowed_paths`.
