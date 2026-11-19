# FEAT-001 acceptance-criteria results (API and screens)

Ticket: `tickets/FEAT-001.md` (Locations module: warehouse, zones and bins, API and screens). Branch: `feat/FEAT-001-locations-api`. Date: 2026-11-19.
The ticket was delivered in two PRs: the API (criteria 1 to 15, PR 37, branch `feat/FEAT-001-locations-api`) and the screens (criteria 16 to 27, branch `feat/FEAT-001-locations-screens`, this second PR). Both halves are recorded below.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (decisions recorded in the ticket), `change-implementer` in two stages (contract and Application layer, then Infrastructure and Api), then two `test-writer` runs in parallel (unit tests, API tests on SQLite), then a third `change-implementer` run to fix a production defect the API tests found, each in an isolated worktree with work carried back by `worktree-apply.mjs`. The pipeline skill `/run-feature-be` was not used (owner decision). Numbers come from my own runs in the main working tree.

| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` (run twice) | Both runs: Domain.Tests 63, Application.Tests 199, Api.Tests 408 passed, 0 failed (670 tests; 328 new: 119 Application, 209 Api; Domain tests only gained traits) |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | Domain 100%, Application 97.6%, Api 100%; `Combined line coverage: 98.87% (610 of 617 lines); threshold 90%` `GATE PASSED.` |
| `npx @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml" --fail-severity=warn` | `No results with a severity of 'warn' or higher found!` exit 0 |
| `node docs/specs/api/tools/ac-coverage.mjs` | 19 of 19 LOC and PRD criteria covered, none missing in either direction |
| `src/backend/MiniWms.Api/App_Data` after all runs | does not exist |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | `GET /warehouse` returns the single warehouse; no route creates or deletes one | PASS | 8 Api tests (`WarehouseApiTests`: fields, POST and DELETE give 405 with `Allow: GET, PATCH`) | Tests passed | 2026-11-19 |
| 2 | `PATCH /warehouse` renames; blank or over-100 name gives 400 under `name`; stale version gives 409 | PASS | 9 Application + 14 Api tests | Tests passed | 2026-11-19 |
| 3 | `POST /zones` gives 201 with a `Location` header, saved Active | PASS | 4 Application + 6 Api tests | Tests passed | 2026-11-19 |
| 4 | Zone with missing or invalid fields or a duplicate code (any case) is rejected field-level, nothing saved | PASS | 17 Application + 21 Api tests (includes a duplicate-code race: one 201, the rest 400 under `code`) | Tests passed | 2026-11-19 |
| 5 | `POST /bins` in an Active zone gives 201 with `path` and `zoneCode` | PASS | 2 Application + 3 Api tests | Tests passed (after the bin query fix) | 2026-11-19 |
| 6 | Bin with missing fields, an unknown or Inactive zone, or a duplicate code is rejected field-level, nothing saved | PASS | 13 Application + 14 Api tests | Tests passed | 2026-11-19 |
| 7 | Zone and bin rename keeps the code; zone type changes only with no bins (else 409); stale version 409 | PASS | 3 Domain + 14 Application + 17 Api tests | Tests passed | 2026-11-19 |
| 8 | A bin with no stock is deactivated, still readable, excluded by `isActive=true` | PASS | 4 Application + 10 Api tests | Tests passed | 2026-11-19 |
| 9 | A bin holding stock is refused with 409 naming the quantity | PASS | 2 Domain + 2 Application + 3 Api tests (stock comes from a fake `IStockQuery`; the real one returns 0 until Inventory exists) | Tests passed | 2026-11-19 |
| 10 | A zone with an Active bin is refused with 409; otherwise deactivated | PASS | 2 Domain + 5 Application + 9 Api tests | Tests passed | 2026-11-19 |
| 11 | Zones and bins list: paged (default 25, max 100), sortable by the allowed fields, filterable; the tree returns zones with bins | PASS | 30 Application + 62 Api tests | Tests passed | 2026-11-19 |
| 12 | `q` matches part of a code or name without regard to case | PASS | 7 Application + 28 Api tests (`%` and `_` are literal; ASCII case folding only) | Tests passed | 2026-11-19 |
| 13 | Failures are `application/problem+json`; every update sends the client `version` through the versioned-update helper | PASS | 2 Application + 14 Api tests | Tests passed | 2026-11-19 |
| 14 | The Spectral lint still reports nothing; contract changes are listed | PASS (by a real run, no unit test) | none by design | Lint above, exit 0; `ac-coverage.mjs` 19 of 19. Contract changes: `createZone` and `createBin` lose 409 (a duplicate code is 400 under `code`, a bad zone is 400 under `zoneId`); `deactivateZone` and `deactivateBin` gain a 400; the sort fields, `q` limit, `binCount` and tree behaviour are described | 2026-11-19 |
| 15 | All tests pass with `FEAT-001.<n>` traits; gates met; `REPO_CONTEXT.md` describes the module and the stock query | PASS (by gate output, no unit test of its own) | none by design | 670 of 670 tests pass; coverage 98.87% against 90%; `src/backend/REPO_CONTEXT.md` is 69 lines with a Locations section and the stock query | 2026-11-19 |
| 16 | The Locations page shows the warehouse name with "Rename", and no control to create or delete a warehouse | PASS | 5 tests (`locations.page.spec.ts`: `GET /api/v1/warehouse`, the exact `Locations` heading, no create or delete warehouse control) | Tests passed | 2026-11-19 |
| 17 | Rename saves and shows the new name; a bad name shows under the field; a 409 shows its `detail` in a banner | PASS | 11 tests (`warehouse-rename-dialog.spec.ts` and the page dialog spec: blank and 101-character names, the `{name, version}` body, 400 under `name`, 409 banner with Reload that re-fetches and updates the version) | Tests passed | 2026-11-19 |
| 18 | Tree tab: zones with bins, code, name and an Active or Inactive chip as text, zone type, a show-inactive toggle | PASS | 25 tests (`location-tree.spec.ts` and `status-chip.spec.ts`: hierarchy, chip text and accessible name, the independent toggle, disclosure buttons with `aria-expanded`) | Tests passed (the chip had a real defect, fixed: the `aria-label` bound as an attribute never reached the DOM) | 2026-11-19 |
| 19 | List tab: bins table, sort by header, search, zone filter, paginator of 25, 50 and 100 | PASS | 22 tests (`bin-list.spec.ts` and the page spec: `sort=code` and `-code`, one request with `q` after 300 ms, `zoneId`, the 1-based page, page reset on filter changes) | Tests passed | 2026-11-19 |
| 20 | New zone: required fields, max lengths, a missing field or a duplicate code shows under that field, nothing saved | PASS | 11 tests (`zone-form-dialog.spec.ts`: POST `{code, name, type}`, 400 under `code` keeps the dialog open with no second request) | Tests passed | 2026-11-19 |
| 21 | New bin: Active zones only, errors under their fields | PASS | 10 tests (`bin-form-dialog.spec.ts`: the zone select, POST `{zoneId, code, name}`, 400 under `zoneId` and `code`, 31-character code blocked) | Tests passed | 2026-11-19 |
| 22 | Edit: the code is read-only, the name changes, a zone type is disabled with a hint when it has bins | PASS | 11 tests (zone and bin dialogs: PATCH `{name, version}` without `code`, disabled type with the hint, disabled zone select showing an Inactive current zone) | Tests passed | 2026-11-19 |
| 23 | Deactivate: a confirm dialog names the item, Cancel has the focus, a 409 keeps it open with the message | PASS | 21 tests (the shared `confirm-dialog.spec.ts` and the page dialog spec: `Deactivate zone A1?`, Cancel focused, POST `{version}`, 409 detail shown, success reloads) | Tests passed | 2026-11-19 |
| 24 | Progress bar on first load, empty states, a disabled button with a spinner while saving, a generic message on a 5xx | PASS | 31 tests (page, list, dialogs and shared parts: progress bar until all responses, "No zones yet" and "No bins yet", spinner and disabled submit, a 500 shows no detail or trace id) | Tests passed | 2026-11-19 |
| 25 | Keyboard use: reachable controls, visible focus, labels, required markers, dialogs trap focus (NFR-6) | PASS for what jsdom can prove; the real-browser check of the visible focus is NOT RUN | 30 tests (accessible names, native buttons, no `tabindex="-1"` on interactive items, dialog `aria-labelledby`, focus on the first control, no `outline: none` in the source scan) | Tests passed; press Tab through the Locations page once in a browser to confirm the outline | 2026-11-19 |
| 26 | All calls go through `ApiClient`, field errors use `applyServerErrors`, no host is hard-coded | PASS | 36 tests (every `LocationsApi` method through `HttpTestingController`; source scans: no `HttpClient`, no host, the dialogs call `applyServerErrors(`, only `locations.api.ts` uses `ApiClient`) | Tests passed | 2026-11-19 |
| 27 | Tests pass with `FEAT-001.<n>` names, the coverage gate holds, `npm run build` passes, `REPO_CONTEXT.md` under 80 lines | PASS | 4 tests (`locations.guards.spec.ts`: `REPO_CONTEXT.md` at most 78 lines and names the module, every title starts with `FEAT-001.`, ids 16 to 26 all have tests) | 383 of 383 frontend tests pass, gate 96.52% against 90%, build succeeds with no warning, `REPO_CONTEXT.md` is 77 lines | 2026-11-19 |

Totals: PASS 27, FAIL 0, N/A 0, NOT RUN 0. Parts that are not run are noted on their rows: the real-browser checks of the screens (AC 25 visible focus, and the screens against the running API), and, for the API half, the real stock quantities (Inventory does not exist).

## Gates (over ACs 1 to 15)
- Mapping: ACs 1 to 13 have dedicated tests (13 of 15, 86.7%); AC 14 and 15 are proven by evidence.
- Pass: 670 of 670 tests pass (100%).
- Coverage: 98.87% over Domain, Application and Api (gate 90%); Infrastructure is not part of the gate.

## Notes
- **The API tests found a real defect.** `LocationQueries` projected joined bins into a `BinDto` constructor and then filtered, sorted and looked up on the DTO, which EF Core cannot translate; every bin read (and every bin write, which reads the bin back) returned 500, with 68 API tests failing. A third implementer run fixed it by filtering and sorting on a translatable bin and zone pair and building the DTO after the read. The implementer's own smoke test of the endpoints had been blocked by the harness, so the first real exercise of the queries was the test-writer's.
- **Role checks** ("Given I am an Admin"): no authentication exists yet, so the role part of each Admin AC is N/A until the Auth module; the endpoints stamp the development user.
- **Stock**: `IStockQuery` returns 0 until Inventory exists (`ZeroStockQuery`, registered with `TryAddSingleton` so a later module can replace it); the "holds stock" paths are proven with a fake, and a manual check of that path is NOT RUN.
- **Case-insensitive uniqueness** uses the NOCASE columns, so it folds ASCII only (stated in the contract).
- Lists answer `page`, `pageSize`, `zoneId` and `isActive` errors under their own field names (parsed in the endpoints), and a missing or bad JSON body returns the standard 400 problem.
- `worktree-apply` could not delete three agent worktree folders ("Permission denied", probably a process still holding a file); the changes had applied and the folders were removed by hand.
- PR size: about 41 files and 4,000 changed lines (the limits are 60 files and 10,000 lines), so the screens go in a second PR.

## Screens half: commands and numbers (from `src/frontend`)
| Command | Result |
|---|---|
| `npm run build` | Succeeded, no warnings (initial bundle 566.56 kB; the warning budget was raised to 700 kB in PR 40) |
| `npm run test:coverage` (run twice) | Both runs: Test Files 29 passed (29), Tests 383 passed (383) (169 existing from TASK-007 to TASK-011 plus 214 new for FEAT-001); `Line coverage: 96.52% (694 of 719 lines); threshold 90%.` `GATE PASSED.` |
| `npm run test:gate` | 22 of 22 gate tests pass |
| `npm test -- --filter "\\bFEAT-001\\.<n>\\b"` for n = 16 to 27 | 5, 11, 25, 22, 11, 10, 11, 21, 31, 30, 36 and 4 tests, all passing |

## Notes on the screens half
- The run used four implementer stages (shared parts and the data layer; the page, tree and list; the dialogs and the deactivate wiring; the guard specs), each followed or accompanied by a test-writer, with stages and tests running in parallel on separate files. The pipeline skill `/run-feature-fe` was not used (owner decision).
- **A real defect found by the tests:** the status chip's `aria-label` bound with `[attr.aria-label]` never reached the DOM because Material's chip overrides it; fixed with the chip's own `aria-label` input.
- A tab test was flaky because the List tab's content is attached lazily; it now waits until the list appears.
- A stale-version 409 shows the backend text with a "Reload" action that re-fetches the item and its version; the same button also shows on a type-change 409 in edit mode (harmless).
- The Tree and List show everything by default and mark Inactive items with a chip; the bin's zone select is disabled in edit mode because the contract cannot move a bin.
- The role-based hiding of buttons is N/A until the Auth module exists (the top bar says "Dev Admin"); the real-browser check is NOT RUN.
- Three agent worktrees held a link to the main `node_modules` (the agents had no network for `npm ci`); each link was removed with `rmdir` before the cleanup so the real folder was never touched.
