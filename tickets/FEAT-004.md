---
id: FEAT-004
type: feature
title: Products screens: list, form and detail
status: draft
owner: shivansh-gupta-ness
component: products
risk: low
allowed_paths: [src/frontend/src/app/features/products/**, src/frontend/src/app/shared/**, src/frontend/REPO_CONTEXT.md, tickets/FEAT-004.md, docs/reports/FEAT-004/**, docs/reports/ac-log.md]
docs: [docs/specs/ui.md, docs/specs/product.md, docs/specs/api/products.yaml, docs/adr/ADR-006-angular-structure.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [FEAT-003, FEAT-002]
---

## Description
Replace the Products placeholder with the screens of `docs/specs/ui.md` section 4, calling the API from FEAT-003 through the `ApiClient` and reusing the shared parts FEAT-002 creates (confirm dialog, status chip, empty state, error banner). Set `status: ready` after FEAT-002 and FEAT-003 are merged.

## Acceptance criteria
1. (PRD-3.1) Given I open `/products`, then a table shows SKU, Name, Unit, Reorder level, On hand and Status, sortable by SKU, Name and Status, with a paginator of 25, 50 and 100, and a status filter of Active, Inactive or All (default Active).
2. (PRD-3.2) Given I type part of a SKU or name in the search box, then only matching products are shown.
3. (PRD-1.1, PRD-1.2, PRD-1.3) Given "New product", when I fill SKU (required, max 40), Name (required, max 150), Unit of measure (required, max 20) and a whole-number Reorder level of 0 or more and save, then the product appears; a missing field, a negative level or a duplicate SKU shows the message under that field and nothing is saved.
4. (PRD-2.1) Given "Edit", then the SKU is read-only and name, unit and reorder level can change; a 409 (stale version) shows its `detail` in a banner.
5. (PRD-3.3) Given I click a row, then `/products/:id` shows the fields, the total on hand and a "Stock by bin" table (empty until Inventory exists).
6. (PRD-2.2, PRD-2.3) Given "Deactivate" on the detail page, then a confirm dialog names the product and its default focus is Cancel; on a 409 it stays open and shows the message giving the total on hand; on success the page shows Inactive and a notice that the product cannot be used on new receipts or orders.
7. Given the list, then a Low stock chip appears next to On hand only when the API sends `lowStock` true (not before Inventory exists).
8. Given any screen, then it shows a progress bar on first load, an empty state, a disabled button with a spinner while saving, and a generic message on a 5xx; status words are `Active` and `Inactive` as a chip with text.
9. Given keyboard-only use, then every control is reachable with a visible focus, has a label, required fields are marked, and dialogs trap focus (NFR-6).
10. Given the code, then all calls go through `ApiClient`, field errors use `applyServerErrors`, and no host is hard-coded.
11. Given the tests, when they run, then all pass with test names starting `FEAT-004.<n>`, the coverage gate holds (90% of `src/app`), `npm run build` passes, and `src/frontend/REPO_CONTEXT.md` stays under 80 lines.

## Out of scope
- Role-based hiding of buttons (no authentication yet).
- Real stock figures (Inventory module).
- A real-browser check is a manual step, marked NOT RUN unless run.

## Notes and links
- Spec: `docs/specs/ui.md` sections 2 and 4, `docs/specs/product.md` PRD. The page replaces `features/products/pages/products-placeholder.page.ts`; the lazy route `/products` exists, add `/products/:id` as a child route.
- **Open owner decision (form as page or dialog).** `ui.md` allows either. Recommended: a dialog for create and edit (same pattern as Locations), the detail as a page.
- Reuse the shared components from FEAT-002; do not copy them.
- The feature pipeline appends a `## Delivery` section to this file, files reports in `docs/reports/FEAT-004/` and appends a row to `docs/reports/ac-log.md`, so all three stay in `allowed_paths`.
