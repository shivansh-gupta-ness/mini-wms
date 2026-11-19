---
id: FEAT-002
type: feature
title: Locations screens: tree, list and zone and bin forms
status: draft
owner: shivansh-gupta-ness
component: locations
risk: low
allowed_paths: [src/frontend/src/app/features/locations/**, src/frontend/src/app/shared/**, src/frontend/src/app/core/layout/**, src/frontend/REPO_CONTEXT.md, tickets/FEAT-002.md, docs/reports/FEAT-002/**, docs/reports/ac-log.md]
docs: [docs/specs/ui.md, docs/specs/product.md, docs/specs/api/locations.yaml, docs/adr/ADR-006-angular-structure.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [FEAT-001, TASK-008, TASK-009, TASK-010]
---

## Description
Replace the Locations placeholder page with the real screens of `docs/specs/ui.md` section 3, calling the API from FEAT-001 through the `ApiClient`. Set `status: ready` after FEAT-001 is merged and the open decisions below are answered.

## Acceptance criteria
1. (LOC-1.1) Given I open `/locations`, then the page header shows the warehouse name from `GET /warehouse` with a "Rename" action, and there is no control to create or delete a warehouse.
2. (LOC-2.1, warehouse) Given I use "Rename", when I save a new name, then the header shows it; a blank or over-long name shows the message under the field; a 409 shows its `detail` in a banner at the top of the dialog.
3. (LOC-3.1, tree) Given zones and bins exist, when I open the Tree tab, then zones are parents and bins are children, each showing code, name and an Active or Inactive chip (text, not colour alone), zones also show their type; a toggle shows or hides Inactive items.
4. (LOC-3.1 and LOC-3.2, list) Given I open the List tab, then I see a bins table with the columns Code, Name, Zone and Status, sortable by header, a search box that filters by part of a code or name, a zone filter, and a paginator of 25, 50 and 100.
5. (LOC-1.2, LOC-1.4) Given "New zone", when I fill Code (required, max 20), Name (required, max 100) and Type and save, then the zone appears; a missing field or a duplicate code shows the message under that field, and nothing is saved.
6. (LOC-1.3, LOC-1.4) Given "New bin", when I choose an Active zone and fill Code (required, max 30) and Name (required, max 100), then the bin appears under the zone with its path; errors show under their fields.
7. (LOC-2.1) Given "Edit" on a zone or bin, then the Code is read-only, the Name can change, and the zone Type is disabled with a hint saying why when the zone has bins.
8. (LOC-2.2, LOC-2.3, LOC-2.4) Given "Deactivate" on a bin or zone, then a confirm dialog names the item and its default focus is Cancel; on success the item shows Inactive; on a 409 the dialog stays open and shows the message (the stock on hand, or the Active bin).
9. Given any screen, then it shows a progress bar on first load, an empty state that explains how to add the first item, a disabled button with a spinner while saving, and a generic message on a 5xx (no technical detail).
10. Given keyboard-only use, then every control is reachable with a visible focus, every control has a label, required fields are marked, and dialogs trap focus (NFR-6).
11. Given the code, then all calls go through `ApiClient`, field errors are applied with `applyServerErrors` (the unmatched messages go to the banner), and no host is hard-coded.
12. Given the tests, when they run, then all pass with test names starting `FEAT-002.<n>`, the coverage gate holds (90% of `src/app`), `npm run build` passes, and `src/frontend/REPO_CONTEXT.md` stays under 80 lines.

## Out of scope
- Role-based hiding of buttons: there is no authentication yet, so all buttons show (the top bar says "Dev Admin"); the real check arrives with the Auth module.
- A real-browser check of the screens is a manual step, marked NOT RUN unless run.
- Products screens (FEAT-004).

## Notes and links
- Spec: `docs/specs/ui.md` sections 2 and 3, `docs/specs/product.md` LOC. The page replaces `features/locations/pages/locations-placeholder.page.ts`; the lazy route `/locations` already exists. Reusable parts (confirm dialog, status chip, empty state, error banner) go in `shared/` because FEAT-004 reuses them.
- **Open owner decision 1 (dialogs or pages).** `ui.md` calls the zone and bin forms dialogs. Recommended: Material dialogs with reactive forms. **Open owner decision 2 (tree control).** Recommended: Material tree or nested lists, whichever keeps keyboard navigation simple. **Open owner decision 3 (data loading).** Recommended: signals or RxJS services in `features/locations/data/` calling `ApiClient`; no state library (ADR-006).
- Depends on the answers in FEAT-001: if a duplicate code returns 400, the message appears under the field; if it stays 409, it shows in the banner.
- Test with `HttpTestingController` and Vitest (see the existing specs); the coverage gate counts every new file.
- The feature pipeline appends a `## Delivery` section to this file, files reports in `docs/reports/FEAT-002/` and appends a row to `docs/reports/ac-log.md`, so all three stay in `allowed_paths`.
