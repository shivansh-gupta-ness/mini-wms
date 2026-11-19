---
id: TASK-008
type: task
title: Application shell, navigation and lazy routes
status: done
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-008.md, docs/reports/TASK-008/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-006-angular-structure.md, docs/specs/ui.md]
parent: 
depends_on: [TASK-007]
---

## Description
Build the shell described in `docs/specs/ui.md` section 1 and a lazy-loaded empty route per module. Set `status: ready` after TASK-007 is merged.

## Acceptance criteria
1. Given the app loads, when the shell renders, then it shows the top bar, the left navigation and a content area.
2. Given the navigation order in the UI spec, when the shell renders, then items appear as Stock, Receipts, Orders, Products, Locations, and only for modules that have a route.
3. Given a module route (locations, products, inventory, inbound, outbound, auth), when it is opened, then its feature is loaded lazily and shows a placeholder title.
4. Given an unknown URL, when it is opened, then a not-found page is shown inside the shell.
5. Given keyboard-only use, when the user tabs through the shell, then every navigation item and button is reachable with a visible focus, and every control has a label (NFR-6).
6. Given the tests, when they run, then all pass and each AC-proving test name starts with `TASK-008.<n>`.

## Out of scope
- Any real module screen.
- Authentication UI.

## Notes and links
- Record AC results in `docs/reports/TASK-008/ac-results.md` and one row in `docs/reports/ac-log.md`.
- Analysis before readying (2026-11-19): TASK-007 is merged. The scaffold's root component is `App` (`src/app/app.ts`, `app.html` is still the CLI placeholder page and is replaced here); `core/`, `shared/` and `features/` exist and are empty; tests run with Vitest and jsdom through `npm test` (see `src/frontend/REPO_CONTEXT.md`). The shell goes in `core/` (layout), each module route is lazy-loaded from `features/<module>/`. The navigation order and the rule "items for modules not built yet are not shown" come from `docs/specs/ui.md` section 1; the UI spec also lists **Users** (Admin only), which has no route yet, so it is not shown. Use Material components and keep NFR-6 (labels, keyboard, visible focus) in tests. No HTTP calls in this ticket (TASK-009, TASK-011).
- Owner decisions (2026-11-19, on the scout's brief, recommended options taken): nav URLs are `/stock` (feature folder `inventory`), `/receipts` (`inbound`), `/orders` (`outbound`), `/products`, `/locations`; the auth route is `/sign-in` titled "Sign in" with no nav item; each feature has its own placeholder page in `features/<module>/pages/` so lazy loading is real; the top bar shows the app name and a fixed "Dev Admin" label, no sign-out; an explicit `:focus-visible` rule in `styles.scss` is proven by a source check, and visible focus in a real browser is a manual check noted in the results (jsdom cannot prove it); the not-found page lives in `core/layout/` and is a child of the shell route; the nav is a static list of five items. Keep the TASK-007 guard tests passing (`app.spec.ts` keeps `TASK-007.1`; `src/frontend/REPO_CONTEXT.md` stays under 80 lines with the strings the guard checks).

## Delivery
Delivered in PR 26. All 6 criteria PASS; results in `docs/reports/TASK-008/ac-results.md`, the row in `docs/reports/ac-log.md`.
