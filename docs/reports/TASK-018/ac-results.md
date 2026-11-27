# TASK-018 acceptance-criteria results

Ticket: `tickets/TASK-018.md` (Sort zones and bins by name without regard to case). Branch: `task/TASK-018-case-insensitive-name-sort`. Date: 2026-11-26.
How it was run: `/start-ticket` flow with `ticket-scout` and, for the first time, `spec-drift-auditor` before the plan. It found 11 low-severity, doc-side drift rows in `locations` and stopped; the owner recorded a decision to continue (ticket Notes). Then owner approval of the plan, `change-implementer` (query, contract, backend context file) and `test-writer` (tests), each in an isolated worktree carried back by `worktree-apply.mjs`. The numbers below are from the main session's own runs, not from the agents' summaries.

| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | Domain 63, Application 235, Api 470 passed, 0 failed (Api was 464; 6 new cases) |
| `dotnet test src/backend/MiniWms.Api.Tests --no-build --filter "AC=TASK-018.n"` | n=1: 2 passed; n=2: 2; n=4: 2; 0 failed |
| the same with `AC~FEAT-001.11` (the existing list, sort and filter tests) | 62 passed, unchanged |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | `Combined line coverage: 97.05% (657 of 677 lines); threshold 90%` `GATE PASSED.` |
| `node docs/specs/api/tools/ac-coverage.mjs` | 19 of 19 covered |
| `npx --offline @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml" --fail-severity=warn` | No results with a severity of warn or higher |
| Fix reverted for a moment (`LocationQueries.cs` from `HEAD`), `--filter "AC~TASK-018\|AC~FEAT-001.11"` | 6 failed (all 6 new cases), 62 passed; the fix was then restored byte for byte and the 6 pass again |

| AC # | AC text (short) | Result | Evidence | Date |
|---|---|---|---|---|
| 1 | Zones by `name` ignore case, both directions, equal names keep the `Id` order | PASS | `Zones_sort_by_name_without_regard_to_case_and_equal_names_keep_the_id_order`: seed "apple", "Banana", "Apple", "cherry", "Zebra"; `name` gives `ZA,ZC,ZB,ZD,ZE`, `-name` gives `ZE,ZD,ZB,ZA,ZC` | 2026-11-26 |
| 2 | The same for bins | PASS | `Bins_sort_by_name_without_regard_to_case_and_equal_names_keep_the_id_order`: `B1,B3,B2,B4,B5` and `B5,B4,B2,B1,B3` | 2026-11-26 |
| 3 | Every other sort field and the default order unchanged | PASS (by the existing tests, no new test) | The 62 `FEAT-001.11` tests pass with no change to their expected values; no test line removed (owner decision: no `TASK-018.3` trait) | 2026-11-26 |
| 4 | Order done in SQL with `EF.Functions.Collate`, no EF in Application, no migration | PASS | `Zones_name_order_holds_across_page_boundaries` and `Bins_name_order_holds_across_page_boundaries` (`pageSize=2`: page 1 holds the two case-insensitively first names, the pages joined equal the full order); the diff: `Collate` in `LocationQueries.cs` only, no file under `Application` or `Migrations` changed | 2026-11-26 |
| 5 | Contract says the name sort ignores case, ASCII only; lint and coverage pass | PASS (by diff and real runs, no test) | `listZones` and `listBins` descriptions; Spectral and `ac-coverage.mjs` above | 2026-11-26 |
| 6 | Tests tagged, suite and gate, context file, report and log row | PASS (by real runs, no test) | The command table; `src/backend/REPO_CONTEXT.md` has one line on the name sort (79 lines); this file and the row in `docs/reports/ac-log.md` | 2026-11-26 |

Totals: PASS 6, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 3 of 6 criteria have `TASK-018` tagged tests (50%); criterion 3 is proven by the existing tagged tests, 5 and 6 by the diff and real runs (owner decision).
- Pass: 6 of 6 criteria; all 768 backend tests pass.
- Coverage: 97.05% combined (gate 90%). `LocationQueries.cs` is in Infrastructure, which the gate does not measure (ADR-005).

## Notes
- The SQL text itself (`ORDER BY ... COLLATE NOCASE`) was not captured; the page-boundary tests show that the order is applied before paging, and the reverted run shows that the new tests depend on the collation.
- `NOCASE` folds ASCII letters only; the contract says so.
- The 11 drift rows found at the start are not fixed here: a separate `chore/` PR (owner decision).
- Not run: a check in a real browser.

## Review chain (`/start-ticket` after the build, first run)
| Agent | Verdict | Notes |
|---|---|---|
| `own-code-review` | pass | 4 minor findings (a comment's placement, repeated `Collate`, where the context line sits, the SQL text not captured); no tidy-ups (owner decision) |
| `test-critic` | ready | No tests to add. Not catchable by a black-box test: removing the `Id` tie-break (rowid equals Id) and an in-memory sort before paging; the coverage gate does not measure Infrastructure |
| `independent-code-review` | approve with changes | Criterion 6 asked for a `TASK-018.3` trait that the plan decision ruled out; criterion 6 was reworded (owner decision) |
| `ef-query-verifier` | clean | `ORDER BY Name COLLATE NOCASE, Id` inferred from the LINQ, not captured; no index on `Name`, as before the change |
| `spec-drift-auditor` | drift found | 12 doc-side rows, none from TASK-018: the 11 recorded at the start and one more (the zone `type` sort order is not stated in the contract), added to the decision by the owner; all go to a separate `chore/` PR |

All five ran one after the other, changed no files and stopped once. Not run yet: `/code-review`.
