# TASK-004 acceptance-criteria results

Ticket: `tickets/TASK-004.md` (EF Core SQLite persistence foundation). Branch: `task/TASK-004-ef-core-sqlite-foundation`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval, `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. The numbers below come from my own runs in the main working tree after the hand-offs.

| Command (from `src/backend` unless noted) | Result |
|---|---|
| `dotnet build` | 0 Warning(s), 0 Error(s) |
| `dotnet test --no-build` (run twice) | Both runs: `MiniWms.Api.Tests` Passed 84, `MiniWms.Application.Tests` Passed 46, Failed 0 |
| `dotnet test --no-build --filter "AC=TASK-004.<n>"` | n=1: 3, 2: 6, 3: 7, 4: 2, 5: 7, 6: 3, 7: 4, 9: 6 tests, all passed; n=8: no test carries that trait (see AC 8) |
| `dotnet tool run dotnet-ef --version` | 10.0.15 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` (repo root) | `Combined line coverage: 100% (5 of 5 lines); threshold 90%`. `GATE PASSED.` |
| `src/backend/MiniWms.Api/App_Data` after the whole test and coverage runs | does not exist (deleted before the runs) |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | Local `dotnet-ef` 10.x next to ReportGenerator in the manifest | PASS | 3 tests (`RepositoryConfigurationTests`) | Tests passed; `dotnet tool run dotnet-ef --version` prints 10.0.15 | 2026-11-19 |
| 2 | Every connection: foreign keys on, WAL, busy_timeout 5000 | PASS | 6 tests (`ConnectionSettingsTests`, `TemporaryDatabaseTests`) | Tests passed | 2026-11-19 |
| 3 | Audit fields set centrally; Version increases on update | PASS | 7 tests (`AuditAndVersionTests`, fake clock and user) | Tests passed | 2026-11-19 |
| 4 | Stale Version raises a concurrency exception, nothing stored | PASS | 2 tests (`AuditAndVersionTests`) | Tests passed | 2026-11-19 |
| 5 | Development start creates the DB under `App_Data` and applies migrations | PASS | 2 tests (`HostDatabaseTests`, temporary content root) | Tests passed; real `App_Data` not created | 2026-11-19 |
| 6 | git lists no `.db`, `-wal`, `-shm` | PASS | 3 tests (`git check-ignore`, `RepositoryConfigurationTests`) | Tests passed | 2026-11-19 |
| 7 | Per-test temporary DB, files removed afterwards | PASS | 4 tests (`TemporaryDatabaseTests`) | Tests passed; the cleanup test was flaky at first (global `ClearAllPools`) and was fixed in the test; 10 consecutive full runs passed | 2026-11-19 |
| 8 | All tests pass, with `[Trait("AC", "TASK-004.<n>")]` on those proving the ACs | PASS (by real runs, no unit test) | none by design | Every AC 1 to 7 and 9 has tagged tests (filter runs above); 130 of 130 tests pass | 2026-11-19 |
| 9 | Existing API tests use a temporary DB, never `App_Data/miniwms.db`; pass in parallel | PASS | 6 tests (`HostDatabaseTests`) plus the changed `Host/ApiFactory.cs` used by all 46 existing Api tests | Tests passed twice in parallel; `App_Data` absent after the runs | 2026-11-19 |

Totals: PASS 9, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 8 of 9 ACs have dedicated tests (88.9%). AC 8 is proven by the runs, not by a test.
- Pass: 130 of 130 tests pass (100%); 38 new tests.
- Coverage: 100% (gate 90%). Infrastructure is not part of the gate (ADR-005; open owner decision).

## Review round (`/code-review`, 9 findings)
Fixed: `:memory:` / `file:` / `Mode=Memory` sources no longer treated as file paths (new `SqliteDataSource`, 5 tests); `busy_timeout` now set before WAL; `DateTimeOffset` stored as binary so SQLite can order and compare it; temp-file cleanup no longer throws. After the fixes: build 0 warnings, 84 + 46 tests pass twice, gate 100%.
Not changed (by design or for the owner): `dev-admin` user and Development-only migrate follow ADR-002 until auth exists; `Version` stamping relies on callers setting `OriginalValue` (TASK-006 must enforce it); design-time factory uses a throwaway `design-time.db`.
