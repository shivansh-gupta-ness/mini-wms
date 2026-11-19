# TASK-013 acceptance-criteria results

Ticket: `tickets/TASK-013.md` (Refresh the docs and context files against the code). Branch: `task/TASK-013-refresh-docs-against-code`. Date: 2026-11-20.
How it was run: `/start-ticket` flow with `ticket-scout` (it checked every audit finding against the code before the plan), owner approval with the recommended decisions (recorded in the ticket), then four `change-implementer` runs in parallel on separate files (ADRs; specs and contracts; module context files, GUIDE, roadmap and reports; ticket Delivery sections, the learning log and the placeholder files), each in an isolated worktree with the work carried back by `worktree-apply.mjs` (which now refuses protected and out-of-scope files; it accepted every file). This is a docs-only ticket: there is no new test code, so the proof is the real command output below and the checks of each criterion. The main session ran the commands.

| Command | Result |
|---|---|
| `npx @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml" --fail-severity=warn` | `No results with a severity of 'warn' or higher found!` exit 0 |
| `node docs/specs/api/tools/ac-coverage.mjs` | `Ids in product.md: 19; covered: 19`, none missing in either direction |
| `node docs/specs/api/tests/run-rule-tests.mjs` | All 6 rule tests passed |
| `node .claude/hooks/tests/guard-tests.mjs` | ALL PASSED (80 checks) |
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | Domain 63, Application 199, Api 408 passed, 0 failed (same counts as before the ticket) |
| `npm run test:coverage` (src/frontend) | Test Files 29 passed (29), Tests 383 passed (383); `Line coverage: 96.52% (694 of 719 lines); threshold 90%.` `GATE PASSED.` (same as before) |
| `npm run test:gate` | 22 of 22 pass |
| `npm run build` (src/frontend) | Succeeded, no warning |
| line counts with `split(/\r?\n/).length` | `src/backend/REPO_CONTEXT.md` 71 (under 80); `src/frontend/REPO_CONTEXT.md` 78 (limit 78); root `REPO_CONTEXT.md` 61 |

| AC # | AC text (short) | Result | Evidence | Date |
|---|---|---|---|---|
| 1 | ADRs accepted and corrected (ADR-002, 003, 005, 006, 001, 004) | PASS | All six status lines read `Accepted` (ADR-004: "Accepted; not implemented (Auth deferred, scope decision 2026-11-19)"); grep finds no `Proposed (accepted`, no `ISO 8601 text` and no `NSubstitute` in `docs/adr`; the agent checked each correction against the code (binary UTC ticks, the Development-only seeder that adds the warehouse only if none exists, duplicate code as a 400 under `code`, the six `type` values, 405, the unpaged tree exception, CORS as a fallback, Prettier only, Auth deferred). One agent edit was wrong and was fixed by me: it changed ADR-005's PR limits to "1000-line and 30-file" (it believed the root `REPO_CONTEXT.md` said so); it now reads "10000-line and 60-file" like the code | 2026-11-20 |
| 2 | Backend `REPO_CONTEXT.md` corrected, under 80 lines | PASS | Seeder wording, `FEAT-001.3` trait example, UTC ticks, project-reference and Domain-package tests wording, no scaffold wording, a health and CORS line; 71 lines; the backend structure tests that read the file pass | 2026-11-20 |
| 3 | Frontend `REPO_CONTEXT.md` at most 78 lines, bundle budget line, README one line | PASS | 78 lines before and after (two Gotchas bullets merged, the 700 kB warning and 1 MB error line added); `src/frontend/README.md` is one line; the four frontend guard specs pass inside the 383 tests | 2026-11-20 |
| 4 | `product.md`, `ui.md` and `data-model.md` corrected | PASS | Status lines `Accepted` with the Locations-only scope; "Built" column in the module table; NFR-2 carve-out for `/locations/tree`; all five nav items and buttons shown to everyone until Auth; plural table names, Warehouse code max 20, UTC ticks, CHECKs and `ON DELETE RESTRICT` (verified against the migration); the `- LOC-n.n` and `- PRD-n.n` lines are untouched (ac-coverage 19 of 19) | 2026-11-20 |
| 5 | Contracts lint clean and corrected | PASS | Spectral exit 0 with warnings as failures; `listZones`, `listBins` and `getLocationTree` have the field-level 400; the warehouse operations have a 404 and a path note that POST and DELETE return 405; Locations ids are `int32`; Products is marked design only with the duplicate-SKU response changed from 409 to 400 under `errors.sku`; the contract README says four rules and that Products is design only; ac-coverage 19 of 19 | 2026-11-20 |
| 6 | GUIDE, roadmap and reports README corrected | PASS | GUIDE no longer says `src/` is not scaffolded or that the hooks do nothing, and says the pipeline ran on TASK-001 to TASK-012, BUG-001 and FEAT-001; the roadmap says "Phase 5 is complete: FEAT-001 criteria 1 to 27 PASS (PRs 37 and 41)" and annotates the later modules as not built; the reports README describes what the folders hold | 2026-11-20 |
| 7 | Learning log rows for the audit gaps | PASS | 7 add-only rows dated 2026-11-20 (PR limit change, bundle budget, 400 versus 409 and the ADR drift, TASK-011 NOT RUN, the frontend context file cap, the removed Products ticket and the Locations-only scope, the Phase 6 reviews). The agent chose their statuses and wording; the owner should read them in the PR | 2026-11-20 |
| 8 | Owner decision, Delivery sections, PR numbers | PASS | `docs/reports/TASK-011/ac-results.md` has "Owner decision (2026-11-20): AC 1 accepted as NOT RUN"; 14 tickets have a `## Delivery` section (BUG-001, TASK-001 to TASK-012, FEAT-001), with PR numbers and counts taken from the AC log (TASK-011: "5 PASS, 1 NOT RUN (AC 1, manual check)"); the FEAT-001 report header names PRs 37 and 41 | 2026-11-20 |
| 9 | Stale placeholder files removed | PASS | `src/.gitkeep`, `docs/adr/.gitkeep`, `docs/specs/.gitkeep`, `docs/reports/.gitkeep` and the three `Common/.gitkeep` files are deleted (each folder has other content); `docs/qa/.gitkeep` and the empty-module ones stay | 2026-11-20 |
| 10 | No code changed; build, tests and gates pass as before | PASS | The command table above: same counts as before the ticket (63, 199, 408; 383 frontend tests; coverage 96.52%) | 2026-11-20 |

Totals: PASS 10, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: n/a (a docs-only ticket has no new test code; every criterion is proven by a command or a check).
- Pass: 10 of 10 criteria; all existing tests pass unchanged.
- Coverage: unchanged at 96.52% (frontend) and 98.87% (backend gate); no code was touched.

## Notes
- **Findings the scout corrected before the work:** the root `REPO_CONTEXT.md` was already right (PR 43), and the removed Products ticket was logged from PR 36.
- **Small extras the agents added** (kept, all true): a Roles-row note in `ui.md`; the `docs/qa/README.md` line that QA case files are optional; the `FEAT-001` report header names both PRs.
- **Left as they were, for a later fix:** a broken table row in `GUIDE.md` (a blank line before the `worktree-create` and `worktree-remove` rows), the roadmap's Phase 3 "Tickets that can start immediately" line, the Products contract ids (`int64`, a design-only contract) and the `AppDbContext.cs` comment (code, out of scope).
- Protected files were handled in PR 43, not here.
- The agent for the context files could not run `npm test`; the main session ran it.
- A worktree folder could not be deleted by `worktree-apply` ("Permission denied") again; the changes had applied and the folders were removed by hand.
- PR size: 43 files and about 270 changed lines.
