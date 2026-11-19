# FEAT-001 acceptance-criteria results (API PR)

Ticket: `tickets/FEAT-001.md` (Locations module: warehouse, zones and bins, API and screens). Branch: `feat/FEAT-001-locations-api`. Date: 2026-11-19.
This is the first of two PRs for the ticket: criteria 1 to 15 (the API) are delivered here; criteria 16 to 27 (the screens) are deferred to the second PR on `feat/FEAT-001-locations-screens` (owner decision in the ticket notes) and are recorded as NOT RUN below, not as failures.
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
| 16 to 27 | The screens (`ui.md` section 3) | NOT RUN (deferred to the screens PR, owner decision in the ticket) | none yet | Not part of this PR | 2026-11-19 |

Totals: PASS 15, FAIL 0, N/A 0, NOT RUN 12 (ACs 16 to 27, deferred)

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
