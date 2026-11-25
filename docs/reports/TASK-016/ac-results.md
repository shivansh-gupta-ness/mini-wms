# TASK-016 acceptance-criteria results

Ticket: `tickets/TASK-016.md` (Raise the REPO_CONTEXT line limit to 500). Branch: `task/TASK-016-raise-repo-context-line-limit`. Date: 2026-11-25.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval of a revised plan (the owner added the hierarchical rule, so criteria 1 to 3 were amended and criterion 5 added), then `test-writer` three times (the tests, a fix round after the main session's review, a fix round after `/code-review`), each in an isolated worktree carried back by `worktree-apply.mjs`. `change-implementer` did not run: every changed source file is a test. The numbers below are from the main session's own runs in the main working tree, not from the agent's summaries.

| Command | Result |
|---|---|
| `dotnet build src/backend` | 0 Warning(s), 0 Error(s) |
| `dotnet test src/backend --no-build` | Domain 63, Application 235, Api 450 passed, 0 failed (Application was 199; 36 new cases) |
| `dotnet test src/backend/MiniWms.Application.Tests --no-build --filter "AC=TASK-016.n"` | n=1: 2 passed; n=5: 36 passed; 0 failed |
| `npm test` in `src/frontend` | 29 files, 419 tests passed |
| `npx --offline ng test --no-watch --filter "TASK-016.n"` in `src/frontend` | n=2: 4 passed; n=5: 36 passed |
| `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` | `Combined line coverage: 97.05% (657 of 677 lines); threshold 90%` `GATE PASSED.` |
| `npm run test:coverage` in `src/frontend` | `Line coverage: 96.52% (694 of 719 lines); threshold 90%.` `GATE PASSED.` |
| `npm run test:gate`, `npm run build` in `src/frontend` | 22 passed, 0 failed; bundle generation complete, no warning |
| search for `78 lines`, `80 lines`, `_80_`, `under 80` in the six test files | no match |

| AC # | AC text (short) | Result | Evidence | Date |
|---|---|---|---|---|
| 1 | Backend limit tests accept at most 500 lines, fail above with the split message, no 80 left | PASS | 2 tests tagged `TASK-016.1`: `Backend_REPO_CONTEXT_stays_within_500_lines` and `Backend_repo_context_is_short_and_lists_layout_and_commands`. Live proof: with the limit set to 10 both failed and printed the split message | 2026-11-25 |
| 2 | Four frontend limit tests accept at most 500 lines, fail above with the split message, no 78 or 80 left | PASS | 4 tests titled `TASK-016.2` in `scaffold`, `tooling`, `proxy` and `locations.guards`; the count includes the trailing newline (owner decision), and the titles and messages say so. Live proof: with the limit set to 10 all four failed | 2026-11-25 |
| 3 | Changed tests keep their AC ids; nothing else changes except the criterion 5 tests | PASS (by diff review, no test) | The main session read the diff: the six limit tests change only the limit, message, name or title and the added id | 2026-11-25 |
| 4 | Both test suites and both coverage gates pass; report and log row | PASS (by real runs, no test) | The command table above; this file and the row in `docs/reports/ac-log.md` | 2026-11-25 |
| 5 | Every `REPO_CONTEXT.md` per side: at most 500 lines and listed by its nearest parent; proven on sample trees | PASS | 36 backend and 36 frontend cases tagged `TASK-016.5`: the real tree of that side, valid and invalid sample trees, both sides of the boundary with and without a final newline, accepted and rejected listing forms, skipped folders. Live proofs below | 2026-11-25 |

Totals: PASS 5, FAIL 0, N/A 0, NOT RUN 0

## Live proofs
Copies of the files were taken, one thing was broken in the main working tree, the tests were run, and every file was restored and compared byte for byte.
- All six limits and both helper limits set to 10: 5 backend tests fail (both `TASK-016.1` tests, the real tree, and the two samples at 500 lines) and 7 frontend tests fail (the four `TASK-016.2` tests, the real tree, and the two samples at the limit).
- The accepted-prefix rule removed: exactly the five prefix cases fail on each side.

## Gates
- Mapping: 3 of 5 criteria have tagged tests (60%); criteria 3 and 4 are proven by diff review and real runs, by design.
- Pass: 5 of 5 criteria; all 748 backend tests and 419 frontend tests pass.
- Coverage: 97.05% backend, 96.52% frontend (gate 90%). The ticket changes no production code.

## Review rounds
- Main session: each side also checked the other side's tree, and a listing such as `Foo/REPO_CONTEXT.md.bak` counted. Both fixed.
- `/code-review` (7 findings: 1 important, 6 nits):
  1. Important: a child listed as `./Foo/REPO_CONTEXT.md` or `src/backend/Foo/REPO_CONTEXT.md` was reported as not listed. Fixed on both sides, with accepted and rejected samples.
  2. The real-tree failure text was cut off. Fixed (backend).
  3. The frontend text did not say that the trailing newline counts. Fixed in titles and messages, with boundary samples; the counting is unchanged (owner decision).
  4. The backend walk followed symlinks and junctions. Fixed; NOT covered by a test, because creating a link needs admin rights or Developer Mode on Windows.
  5. The frontend message did not name the parent file. Fixed and asserted.
  6. Prompt log entries were missing. `prompt_logs/` is protected while the ticket is active and is not in this PR; the entries are added after the ticket closes.
  7. The root `REPO_CONTEXT.md` still states 80 and 78. By design: it is protected and follows in the `setup/` PR.

## Notes
- The exact boundary is run only for the criterion 5 helpers. The six single-file tests use the same `at most 500` comparison, but no file of that size exists to run them on.
- A frontend file that ends with a newline may have 499 lines; the backend counts 500.
- The `Stop` hook reported "frontend tests failed" in every agent worktree with no output; worktrees have no `node_modules` (the agent did not install any). The frontend was run in the main working tree instead.
- The third agent worktree folder could not be removed (`EPERM`); the owner deletes it by hand.
- No `REPO_CONTEXT.md` was split or changed. The loading rule for agents goes in the `setup/` PR.
