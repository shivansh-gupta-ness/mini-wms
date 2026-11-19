# TASK-005 acceptance-criteria results

Ticket: `tickets/TASK-005.md` (Locations and Products schema, first migration and warehouse seed). Branch: `task/TASK-005-locations-products-schema`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (with five owner decisions recorded in the ticket), `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. The numbers below come from my own runs in the main working tree after the hand-offs.

| Command | Result |
|---|---|
| `dotnet build` (src/backend) | 0 Warning(s), 0 Error(s) |
| `dotnet test --no-build` (run twice) | Both runs: Domain.Tests Passed 61, Api.Tests Passed 153, Application.Tests Passed 46, Failed 0 |
| `dotnet test --no-build --filter "AC=TASK-005.<n>"` | n=1: 35, 2: 5, 3: 6, 4: 3, 5: 4 Domain + 10 Api, 6: 59, 7: 2 Domain + 6 Api, 8: 4; all passed |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | Domain 100%, Api 100%; `Combined line coverage: 100% (98 of 98 lines); threshold 90%`. `GATE PASSED.` |
| `dotnet tool run dotnet-ef migrations has-pending-model-changes` (implementer run) | "No changes have been made to the model since the last migration." |
| `src/backend/MiniWms.Api/App_Data` after all runs | does not exist |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | Migrations create Warehouse, Zone, Bin, Product tables with columns, lengths, keys, constraints | PASS | 35 tests (`SchemaTests`, `ConstraintTests`, audit tests): columns, NOT NULL, keys, FKs, NOCASE unique indexes, 11 CHECKs, over-length and negative reorder rejected | Tests passed | 2026-11-19 |
| 2 | Fresh start has exactly one `MAIN` warehouse "Main warehouse"; restart adds no second | PASS | 5 tests (`WarehouseSeedTests`: fresh host, second host on same file, seeder twice, existing `main` row) | Tests passed | 2026-11-19 |
| 3 | Codes/SKUs differing only by case are rejected | PASS | 6 tests (`ConstraintTests`, SQLite error 2067) | Tests passed | 2026-11-19 |
| 4 | Bin with a missing zone is rejected (FK) | PASS | 3 tests (`ConstraintTests`, SQLite error 787; also zone without warehouse) | Tests passed | 2026-11-19 |
| 5 | Zone type outside Receiving/Storage/Shipping is rejected | PASS | 10 Api tests (CHECK, raw SQL) + 4 Domain tests (undefined enum) | Tests passed | 2026-11-19 |
| 6 | Domain enforces zone type, active-flag and reorder-level invariants | PASS | 59 Domain tests (`ZoneTests`, `BinAndWarehouseTests`, `ProductTests`). Stock facts are passed as parameters; the stock-based checks themselves wait for the inventory module | Tests passed | 2026-11-19 |
| 7 | All tests pass with AC traits | PASS | 8 trait-presence tests (6 Api, 2 Domain) | 260 of 260 tests pass, twice | 2026-11-19 |
| 8 | First migration made with local `dotnet-ef`, committed with snapshot, no pending changes | PASS | 4 tests (`MigrationTests`: files exist, migration listed, `HasPendingModelChanges()` false, no pending migration on a new DB) | Tests passed; implementer's CLI check printed no changes | 2026-11-19 |

Totals: PASS 8, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 8 of 8 ACs have dedicated tests (100%).
- Pass: 260 of 260 tests pass (100%); 130 new tests (61 Domain, 69 Api).
- Coverage: 100% (gate 90%).

## Notes
- Fixture: `TemporaryDatabase` now migrates through a plain `AppDbContext` and creates only the test-only table with hard-coded SQL; update that SQL if `PersistenceTestItem` changes.
- `Migrations/.editorconfig` marks the generated migration as generated code (otherwise IDE0161 fails the build).
- `IsActive` default uses `HasDefaultValueSql("1")`, because `HasDefaultValue(true)` makes EF turn an explicit `false` into `true`.
- Not exercised: the `Seed:Warehouse:*` configuration override.
- PR size: about 2,400 changed lines in 35 files; about 720 lines are generated migration files. Within the 5000-line and 60-file limits.
