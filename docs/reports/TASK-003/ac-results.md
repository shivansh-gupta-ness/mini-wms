# TASK-003 acceptance-criteria results

Ticket: `tickets/TASK-003.md` (Backend test and coverage tooling with 90% gate). Branch: `task/TASK-003-backend-test-coverage-tooling`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval, `change-implementer` (steps 1 to 4 and 6, then one fix round) and `test-writer` (step 5), each in an isolated worktree, work carried back with `worktree-apply.mjs`. The numbers below come from my own runs in the main working tree after the hand-offs, not from the agents' summaries.

| Command (from the repository root) | Result |
|---|---|
| `dotnet tool restore` | Restore was successful; `dotnet tool run reportgenerator` runs (ReportGenerator 5.6.0) |
| `dotnet build src/backend` | Build succeeded. 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | `MiniWms.Api.Tests`: Passed 12. `MiniWms.Application.Tests`: Passed 46, Failed 0. `MiniWms.Domain.Tests`: no tests yet. Exit code 0 |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` (clean `TestResults`) | Exit 0. 3 Cobertura files produced (one per test project). Domain n/a, Application n/a, Api 100%. `Combined line coverage: 100% (5 of 5 lines); threshold 90%`. `GATE PASSED.` |
| the same with `-Threshold 101` | Exit 1. `GATE FAILED: line coverage 100% is below the 101% threshold (shortfall 1 percentage points).` |
| `... coverage.ps1 -Cobertura "src/backend/TestResults/*/coverage.cobertura.xml"` | Exit 0, found 3 files, ran no tests, `GATE PASSED.` |
| `dotnet test src/backend --no-build --filter "AC=<id>"` | `TASK-001.2` ran 5 tests, `TASK-002.5` ran 3, `TASK-003.5` ran 4, all passed; an unknown id (`TASK-099.9`) ran none. Each count equals the number of tests carrying that trait in the source |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | `dotnet tool restore` installs ReportGenerator from `.config/dotnet-tools.json` | PASS | 3 tests in `CoverageToolingTests` (manifest exists; lists `dotnet-reportgenerator-globaltool` with a version; declares the `reportgenerator` command) | Tests passed; real `dotnet tool restore` succeeded and `dotnet tool run reportgenerator` runs | 2026-11-19 |
| 2 | `dotnet test ... --collect:"XPlat Code Coverage"` produces a Cobertura file for each test project | PASS | 6 tests (coverlet.collector without a version in all three test projects, version in `Directory.Packages.props`, runsettings enables the collector with cobertura format) | Tests passed; the real run produced 3 Cobertura files, one per test project | 2026-11-19 |
| 3 | The report command writes a per-assembly text summary under `TestResults/` and prints it | PASS | 4 tests (script run in report-only mode on a fixture: `Summary.txt` written under the report folder, per-assembly lines for Domain, Application and Api printed) | Tests passed; the real run wrote `src/backend/TestResults/coverage-report/Summary.txt`, which git ignores (0 entries in `git status`) | 2026-11-19 |
| 4 | Migrations, `Program.cs` and generated code excluded; only Domain, Application and Api counted | PASS | 9 tests (include list is exactly the three assemblies; excludes `Program.cs`, `Migrations`, `*.g.cs` and the GeneratedCode and CompilerGenerated attributes; a scope fixture with an Infrastructure package at 0% still gates at 100% and does not list Infrastructure) | Tests passed; in the real run no Cobertura file contained `Program.cs`, `MiniWms.Infrastructure` or a test assembly | 2026-11-19 |
| 5 | Below 90% the gate exits non-zero and names the shortfall; at or above 90% it exits zero | PASS | 4 tests (85% exits 1 and names percentage, threshold and shortfall; exactly 90% exits 0; 95% exits 0) | Tests passed; the real run with `-Threshold 101` exited 1 with the shortfall message | 2026-11-19 |
| 6 | `dotnet test --filter "AC=<id>"` runs exactly the tests for that AC | PASS (by real runs, no unit test) | none by design | See the filter runs above: 5, 3 and 4 tests for three ids, each equal to the trait count in the source; an unknown id runs none. A data-driven test counts once per case (`TASK-003.4` runs 9 tests from 6 trait lines) | 2026-11-19 |
| 7 | `src/backend/REPO_CONTEXT.md` lists the test, coverage and gate commands | PASS | 6 tests (`dotnet tool restore`, `XPlat Code Coverage`, `coverage.ps1`, `-ExecutionPolicy Bypass`, `--filter "AC=`, under 80 lines) | Tests passed; the file is 45 lines | 2026-11-19 |

Totals: PASS 7, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 6 of 7 ACs have dedicated tests (85.7%). AC 6 is proven by real command runs, not by a test.
- Pass: 58 of 58 tests pass across the solution (32 new in `CoverageToolingTests`: 3 + 6 + 4 + 9 + 4 + 6) (100%).
- Coverage: 100% line coverage, but only 5 coverable lines exist (all in `MiniWms.Api.Common.HealthEndpoints`). Domain and Application have no code yet, so the gate has not been tested against real code.

## Process notes
- The editing agents ran in isolated worktrees accepted by the `WorktreeCreate` hook. No hook blocked an edit or a command. Three hand-offs (8 files, 2 files, 6 files) applied cleanly.
- `change-implementer` needed a second round. My own checks found two defects its first run did not: the report-only mode (`-Cobertura`) resolved a relative path after changing into the script's folder and found no files, and this machine's PowerShell policy refuses `.ps1` files, so the documented `powershell -File` command failed. Both were fixed by the agent and re-verified by me. Every documented invocation now uses `-NoProfile -ExecutionPolicy Bypass`; the flag applies to one run only and changes no machine setting.
- `test-writer` reported 10 tests for AC 4; the real count is 9 (the agent's table was off by one, its total of 32 was right).
- The scout's brief had three stale points (agents have no network, a 400-line PR limit, the frontend runner line from ADR-005); all were corrected before approval.

## Open points for the owner
- The coverage gate is only proven on fixtures and on 5 real lines. It will first bite when TASK-004 to TASK-006 add real code.
- `dotnet tool restore` runs on every script call, so the script needs ReportGenerator in the NuGet cache or network access.
- ADR-005 still says "TASK-003 records the frontend test runner"; that is TASK-007 now. Suggested chore fix.
- Anyone running the script must pass `-ExecutionPolicy Bypass` unless they change their PowerShell policy.
