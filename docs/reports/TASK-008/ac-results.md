# TASK-008 acceptance-criteria results

Ticket: `tickets/TASK-008.md` (Application shell, navigation and lazy routes). Branch: `task/TASK-008-app-shell-navigation-routes`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (seven decisions recorded in the ticket), `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. Numbers come from my own runs in the main working tree after the hand-offs.

| Command (in `src/frontend`) | Result |
|---|---|
| `npm run build` | Succeeded, no errors or warnings; the six placeholder pages are separate lazy chunks |
| `npm test` (run twice) | Both runs: Test Files 5 passed (5), Tests 48 passed (48) (21 existing, 27 new) |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | Shell shows top bar, left navigation and content area | PASS | 3 tests (`TASK-008.1`, `shell.spec.ts`: banner with "Mini WMS" and "Dev Admin", `nav[aria-label="Main navigation"]`, `main#main-content` with the outlet) | Tests passed | 2026-11-19 |
| 2 | Nav order Stock, Receipts, Orders, Products, Locations, only for modules with a route | PASS | 4 tests (`TASK-008.2`: labels in order, hrefs, no Users or sign-in item, every target is a configured route) | Tests passed | 2026-11-19 |
| 3 | Each module route loads lazily and shows a placeholder title | PASS | 13 test cases from 3 test definitions (`TASK-008.3`, `app.routes.spec.ts`: six routes render their heading inside the shell, each uses `loadChildren`, `''` redirects to `/stock`) | Tests passed | 2026-11-19 |
| 4 | Unknown URL shows a not-found page inside the shell | PASS | 3 tests (`TASK-008.4`: unknown and nested unknown URL keep the nav and banner, "Back to Stock" link works) | Tests passed | 2026-11-19 |
| 5 | Keyboard-only use reaches every nav item and button with a visible focus; every control has a label (NFR-6) | PASS for what jsdom can prove; a real-browser check of the visible focus is NOT RUN | 8 tests (`TASK-008.5`, `a11y.spec.ts`: skip link is first focusable and targets `main`, every control has an accessible name, no interactive `tabindex="-1"`, five native anchors in order that take focus, `styles.scss` has a `:focus-visible` rule with an outline) | Tests passed. jsdom cannot compute `:focus-visible` or paint an outline, so the owner should press Tab through the shell once in a browser (`npm start`) | 2026-11-19 |
| 6 | All tests pass; AC-proving test names start with `TASK-008.<n>` | PASS | 1 test (`TASK-008.6`) scanning the new specs for the prefix | 48 of 48 tests pass, twice | 2026-11-19 |

Totals: PASS 6, FAIL 0, N/A 0, NOT RUN 0 (AC 5 has the manual browser part open, see its row)

## Gates
- Mapping: 6 of 6 ACs have dedicated tests (100%).
- Pass: 48 of 48 frontend tests pass (100%); 27 new.
- Coverage: not measured; the frontend coverage gate is TASK-010.

## Notes
- The skip link uses a click handler that focuses `main` instead of a plain `#main-content` anchor, because `<base href="/">` would make a plain anchor reload the page.
- `main` has `tabindex="-1"` so the skip link can focus it; it is not an interactive item.
- Navigation is a plain `<nav>` with a Material nav list (not `mat-sidenav`) to avoid nested landmarks.
- The top bar shows a fixed "Dev Admin" label and no sign-out (decision 4); auth UI is out of scope.
- `src/frontend/REPO_CONTEXT.md` updated (74 lines) with the layout and a routes table.
- PR size: about 26 files and 900 changed lines (545 added, 349 removed, mostly the CLI placeholder `app.html`).
