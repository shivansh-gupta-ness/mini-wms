---
id: FEAT-002
type: feature
title: Products module: catalogue (API and screens)
status: draft
owner: shivansh-gupta-ness
component: products
risk: low
allowed_paths: [src/backend/**/Products/**, src/backend/MiniWms.Api/Program.cs, src/backend/MiniWms.Infrastructure/DependencyInjection.cs, src/backend/MiniWms.Infrastructure/Persistence/**, src/backend/MiniWms.Application/Common/**, src/backend/REPO_CONTEXT.md, docs/specs/api/products.yaml, tickets/FEAT-002.md, docs/reports/FEAT-002/**, docs/reports/ac-log.md, src/frontend/src/app/features/products/**, src/frontend/src/app/shared/**, src/frontend/REPO_CONTEXT.md, tickets/FEAT-002.md, docs/reports/FEAT-002/**]
docs: [docs/specs/product.md, docs/specs/data-model.md, docs/specs/api/products.yaml, docs/adr/ADR-001-solution-layering.md, docs/adr/ADR-003-api-conventions.md, docs/specs/ui.md, docs/adr/ADR-006-angular-structure.md]
parent: 
depends_on: [FEAT-001, TASK-005, TASK-006, TASK-008, TASK-009, TASK-010]
---

## Description
Build the whole Products module (PRD-1 to PRD-3): the API in `docs/specs/api/products.yaml` with its use cases and tests, then the screens of `docs/specs/ui.md` section 4. It follows the pattern FEAT-001 sets (module folders, endpoint registration, the stock query, the shared screen parts). Set `status: ready` after FEAT-001 is merged, the open decisions are answered and the knowledge-map component `products` exists.

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
12. Given the tests, when they run, then all pass with `[Trait("AC", "FEAT-002.<n>")]` on those proving the ACs above, the gates are met (mapping 100, pass 100, coverage 90), and `src/backend/REPO_CONTEXT.md` describes the module.
13. (PRD-3.1) Given I open `/products`, then a table shows SKU, Name, Unit, Reorder level, On hand and Status, sortable by SKU, Name and Status, with a paginator of 25, 50 and 100, and a status filter of Active, Inactive or All (default Active).
14. (PRD-3.2) Given I type part of a SKU or name in the search box, then only matching products are shown.
15. (PRD-1.1, PRD-1.2, PRD-1.3) Given "New product", when I fill SKU (required, max 40), Name (required, max 150), Unit of measure (required, max 20) and a whole-number Reorder level of 0 or more and save, then the product appears; a missing field, a negative level or a duplicate SKU shows the message under that field and nothing is saved.
16. (PRD-2.1) Given "Edit", then the SKU is read-only and name, unit and reorder level can change; a 409 (stale version) shows its `detail` in a banner.
17. (PRD-3.3) Given I click a row, then `/products/:id` shows the fields, the total on hand and a "Stock by bin" table (empty until Inventory exists).
18. (PRD-2.2, PRD-2.3) Given "Deactivate" on the detail page, then a confirm dialog names the product and its default focus is Cancel; on a 409 it stays open and shows the message giving the total on hand; on success the page shows Inactive and a notice that the product cannot be used on new receipts or orders.
19. Given the list, then a Low stock chip appears next to On hand only when the API sends `lowStock` true (not before Inventory exists).
20. Given any screen, then it shows a progress bar on first load, an empty state, a disabled button with a spinner while saving, and a generic message on a 5xx; status words are `Active` and `Inactive` as a chip with text.
21. Given keyboard-only use, then every control is reachable with a visible focus, has a label, required fields are marked, and dialogs trap focus (NFR-6).
22. Given the code, then all calls go through `ApiClient`, field errors use `applyServerErrors`, and no host is hard-coded.
23. Given the tests, when they run, then all pass with test names starting `FEAT-002.<n>`, the coverage gate holds (90% of `src/app`), `npm run build` passes, and `src/frontend/REPO_CONTEXT.md` stays under 80 lines.

## Out of scope
- Role checks and role-based hiding of buttons (no authentication yet; see FEAT-001).
- Real stock quantities and the Low stock flag (Inventory module).
- Blocking Inactive products on new receipts and orders (PRD-2.3 second half): that belongs to the Inbound and Outbound modules; here the product exposes `isActive` and the list filter.
- A real-browser check of the screens is a manual step, marked NOT RUN unless run.

## Notes and links
- Backend criteria are 1 to 12 (the API), screen criteria are 13 to 23; tests carry `FEAT-002.<n>` with the same numbers (`[Trait("AC", "FEAT-002.<n>")]` in the backend, test names starting `FEAT-002.<n>` in the frontend).
- Spec: `docs/specs/product.md` section 7 (PRD-1 to PRD-3), `docs/specs/data-model.md`, `docs/specs/api/products.yaml`. `Product.Create`, `Rename`/update and `Deactivate(totalOnHand)` already exist in `MiniWms.Domain/Products` (TASK-005).
- **Open owner decision 1 (duplicate SKU: 400 or 409).** Same question as FEAT-001: `product.md` (PRD-1.2) says a field-level message, `products.yaml` says 409. Recommended: decide once for both modules (400 under `sku`), and change `createProduct` in the contract. **Decision 2** and **3**: the stock query and the Admin role follow FEAT-001.
- Reuse from FEAT-001: the endpoint registration pattern, the `IStockQuery` abstraction, the list, sort and paging helpers, the versioned update. Sort fields: `sku`, `name`, `isActive`.
- Spec: `docs/specs/ui.md` sections 2 and 4, `docs/specs/product.md` PRD. The page replaces `features/products/pages/products-placeholder.page.ts`; the lazy route `/products` exists, add `/products/:id` as a child route.
- **Open owner decision (form as page or dialog).** `ui.md` allows either. Recommended: a dialog for create and edit (same pattern as Locations), the detail as a page.
- Reuse the shared components built for Locations in FEAT-001; do not copy them.
- **Delivery in two PRs, one ticket.** The ticket spans both layers of this one repository (the layer rule in `tickets/README.md` is about separate repositories). Run the backend first (`/run-feature-be FEAT-002`, branch `feat/FEAT-002-products-api`), merge it, then the screens (`/run-feature-fe FEAT-002 B`, branch `feat/FEAT-002-products-screens`). Each PR must stay within the limits (10000 lines, 60 files) and passes `pr-guard` on its own. The `ac-results.md` file is written in the first PR and completed in the second; add the `ac-log.md` row with the second PR. The scope guard allows both layers' paths for the whole ticket, so agents must still work on one layer per run.
- The feature pipeline appends a `## Delivery` section to this file, files reports in `docs/reports/FEAT-002/` and appends a row to `docs/reports/ac-log.md`, so all three stay in `allowed_paths`.
- Decisions inherited from FEAT-001 (2026-11-19, recommended options): a duplicate SKU returns 400 with the message under `sku` and `createProduct` in `docs/specs/api/products.yaml` changes from 409 to 400 for this case (the PR lists the contract change); the stock query is the `IStockQuery` that FEAT-001 adds; no role checks until the Auth module; the product form is a dialog and the detail is a page. This ticket stays `draft` until FEAT-001 is merged; then analyse it against what FEAT-001 actually built and set it to `ready`.
