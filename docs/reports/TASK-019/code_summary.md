# TASK-019 code summary

Ticket: `tickets/TASK-019.md` (Order the default bins list by Id once)
Branch: `task/TASK-019-default-bins-order-id-once` (uncommitted on `main` at `7aaac1e`)
Date: 2026-11-27

## Changed files
| File | Change |
|---|---|
| `src/backend/MiniWms.Infrastructure/Locations/LocationQueries.cs` | `ListBinsAsync`: each named sort arm ends in `.ThenBy(p => p.Bin.Id)`; the `_` arm stays `OrderBy(p => p.Bin.Id)` alone; the shared `.ThenBy` after the `switch` is removed (+5 / -5 lines) |
| `docs/reports/TASK-019/ticket_plan.md` | new: the approved plan |
| `docs/reports/TASK-019/ac-results.md` | new: acceptance-criteria results |
| `docs/reports/ac-log.md` | one row added |

No test, contract, migration or Application file changed.

## change-implementer
- Did step 1 of the plan only, in an isolated worktree, carried back with `worktree-apply.mjs`.
- Each named arm (`code`, `name`, `zone`, `isactive`, both directions) is wrapped in parentheses with `.ThenBy(p => p.Bin.Id)`. The default arm is unchanged, and the shared `.ThenBy` is removed. The variable stays `IOrderedQueryable<BinPair>`, and the order stays in SQL.
- Its runs: `dotnet build src/backend` 0 warnings, 0 errors; `dotnet test src/backend` 0 failures (Domain 63, Application 235, Api 470).
- The worktree folder could not be removed (EPERM). The owner deletes `.claude/worktrees/agent-af8060f6350ae17e8` by hand, then runs `git worktree prune` and deletes branch `worktree-agent-af8060f6350ae17e8`.

## test-writer
- Changed no file, as the owner decided: no new test, no new expected value, no new trait.
- The five proving tests passed (13 cases). Full suite: 768 passed. Coverage: 97.05%, gate passed.
- It reasoned that the tie-break tests might not fail if a per-arm `ThenBy` were dropped, because SQLite often returns ties in rowid order. Not checked by experiment.

## Build and tests (main session's own runs)
| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | Domain 63, Application 235, Api 470 passed, 0 failed |
| the five bins default, sort, name-order and page-boundary tests, by name | 13 passed, 0 failed |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | Combined line coverage 97.05% (657 of 677 lines); GATE PASSED |
| `own-code-review`: `dotnet build` and `dotnet test` | pass, 768 tests |

## Acceptance criteria
PASS 4, FAIL 0, N/A 0, NOT RUN 0 (details in `ac-results.md`). Criteria 1 and 2 are proven by the existing tests passing unchanged, and 3 and 4 by the diff and real runs (owner decision: no new tests, no traits).
