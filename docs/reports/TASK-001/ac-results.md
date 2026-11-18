# TASK-001 acceptance-criteria results

Ticket: `tickets/TASK-001.md` (Backend solution skeleton and build configuration). Branch: `task/TASK-001-backend-solution-skeleton`. Date: 2026-11-19.
How it was run: the main session did the work (see "Process note"). Results below come from these real runs on the branch:

| Command | Result |
|---|---|
| `dotnet --version` (inside the repo) | `10.0.300` |
| `dotnet build src/backend` | Build succeeded. 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | `Passed! - Failed: 0, Passed: 14, Skipped: 0, Total: 14` (MiniWms.Application.Tests); exit code 0. The Domain and Api test projects contain no tests yet and report "No test is available" |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | Solution builds with zero warnings and contains the seven projects | PASS | `Solution_contains_the_seven_projects` | Build: 0 warnings, 0 errors; test passed | 2026-11-19 |
| 2 | Reference rules: Domain none, Application to Domain, Infrastructure to Application and Domain, Api to Application and Infrastructure | PASS | `Domain_references_nothing`, `Application_references_only_Domain`, `Infrastructure_references_only_Application_and_Domain`, `Api_references_only_Application_and_Infrastructure`, `Source_projects_never_reference_test_projects` | 5 tests passed | 2026-11-19 |
| 3 | `global.json` pins the .NET 10 SDK | PASS | `Global_json_pins_the_dotnet_10_sdk` | Test passed; `dotnet --version` printed `10.0.300` | 2026-11-19 |
| 4 | `Directory.Build.props` and `src/backend/.editorconfig`: nullable, implicit usings, warnings as errors | PASS | `Build_props_turn_on_nullable_implicit_usings_and_warnings_as_errors`, `Editorconfig_sits_in_src_backend` | 2 tests passed; build clean with warnings as errors on | 2026-11-19 |
| 5 | Package versions only in `Directory.Packages.props` | PASS | `Package_versions_are_managed_centrally`, `No_project_file_carries_a_package_version` | 2 tests passed; restore and build succeeded with central management | 2026-11-19 |
| 6 | `Common/` and the module folders exist as placeholders | PASS, with a deviation (see below) | `Every_source_project_has_a_Common_folder`, `Domain_has_the_six_module_folders` | 2 tests passed | 2026-11-19 |
| 7 | `dotnet test` passes and each AC-proving test carries `[Trait("AC", "TASK-001.<n>")]` | PASS | the whole run | 14 of 14 tests pass, exit code 0; all 14 `[Fact]` methods carry an `AC` trait (14 traits, 14 facts) | 2026-11-19 |
| 8 | `src/backend/REPO_CONTEXT.md` lists layout, commands and ADR-001 rules, under 80 lines | PASS | `Backend_repo_context_is_short_and_lists_layout_and_commands` | Test passed; file is 38 lines | 2026-11-19 |

Totals: PASS 8, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 7 of 8 ACs have a dedicated test (87.5%). AC 7 is a property of the test run itself and has no separate test.
- Pass: 14 of 14 (100%).
- Coverage: not measured. Coverage tooling is TASK-003.

## Deviation for the owner to confirm (AC 6)
The approved plan created the module folders in all four source projects (28 `.gitkeep` files). With the 19 other files that would make the PR touch about 47 files, over the 30-file limit. I kept `Common/` in all four projects and put the six module folders in `MiniWms.Domain` only; Application, Infrastructure and Api get theirs with each module's first ticket. The ticket wording "where a project needs them" allows this, but it is narrower than the plan. If you want all 28 placeholders, the limit has to rise or the placeholders move to a separate ticket.

## Process note (harness finding)
`change-implementer` and `test-writer` could not start. Claude Code refused the isolation worktree twice with "Refusing to use c:\Users\...\.claude\worktrees\agent-... as an isolation worktree: git resolves its working tree to C:/Users/..." The worktrees were healthy: `git rev-parse --show-toplevel` inside one returned the worktree itself. The refusal compares the lowercase-drive, backslash path with the uppercase-drive, forward-slash path as different folders. Changing the shell's drive-letter casing did not help. The two empty worktrees and their branches were removed. The implementation was then done in the main session with the scope guard active (the guard allowed every edit inside `allowed_paths`). So the agent hand-off, and guard behaviour inside a worktree, are still untested.
