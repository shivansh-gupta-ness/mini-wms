# TASK-019 review findings

Ticket: `tickets/TASK-019.md`. Range: the uncommitted working tree on `task/TASK-019-default-bins-order-id-once` against `main` (`7aaac1e`). Date: 2026-11-27. Run: first run (`/start-ticket` after the build).
Each agent's report is below unchanged, in chain order, followed by the summary table and the stop notices.

## 1. own-code-review
Plan: supplied (approved 2026-11-27, also saved at `docs/reports/TASK-019/ticket_plan.md`). The change is uncommitted on `task/TASK-019-default-bins-order-id-once`, with no commits on top of main. Files changed outside `prompt_logs/`:
- M `src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs`
- M `docs/reports/ac-log.md`
- ?? `docs/reports/TASK-019/` (`ac-results.md`, `ticket_plan.md`; read both)

**Plan and AC table**

| item | implemented in | status |
|---|---|---|
| Plan 1a: `.ThenBy(p => p.Bin.Id)` on each named arm (`code`, `name`, `zone`, `isactive`) | LocationQueries.cs:98, :99, :100, :101 | done |
| Plan 1b: `_` arm stays `bins.OrderBy(p => p.Bin.Id)` alone | LocationQueries.cs:102 | done |
| Plan 1c: shared `.ThenBy` after the `switch` removed | LocationQueries.cs:105-107 (removed line shown in the diff) | done |
| Plan 1d: ordering stays in the IQueryable, so it stays in the SQL. No other code change, no migration, contract or Application change | LocationQueries.cs:96-107. `sorted` is an `IOrderedQueryable<BinPair>` until `ToPagedResultAsync`, and no other `src/` file changed | done |
| Plan 2: no new test, no change to expected values, no new traits | `LocationListApiTests.cs` is unchanged. All five named tests exist (lines 65, 158, 202, 227, 243) | done |
| Plan 3: `dotnet test` plus the coverage gate | The test run passes (below). The coverage gate result is only what the report says (not run by this agent) | done (gate not re-run here) |
| Plan 4: `ac-results.md` and one `ac-log.md` row | docs/reports/TASK-019/ac-results.md, docs/reports/ac-log.md:28 | done |
| Plan 5: review chain | in progress (this review) | pending |
| AC 1: no sort gives ascending `Id`, page by page, as today | `_` arm at :102. The run passed `Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest` and `Without_a_sort_the_lists_come_back_in_id_order` unchanged | pass |
| AC 2: every allowed sort, both directions, keeps the order and the `Id` tie-break | :98-101. The run passed the sort, name-order and page-boundary tests unchanged | pass (see minor finding 1) |
| AC 3: `Bin.Id` once in the default order, tie-break after every real sort key, sort in SQL, no migration or contract change | Read from the diff at :96-107. No migration and no `docs/specs` change in the diff | pass (by reading the code. The SQL text was not captured, which the plan accepts) |
| AC 4: suite and 90% gate pass, report and log row record real output | The `dotnet test` run passes. The report records 97.05%, GATE PASSED | pass (gate not re-run by this agent) |

Nothing was planned and left out, and nothing was added that the plan did not include.

**Scope check**

Pass. All three changed paths are in `allowed_paths`: `LocationQueries.cs`, `docs/reports/TASK-019/**` and `docs/reports/ac-log.md`. No protected path changed apart from `prompt_logs/`, which the logging hooks update automatically and which this review leaves out by design.

**Findings**

| severity | file:line | evidence | suggested fix |
|---|---|---|---|
| minor | src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs:98-101 | The per-arm `ThenBy` may not be proven by any test. ac-results.md:29 says the tie-break tests might still pass if a `ThenBy` were dropped, because SQLite often returns tied rows in rowid order and that matches `Id` order here. Nobody has tested this. It does not block this ticket, which only needs the order unchanged. | No change needed in this ticket. Flag it to `test-critic`. The `ToQueryString()` helper waits on an owner decision. |
| minor | src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs:99 | The `name` arm line is now about 200 characters. The old line was already long, and the backend has no `max_line_length` rule (only `src/frontend/.editorconfig:16` sets one, to `off`). | Optional: wrap the ternary over several lines, as `ListZonesAsync` does at :51-53. |

Leftover check: no debug output, no TODOs, no commented-out code and no new usings (`EF.Functions` is still used). The only untracked files are the two reports. No secrets.

Hand-off checklist:
- `src/backend/REPO_CONTEXT.md`: no update needed. Line 62 already describes the `Id` tie-breaker in general terms, and behaviour has not changed.
- No context file changed, so the line limit and split rules still hold.
- `ac-results.md` is ready. The `ac-log.md` row is ready with "(PR pending)", and Mapping 0% is explained at ac-results.md:23.
- No specs changed, so `ac-coverage` was not needed.

**Commands run**

| command | result |
|---|---|
| `git diff --merge-base main -- . ":!prompt_logs"` | 2 files changed: LocationQueries.cs (4 arms changed, 1 line removed) and ac-log.md (+1 row) |
| `git status --short -- . ":!prompt_logs"` | 2 modified files, plus untracked `docs/reports/TASK-019/` |
| `git log --oneline main..HEAD` | empty (all work is uncommitted) |
| `dotnet build src/backend` | Build succeeded, 0 warnings, 0 errors |
| `dotnet test src/backend --no-build` | Domain 63/63, Application 235/235, Api 470/470 passed, 0 failed (768 total) |
| `src/backend/coverage.ps1` | not run: not among this agent's allowed commands. ac-results.md:11 records `97.05% ... GATE PASSED` |

**Verdict**

Pass. The diff does what the plan said, exactly, and nothing else.

Punch list for the implementer (none of these block the PR):
1. Optional: wrap the long `name` arm at LocationQueries.cs:99.
2. Before the PR, replace "(PR pending)" at docs/reports/ac-log.md:28 with the PR number.
3. Leave the open question about the tie-break tests (ac-results.md:29) to `test-critic` and the owner.

## 2. independent-code-review
- **Summary**: In `ListBinsAsync`, the change moves the `Bin.Id` tie-break into each named sort arm (`code`, `name`, `zone`, `isactive`) and removes the shared `.ThenBy(p => p.Bin.Id)` that ran after the `switch`. So the default (`_`) arm now orders by `Bin.Id` once instead of twice. Nothing else changed: no tests, contract, migration or docs. Files: `src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs` (+5/-5), plus report files under `docs/reports/`.

- **Findings**: No blocker, major or minor findings.

- **Checked and clean**:
  - Ticket AC (TASK-019, risk low, component `locations`):
    - AC1: the default arm is `_ => bins.OrderBy(p => p.Bin.Id)` at LocationQueries.cs:102, and nothing adds to it afterwards (lines 105-107).
    - AC2: every named arm keeps its key, direction and the Id tie-break, for example `"zone" => (descending ? ... : bins.OrderBy(p => p.Zone.Code)).ThenBy(p => p.Bin.Id)` at :100. Lines :98, :99 and :101 follow the same pattern.
    - AC3: the default order puts `Bin.Id` in the ORDER BY once; every allowed sort is still followed by the `Bin.Id` tie-break; the whole chain stays on `IQueryable` (no `AsEnumerable` or in-memory sort); no migration or contract file is in the diff.
    - AC4 (tests): `dotnet test src/backend` passed: Domain 63/63, Application 235/235, Api 470/470.
  - Existing tests that cover the behaviour are unchanged and pass: `Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest` (LocationListApiTests.cs:65), `Bins_sort_by_every_allowed_field_with_the_id_as_tie_break` (:158, all 8 sort directions), `Bins_sort_by_name_without_regard_to_case_and_equal_names_keep_the_id_order` (:202) and `Bins_name_order_holds_across_page_boundaries` (:227).
  - Scope: the changed and untracked files are `LocationQueries.cs`, `docs/reports/ac-log.md` and `docs/reports/TASK-019/{ac-results.md,ticket_plan.md}`. All of them are in `allowed_paths`. No protected path is touched, and the size is far below 10000 lines and 60 files.
  - Architecture (ADR-001): this is an Infrastructure-only change with no new references. `EF.Functions` was already used there.
  - API and security (ADR-003/004): no change to endpoints, validation, error shape, status codes, roles or logging. The sort allow-list is untouched; unknown fields still fall to the `_` arm, as before.
  - Correctness: `sort?.Field.ToLowerInvariant()` is null-safe when `sort` is null and lands in the `_` arm. The async and cancellation path is unchanged. This is a read-only query, so the `Version` rule does not apply.
  - Docs: `src/backend/REPO_CONTEXT.md:62` already describes the "`Id` tie-breaker" in general terms and is still accurate, so no context file needed an update.

- **Not verified**:
  - The SQL text itself (whether it really reads `ORDER BY "b"."Id"` once). There is no SQL-capture test, and the ticket puts that helper out of scope. Judged from reading the LINQ only.
  - The 90% coverage gate (`coverage.ps1`), because that script is not in this agent's allowed shell commands. Since this change removes code rather than adding branches, it is unlikely to lower coverage, but it was not measured.
  - The content of `docs/reports/ac-log.md` and `docs/reports/TASK-019/*`, which the independence rules forbid reading, so whether they "record the real output" (AC4) was not checked.
  - Commit messages and the ticket from `## Notes and links` onward were not read, and no plan was given.

- **Verdict**: approve.

## 3. security-reviewer
**Attack surface**: Only the ORDER BY of `GET /api/v1/bins` changes (src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs:96-107). No new endpoint, parameter, header, body field, UI or harness change. The `sort` input reaches the switch as before. The change only moves the `Bin.Id` tie-break: it now sits inside each named sort arm, and the default arm `_ => bins.OrderBy(p => p.Bin.Id)` no longer gets a second `Id` key.

**Findings**: none

**Pre-existing**: none found near the change. The `"NOCASE"` value passed to `EF.Functions.Collate` (line 99) is a constant, not input. The `q` filter (lines 88-93) goes through `LikePattern` with `LikeEscape`, and the diff does not touch it.

**Checked and clean**:
- Step 1: the ticket was read up to `## Notes and links`. The diff is one file, and that file is inside `allowed_paths`. No untracked files outside the excluded paths.
- Step 2: no endpoint, input or error shape changed. The sort field is still matched against fixed lowercase literals. An unknown value falls to the `Id` order, so input never becomes a column name.
- Step 3: no raw SQL, no string-built SQL, no new `Like`/`Collate` fed with input. The ordering stays in LINQ and is translated to SQL.
- Step 4: no auth change. Locations endpoints stay open until the Auth ticket, as planned.
- Step 5: paging still goes through `ToPagedResultAsync(paging, ...)` (line 107), so the page-size limits are unchanged. Every arm still produces a total order (a unique `Id` key is last), so paging stays stable and no row is skipped or repeated between pages. No writes, no `Version` or mass-assignment code touched.
- Steps 6 to 9: no headers, CORS, frontend, secrets, packages or harness in the diff.
- Step 10: no new security behaviour, so no security test is needed.

**Not verified**:
- Dependency advisories: no package changes, and advisories cannot be checked offline anyway.
- `dotnet build` and `dotnet test` were not run. The change only reorders sort keys, which matters for correctness rather than security, and those tests belong to other reviewers.
- Where `SortSpec<BinDto>` validates the allow-list was not traced. The diff did not change it, and the switch only accepts fixed literals either way.

The `docs/reports/TASK-019/` files and the edited `docs/reports/ac-log.md` were ignored, as the independence rules require.

**Verdict**: approve. Nothing to fix: the change only reorders sort keys and adds no attack surface.

## 4. test-critic
Change checked with `git diff --merge-base main -- . ":!prompt_logs"` and `git status --short`:
- The only code change is `src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs:98-105`. Each named arm (`code`, `name`, `zone`, `isactive`) now ends in its own `.ThenBy(p => p.Bin.Id)`. The `_` arm is `OrderBy(p => p.Bin.Id)` alone (line 102). The shared `.ThenBy` is removed.
- Other changes: `docs/reports/ac-log.md` (one row) and the untracked `docs/reports/TASK-019/ac-results.md` and `ticket_plan.md`, which were read.
- No test file changed, and no test carries a `TASK-019.*` trait (grep of `src/` for `TASK-019`: no matches). That follows the owner's decision recorded in `docs/reports/TASK-019/ticket_plan.md:26`.
- The ordering stays in SQL: `ToPagedResultAsync` does `CountAsync` and `ApplyPage(...).ToListAsync` on the `IQueryable` (`src/backend/MiniWms.Infrastructure/Persistence/PagedQueryableExtensions.cs:13-14`).

Command run: `dotnet test src/backend/MiniWms.Api.Tests --filter` on `LocationListApiTests` (`Bins_default_to_page_1`, `Without_a_sort_the_lists`, `Bins_sort_by*`, `Bins_name_order_holds`, `Bins_filter`, `A_blank_q`, `Bins_match_q`). Result: Passed 21, Failed 0. This shows only that unmodified code passes; it confirms no mutation result. The suite totals, the 13-test run and the coverage percentage in `ac-results.md` are not verified (the full suite and `coverage.ps1` were not run).

### AC to test table
| AC | tests | verdict | note |
|---|---|---|---|
| 1 Default order is ascending Id, page by page | `LocationListApiTests.cs:243` `Without_a_sort_the_lists_come_back_in_id_order`, `LocationListApiTests.cs:65` `Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest` | strong | The 243 test's Id order (B3,B1,B2,B4) differs from both code and name order, so a wrong default key fails it. The 65 test alone would not catch a default that orders by `Code`, because B01..B26 are created in code order. |
| 2 Every allowed sort, both directions, keeps order and Id tie-break | `LocationListApiTests.cs:158` (8 cases), `:202` (2 cases), `:227` | weak | The tests prove the order and the zone and isActive tie-break against a `Code` tie-break. They cannot tell an Id tie-break from a Code tie-break on the `name` arm (F1, M1). They cannot tell any arm's tie-break from SQLite's natural row order (F2, M2). The `code` arm has no ties because `Bin.Code` is unique (`BinConfiguration.cs:20`). |
| 3 Default has `Bin.Id` once, each sort keeps the tie-break, order in SQL, no migration or contract change | none (the ticket defines it as a code-reading check) | strong (by reading, not by test) | Checked: `LocationQueries.cs:98-102`, `PagedQueryableExtensions.cs:13-14`, and the diff touches no migration or `docs/specs` file. No test guards it against regression (M3). |
| 4 Suite and 90% gate pass; reports written | none (process criterion) | strong for the files, numbers not verified | The report and log row exist in the diff. Their test counts and coverage numbers are not verified (see above). |

### Findings
F1. minor | `src/backend/MiniWms.Api.Tests/Locations/LocationListApiTests.cs:176-185` (used at `:199-200`, `:236-237`) | The only tied-name pair is B1 "apple" (id 1) and B3 "Apple" (id 3). Its code order equals its Id order, so the `name` arm's tie-break at `LocationQueries.cs:99` could be `Bin.Code` and every name test still passes. In the default seed (`:30-39`) all names differ, so no ties occur. This ticket moved the tie-break into each arm, which makes a per-arm slip the main risk of the change. | Add a bins name-sort test with a tied pair whose code order is the opposite of its Id order.

F2. minor | `LocationQueries.cs:99-101` vs `LocationListApiTests.cs:30-39`, `:176-185` | Every seed creates rows in Id order, and SQLite's rowid is the Id. If an arm's `.ThenBy(p => p.Bin.Id)` is dropped, tied rows likely still come back in Id order through table or FK-index scan order. `ac-results.md:29` makes the same point and says it was not checked. The only robust guard is checking the SQL text. | A `ToQueryString()` assertion (out of scope, see "Not worth a test").

F3. minor | `docs/adr/ADR-005-testing-coverage-and-ac-log.md:23` vs `docs/reports/TASK-019/ac-results.md:15-20,23` | ADR-005 says an AC with no tagged test is reported as NOT RUN. `ac-results.md` reports all 4 ACs as PASS while its own mapping line says no criterion has a `TASK-019` tagged test. A written owner decision (`ticket_plan.md:26`) covers the exception under ADR-005:25, but the result wording does not follow ADR-005:23. | No test needed: either word the results as "PASS by owner decision (no tagged test)", or add `TASK-019.1` and `TASK-019.2` traits to the five tests.

### Surviving mutations
M1. reasoned | `LocationQueries.cs:99`: change `.ThenBy(p => p.Bin.Id)` to `.ThenBy(p => p.Bin.Code)` | Closest tests: `LocationListApiTests.cs:202` and `:227` | The tied pair B1/B3 sorts B1,B3 under both Id and Code, ascending and descending (`:199-200`). The default seed used by `:158` has no tied names. (Checked: the same change on the `zone` arm (`:100`) is caught by `:153`, which expects B3,B2 in zone M, and on the `isactive` arm (`:101`) by `:155`.)

M2. reasoned | `LocationQueries.cs:99`, `:100` or `:101`: remove `.ThenBy(p => p.Bin.Id)` from that arm | Closest tests: `LocationListApiTests.cs:158` (`zone`, `-zone`, `isActive`, `-isActive` cases) and `:202` | Tied rows fall back to SQLite's output order. With Id = rowid = insertion order, that is very likely Id order. Not run (this agent never changes code).

M3. reasoned | `LocationQueries.cs:102`: `_ => bins.OrderBy(p => p.Bin.Id).ThenBy(p => p.Bin.Id)`, which puts back the duplicate key this ticket removes | Closest test: `LocationListApiTests.cs:243` | The rows come back identical. Grep of `src/backend` `*.cs` for `ToQueryString|ORDER BY|LogTo\(`: no files found, so no test reads the SQL.

M4. reasoned | `LocationQueries.cs:98`: remove or change the `code` arm's `.ThenBy(p => p.Bin.Id)` | Closest test: `LocationListApiTests.cs:149-150` | This is an equivalent mutant: `Bin.Code` has a unique index (`src/backend/MiniWms.Infrastructure/Persistence/Configurations/BinConfiguration.cs:20`), so code ties cannot happen.

### Coverage gate
The gate hides this gap completely. `src/backend/coverage.ps1:40` measures only `MiniWms.Domain`, `MiniWms.Application` and `MiniWms.Api`, so `LocationQueries.cs` (Infrastructure) is outside it. Line coverage could not catch M1 to M3 even if it were measured: each mutated `ThenBy` stays on a line that runs.

### Tests to add or strengthen
| priority | kind | test | file | closes |
|---|---|---|---|---|
| should | new | One zone. Create bin "B9" named "apple", then bin "B1" named "Apple", so the higher code gets the lower Id. Assert `sort=name` returns B9,B1 and `sort=-name` also returns B9,B1 (ties ascending by Id). A Code tie-break would give B1,B9. | `src/backend/MiniWms.Api.Tests/Locations/LocationListApiTests.cs` | F1, M1, AC 2 |

Not worth a test:
- F2, M2, M3: only a SQL-text check (`ToQueryString()`) can tell an explicit Id tie-break or a duplicate key apart from SQLite's natural order. The ticket puts that helper out of scope until the owner decides (`tickets/TASK-019.md:28`), and AC 3 is a code-reading check confirmed above. Worth raising with the owner as a follow-up decision.
- M4: equivalent mutant; ties on `Bin.Code` cannot happen (unique index).
- F3: a reporting and wording issue, not a test gap. The owner chose no traits (`ticket_plan.md:26`).

### Verdict
ready

## 5. ef-query-verifier
Scope: the diff of `src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs` (`ListBinsAsync`). The code and the SQL it should produce were read. No SQL was captured.

**Query table**

| query | LINQ | expected SQL shape | verdict |
|---|---|---|---|
| `ListBinsAsync`, no sort (`LocationQueries.cs:102`) | `BinPairs()` (Bins join Zones, `AsNoTracking`) with optional filters, then `OrderBy(p => p.Bin.Id)`, then `Select(BinRow)` and `ToPagedResultAsync` | `ORDER BY b.Id LIMIT/OFFSET`, plus a separate `COUNT(*)` over the same filters. `Id` appears once, and `Id` is the primary key. | clean (the dead second `Id` key is gone) |
| `ListBinsAsync`, sort=code/-code (`:98`) | `OrderBy(Descending)(Bin.Code).ThenBy(Bin.Id)` | `ORDER BY b.Code [DESC], b.Id`. `Code` is NOCASE with a unique index. | clean |
| `ListBinsAsync`, sort=name/-name (`:99`) | `OrderBy(Descending)(Collate(Bin.Name,"NOCASE")).ThenBy(Bin.Id)` | `ORDER BY b.Name COLLATE NOCASE [DESC], b.Id` | clean, but no index on `Name` (see Findings) |
| `ListBinsAsync`, sort=zone/-zone (`:100`) | `OrderBy(Descending)(Zone.Code).ThenBy(Bin.Id)` | `ORDER BY z.Code [DESC], b.Id` on the join | clean |
| `ListBinsAsync`, sort=isActive/-isActive (`:101`) | `OrderBy(Descending)(Bin.IsActive).ThenBy(Bin.Id)` | `ORDER BY b.IsActive [DESC], b.Id` | clean (a boolean sort key, so the tie-break matters) |
| Filters and paging (`:78-93`, `:105-107`) | `isActive`, `zoneId`, and `EF.Functions.Like` with escape `\` | parameterised `WHERE`, count then a sliced page | unchanged by the diff, clean |

How the diff reads:
- Each `?:` yields `IOrderedQueryable<BinPair>`, so `ThenBy` is valid. The shared `.ThenBy` after the switch is removed (`:105`).
- The `_` arm is the only arm without a tie-break.
- Every allowed sort field (code, name, zone, isactive) still ends in `Bin.Id`.
- The ordering is built on `IQueryable` before `Select`, `Skip` and `Take`, so nothing sorts in memory.
- No migration or contract file changed. The diff for `src/backend` touches only this file.

**Findings**
- No blocker or major findings.
- minor | `LocationQueries.cs:99,101` | Sorting by `Name` (NOCASE collation expression) or `IsActive` has no matching index. The only Bins indexes are the `Id` primary key, the unique `Code` index (`BinConfiguration.cs:20`) and the default foreign-key index on `ZoneId`. | This predates TASK-019, the task puts it out of scope, and the table is small, so no change is recommended. If volume grows, add a `NOCASE` index on `Name`.
- minor | `PagedQueryableExtensions.cs:13-14` | `ToPagedResultAsync` issues two statements (count, then page), so the count and page can drift under concurrent writes. | This is existing, accepted behaviour and out of scope.

**Tests (step 7 and step 8)**
- Ran `dotnet test src/backend --filter "FullyQualifiedName~LocationListApiTests"` (the same tests that the AC traits select). Result: 96 passed, 0 failed.
- Default order is covered by `Bins_default_to_page_1_of_25_and_the_second_page_holds_the_rest` (`LocationListApiTests.cs:65`, expects B26 on page 2).
- `Without_a_sort_the_lists_come_back_in_id_order` (`:243-250`) expects `"B3,B1,B2,B4"`. That differs from the code order, so it does separate Id order from Code order.
- Every allowed sort and tie-break is covered by `Bins_sort_by_every_allowed_field_with_the_id_as_tie_break` (`:158`, 8 cases).
- Name order with equal names is covered by `Bins_sort_by_name_without_regard_to_case_and_equal_names_keep_the_id_order` (`:202`).
- Name order across pages is covered by `Bins_name_order_holds_across_page_boundaries` (`:227`), which shows the order is applied in SQL before `Skip` and `Take`.
- Other test projects were not searched and test names were not listed with `--list-tests`. No "no test" claim is made, apart from the `ToQueryString()` helper in Not verified, judged from the ticket text.

**Not verified**
- The generated SQL text. The `ORDER BY` with `Id` once (criterion 3) is inferred from reading the LINQ. It is unknown without a SQL capture (`ToQueryString()` or logging). The ticket says to ask the owner before adding such a helper.
- `EXPLAIN QUERY PLAN` for the join and sort paths. A temp B-tree sort is expected for `name`, `isActive` and `zone`, but was not seen.
- The criterion 4 gates (the 90% coverage gate, `docs/reports/TASK-019/ac-results.md`, the `ac-log.md` row). Not run or checked.

**Verdict:** clean. The default bins order now puts `Bin.Id` in the ORDER BY once, every sort keeps the `Id` tie-break, and the Locations list tests pass (96/96).

## 6. spec-drift-auditor (component `locations`)
Branch `task/TASK-019-default-bins-order-id-once`. The working tree was checked, including the uncommitted TASK-019 change to `LocationQueries.cs`.

**Baseline**
- `node docs/specs/api/tools/ac-coverage.mjs`: exit 0. All 19 IDs are covered: LOC-1.1 to LOC-3.2 and PRD-1.1 to PRD-3.3. No ID is in product.md without a contract operation, and no contract lists an ID that product.md lacks.
- `npx --offline @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml" --fail-severity=warn`: exit 0, "No results with a severity of 'warn' or higher found!" It ran from the local npx cache.
- TASK-019 diff (`git diff`): it only moves the `ThenBy(Bin.Id)` tie-break into each sort arm, so the default bins order uses `Id` once. The contract does not specify a default order. No contract, schema or migration changes. No drift from this diff.

**Drift table**

All findings are low severity and are documentation versus code. No contradiction was found in status codes, schemas, columns, lengths, nullability, indexes or foreign keys.

| # | Spec claim | Code reality | Suggested direction of fix | Owner action |
|---|---|---|---|---|
| 1 | Bin `path` is "zone / bin, for display" (`docs/specs/api/locations.yaml:236`). The same wording is in `docs/specs/product.md:82` and the frontend model comment `src/frontend/src/app/features/locations/data/location.models.ts:29`. | Built as `ZoneCode + "/" + Code` with no spaces (`src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs:163`). The DTO doc says `ZONE/BIN` (`src/backend/MiniWms.Application/Locations/LocationDtos.cs:9`). `tickets/FEAT-001.md:23` agrees with the code (`<zone code>/<bin code>`). | Doc | Change the contract description (and the model comment) to `<zoneCode>/<binCode>`. |
| 2 | The zones list contract only says `type` is an allowed sort field and gives no order (`locations.yaml:46`, `locations.yaml:198`). | `type` sorts in enum order Receiving, Storage, Shipping, not alphabetically. A code comment claims "the contract wants enum order" (`LocationQueries.cs:16-18`, `LocationQueries.cs:48-49`). The contract does not say this. | Doc | Write the enum-order rule into the `listZones` description, or confirm alphabetical order is wanted and change the code. |
| 3 | The `updateZone` description names only "A stale version returns 409" as a 409 cause (`locations.yaml:85`). | Changing the type of a zone that has bins throws `DomainException` (`src/backend/MiniWms.Domain/Locations/Zone.cs:44-47`), which becomes a 409 (`src/backend/MiniWms.Api/Common/Errors/ConflictExceptionHandler.cs:20`). `docs/adr/ADR-003-api-conventions.md:40` documents this. | Doc | Add the type-change 409 to the `updateZone` description. |
| 4 | No operation in `locations.yaml` lists a 413 response (for example the write operations at `locations.yaml:33-40` and `locations.yaml:64-69`). | Every route returns a 413 problem when the body is over 64 KB (`src/backend/MiniWms.Api/Common/RequestLimits/RequestBodyLimitMiddleware.cs:21`, wired at `src/backend/MiniWms.Api/Program.cs:92`). Only `ADR-003-api-conventions.md:41` documents it. | Doc | Add a shared `PayloadTooLarge` response to the body-carrying operations, or record that cross-cutting statuses live only in ADR-003. |
| 5 | `Problem.detail` is present "when the text was written by our own code (validation and business-rule messages)" (`locations.yaml:300`). `ADR-003-api-conventions.md:26` says the same. | 400 responses never carry `detail`: `WriteValidation` sets no `Detail` and does not call `AllowDetail` (`src/backend/MiniWms.Api/Common/Errors/ProblemResponses.cs:21-31`). Only the 409 path sets it (`ProblemResponses.cs:33-45`, customizer at `Program.cs:26-33`). | Doc | Reword: validation messages go in `errors`, and `detail` appears only on a business-rule or stale-version 409. |
| 6 | The ADR-003 example 400 body has `"title": "Validation failed"` (`ADR-003-api-conventions.md:28`). | The real title is "One or more validation errors occurred." (`ProblemResponses.cs:27`). | Doc | Update the example. |
| 7 | LOC-1.1: on a fresh database, "when the app starts, then exactly one warehouse exists" (`docs/specs/product.md:80`). | Migrate and seed run only when the environment is Development (`Program.cs:69-81`). In any other environment no warehouse is created and `GET /warehouse` returns 404. `ADR-002-ef-core-sqlite.md:15` and `src/backend/REPO_CONTEXT.md:53` already say "Development only". | Doc (product.md) | Add "in the Development environment" to LOC-1.1, or decide that seeding must run in every environment (code change). |
| 8 | "Every text column with a maximum length has a `CHECK (length(...) <= max)`" (`docs/specs/data-model.md:9`). | `Zones.Type` has max length 20 (`src/backend/MiniWms.Infrastructure/Persistence/Configurations/ZoneConfiguration.cs:20`, migration `20261119055330_InitialLocationsProducts.cs:70`) but no length CHECK. It only has `CK_Zones_Type IN (...)` (migration `:83`), which caps the length in practice. | Doc | Note that an enum `IN` check stands in for the length CHECK. A code fix would need a migration and adds nothing. |
| 9 | Root context rule: the parent keeps "an index table, `file \| read when you touch`, that lists every child" (`REPO_CONTEXT.md:53`). | The root lists its children `src/backend/REPO_CONTEXT.md` and `src/frontend/REPO_CONTEXT.md` only in the Layout table (`REPO_CONTEXT.md:15-16`). No file in the repo has a `read when you touch` table. Unknown whether the tests accept the Layout table as the index. | Doc | Owner, on a `setup/` branch (protected file): add the index table or reword the rule. |

Consistent (no drift): every operation, route and verb; 201 plus `Location` on create; 405 plus `Allow: GET, PATCH` on POST and DELETE `/warehouse`; duplicate code returns 400 under `code`, case-insensitive; unknown or Inactive zone returns 400 under `zoneId`; deactivate returns 409, and an already Inactive item returns 200 unchanged; stale version returns 409; paging defaults and limits; sort allow-lists; `q` max 100 with ASCII-only case folding; `binCount` counts inactive bins; tree `isActive` filters zones and bins independently; DTO fields match the schemas; the data model (lengths, NOCASE unique indexes, RESTRICT foreign keys, `Version` concurrency token, PRAGMAs per ADR-002); ADR-001 layering (Application has no EF Core reference); ui.md section 3 (tabs, tree toggle, list columns and filters, paginator 25/50/100, read-only code on edit, zone type locked while the zone has bins, confirm dialog focuses Cancel, 409 keeps the dialog open); backend and frontend REPO_CONTEXT commands and versions match `global.json` (10.0.300) and `package.json` (Angular/Material 22.3.0, TS ~6.0.2, RxJS ~7.8.0, Vitest 5).

**Untested spec IDs**
None. Each LOC ID maps to FEAT-001 ACs that have tests (backend `[Trait("AC","FEAT-001.n")]`, frontend `FEAT-001.n` test names):
- LOC-1.1: FEAT-001 AC1 and AC16. The "from configuration" part is proven by `[Trait("AC","TASK-005.2")]` in `src/backend/MiniWms.Api.Tests/Persistence/WarehouseSeedReviewTests.cs:18`.
- LOC-1.2: AC3 and AC20.
- LOC-1.3: AC5 and AC21.
- LOC-1.4: AC4, AC6, AC20 and AC21.
- LOC-2.1: AC2, AC7, AC17 and AC22.
- LOC-2.2: AC9 and AC23.
- LOC-2.3: AC8 and AC23.
- LOC-2.4: AC10 and AC23.
- LOC-3.1: AC11, AC18 and AC19.
- LOC-3.2: AC12 and AC19.

Gaps in the evidence, not missing tests:
- LOC-2.2 is proven only with a fake `IStockQuery`. Production uses `ZeroStockQuery`, which always returns 0, until Inventory exists.
- The "cannot be chosen for new stock" part of LOC-2.3 is proven only through the `isActive` filter.
- The Admin-role parts of the ACs are N/A until Auth exists (`tickets/FEAT-001.md:48`).

**Not verified**
- `dotnet test` and `npm test` were not run; they are outside this agent's allowed commands. So tests were confirmed to exist for each ID, not that they pass.
- Runtime-only behaviour is unknown, for example which `Allow` header ASP.NET routing sends on `PUT /warehouse`. The contract does not cover PUT.
- The Angular versions actually installed per `package-lock.json`, and the Node/npm versions in `src/frontend/REPO_CONTEXT.md:6`, are unknown. Only `package.json` was read.
- ui.md section 1 (shell and navigation) is outside the `locations` code paths. The real-browser UI checks were not run (FEAT-001 records them as NOT RUN).
- Whether the context-file tests treat the root Layout table as the required child index is unknown (finding 9).

**Verdict**: drift found: 9 low-severity doc mismatches; no contract, schema or behaviour contradiction in the code, and the TASK-019 change is in sync.

## Summary
| Agent | Verdict | Blockers | Majors | `must` rows | Critical/high security | Drift rows | Run or skipped |
|---|---|---|---|---|---|---|---|
| own-code-review | pass | 0 | 0 | - | - | - | run; build and tests passed, so no fail-fast stop |
| independent-code-review | approve | 0 | 0 | - | - | - | run (ID only) |
| security-reviewer | approve | 0 | 0 | - | 0 | - | run (ID only; first run on a ticket) |
| test-critic | ready | 0 | 0 | 0 (1 `should`: tied-name test with code order opposite to Id order) | - | - | run |
| ef-query-verifier | clean | 0 | 0 | - | - | - | run (`MiniWms.Infrastructure/Locations/LocationQueries.cs` changed) |
| spec-drift-auditor | drift found | - | - | - | - | 9 (low, doc side) | run on `locations` |

## Stop notices
- **Drift:** any drift in the component stops the ticket (owner rule). Fix the docs or the code, or record a decision to continue in the ticket Notes, before `/code-review` and the PR. None of the 9 rows comes from TASK-019. Rows 1 to 8 match drift rows the owner already recorded on TASK-018 for a separate `chore/` PR, and that PR is not done yet. Row 9 (the root `REPO_CONTEXT.md` index table) is new.
- **Security:** no block.
