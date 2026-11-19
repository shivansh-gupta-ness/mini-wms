# TASK-010 acceptance-criteria results

Ticket: `tickets/TASK-010.md` (Frontend test and coverage gate). Branch: `task/TASK-010-frontend-coverage-gate`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (decisions recorded in the ticket, including the npm-registry exception for this ticket), `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. Numbers come from my own runs in the main working tree, except where a row says otherwise.

| Command (in `src/frontend`) | Result |
|---|---|
| `npm run build` | Succeeded |
| `npm run test:coverage` (run twice) | Both runs: Test Files 14 passed (14), Tests 141 passed (141); coverage table printed; `Line coverage: 98.41% (124 of 126 lines); threshold 90%.` `GATE PASSED.` |
| `npm run test:gate` | 22 tests, 22 pass, 0 fail, exit 0 |
| `node scripts/coverage-gate.mjs --threshold 101` | `GATE FAILED: line coverage 98.41% is below the 101% threshold (shortfall 2.59 percentage points).` exit 1 |
| `npm test -- --filter "\bTASK-010\.2\b"` (and `.3`, `.6`) | 3 passed, 138 skipped, for each id (3 tests carry each id) |
| `git status` after the runs | `coverage/` does not appear (git-ignored by `src/frontend/.gitignore`) |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | `npm test` runs once and exits with the pass or fail status | PASS (by real runs, no unit test) | none by design | `npm test` and `npm run test:coverage` exit 0 and exit after running (`--no-watch`). The test-writer also ran a deliberately failing temporary spec: `Tests 1 failed | 138 passed`, exit 1, then deleted it (reported by the agent, not re-run by me) | 2026-11-19 |
| 2 | Coverage command prints a summary and writes it under a git-ignored folder | PASS | 5 tests (`TASK-010.2`, `tooling.spec.ts`: the provider version equals the installed Vitest, the old report is removed first, reporters `text` and `json-summary`, `coverage` ignored by `src/frontend/.gitignore`, `test:coverage` runs `--coverage` and then the gate) | Tests passed; the real run printed the table and wrote `coverage/mini-wms/coverage-summary.json`, which git does not list | 2026-11-19 |
| 3 | Only `src/app` is counted; test files and generated code excluded | PASS | 3 tests (`TASK-010.3`: include is `src/app/**/*.ts`, exclude has the spec and `.d.ts` patterns, the gate script lives outside `src/app`) | Tests passed; the real report lists only `app/...` files | 2026-11-19 |
| 4 | Below 90% the gate exits non-zero and names the shortfall; at or above 90% it exits zero | PASS | 22 tests (`TASK-010.4`, `coverage-gate.test.mjs` under `node --test`: 84%, exactly 90%, 95%, 89.99%, threshold 101, missing or invalid input, an empty report, thresholds that would switch the gate off, missing option values; the real script spawned once) | 22 of 22 pass; real run with `--threshold 101` exited 1 with the shortfall message | 2026-11-19 |
| 5 | A name filter on an AC id runs only the tests for that AC | PASS (by real runs, no unit test) | none by design | `npm test -- --filter "\bTASK-010\.N\b"` ran 3 of 141 tests for N = 2, 3 and 6, equal to the number of tests carrying each id. A filter matches a test's full name, so tests in a `describe` whose title contains the id also run (for `TASK-009.1` it ran 35 tests) | 2026-11-19 |
| 6 | `src/frontend/REPO_CONTEXT.md` lists the test, coverage and gate commands | PASS | 3 tests (`TASK-010.6`: the commands, a `--filter` example with `TASK-010`, at most 78 lines) | Tests passed; the file is 74 lines | 2026-11-19 |

Totals: PASS 6, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 4 of 6 ACs have dedicated tests (66.7%); AC 1 and AC 5 are proven by real runs, as AC 6 of TASK-003 was.
- Pass: 141 of 141 frontend tests and 22 of 22 gate tests pass (100%); 12 new frontend tests and 22 gate tests.
- Coverage: 98.41% line coverage of `src/app` (gate 90%).

## Notes
- `@vitest/coverage-v8` 5.1.1 (exactly the installed Vitest) is the only new dev dependency (MIT, with its helper packages); the lockfile grew by about 147 lines.
- The test-writer found that `npm run test:gate` failed on Node 24 because `node --test scripts/` treats `scripts` as a module path. The script is now `node --test "scripts/*.test.mjs"`.
- The test-writer left `app.config.ts` line 8 and the skip-link handler in `shell.ts` uncovered on purpose; a genuine behaviour test of the skip link was added (`TASK-008.5`), which took coverage from 96.82% to 98.41%.
- Quoting for the filter: `npm test -- --filter "\bTASK-010\.1\b"` works the same in bash and PowerShell; `cmd.exe //c` from Git Bash mangles it (a Git Bash artefact).
- `src/frontend/REPO_CONTEXT.md` was trimmed (versions table collapsed) to stay under the 80-line guard.
- PR size: about 12 files and 550 changed lines (including the lockfile delta of about 150 lines).

## Review round (`/code-review`, 9 findings)
Fixed:
- #1 a report that counts no lines (a mis-scoped `coverageInclude`) now fails instead of reading as 100%;
- #2 and #3 the gate rejects a threshold that would switch it off (empty, 0, negative, infinite, not a number) and an option with no value or followed by another flag;
- #4 the skip-link spec cleans the DOM in `afterEach`, so a failing assertion cannot leak elements;
- #8 a test now checks that the `@vitest/coverage-v8` version equals the installed Vitest version, so an `@angular/build` upgrade that moves Vitest fails a test with a clear message;
- #9 `test:coverage` first removes the old `coverage/` folder (`scripts/clean-coverage.mjs`), so the gate cannot read a stale summary.
Not changed: #5 the skip-link test is a real behaviour test of TASK-008 AC 5 that the coverage report revealed as missing; it keeps its `TASK-008.5` id (traceable to the TASK-008 AC) and is noted in the PR; #6 the 78-line check mirrors the existing `scaffold.spec.ts` guard, and the script text check stays loose (`&& node scripts/coverage-gate.mjs`); #7 reading files relative to `process.cwd()` is the same approach `scaffold.spec.ts` already uses, and `npm test` always runs from `src/frontend`.
After the fixes: 141 of 141 frontend tests and 22 of 22 gate tests pass, the gate passes at 98.41%, build succeeds.
