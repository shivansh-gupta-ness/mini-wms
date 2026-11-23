---
id: TASK-015
type: task
title: Record the first-run result in the AC log
status: ready
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [docs/reports/ac-log.md, docs/reports/README.md, docs/adr/ADR-005-testing-coverage-and-ac-log.md, docs/reports/TASK-015/**, tickets/TASK-015.md]
docs: [docs/adr/ADR-005-testing-coverage-and-ac-log.md, docs/reports/evaluation.md]
parent: 
depends_on: []
---

## Description
The adoption report and the evaluation could not say how many acceptance criteria "passed first time", because `docs/reports/ac-log.md` only keeps the final result. Add one column that records, per ticket, how many criteria passed on the first full test run, and write down how to fill it. This is documentation only: no code, no harness change.

## Acceptance criteria
1. Given `docs/reports/ac-log.md`, when read, then the table has a new last-but-one column "First-run PASS" (the number of criteria that passed on the first full run of the ticket's tests), placed before the PR column, and every existing row has a value: a number where the ticket's `ac-results.md` or the learning log states it, otherwise `n/r` (not recorded). No number is guessed.
2. Given `docs/reports/README.md` and `docs/adr/ADR-005-testing-coverage-and-ac-log.md`, when read, then each says in one or two lines what "first-run PASS" means, that `n/r` marks old rows, and that the ticket's `ac-results.md` should state it (the count and the first failing criteria, if any) from the real output of the first full run.
3. Given the 16 existing rows, when each `n/r` or number is checked against `docs/reports/<ID>/ac-results.md` and `docs/learning-log.md`, then none contradicts them (for example FEAT-001 API criteria that failed on the first run, and TASK-014 AC 3, are not shown as first-run PASS).
4. Given `docs/reports/TASK-015/ac-results.md`, when read, then it records one row per criterion with PASS, FAIL, N/A or NOT RUN from the real check (`git diff` on the three files, a read of the table, and `node docs/specs/api/tools/ac-coverage.mjs` still reporting 19 of 19), and one row for TASK-015 is added to `docs/reports/ac-log.md` using the new column.
5. Given the repository after the changes, when `dotnet build src/backend`, `dotnet test src/backend --no-build` and `npm test` in `src/frontend` run, then they pass as before (this ticket changes no code).

## Out of scope
- The pipeline prompts and skills that write the ac-log row (`.claude/prompts/`, `.claude/skills/`) and the plugin copies in `claude-plugins`: they are protected, so teaching them the new column is a separate `setup/` PR.
- Guessing first-run results for old tickets, or re-running old tickets.
- Any change to code, tests, CI or the harness.

## Notes and links
- Source of the need: recommendation 3 in `docs/reports/adoption-report.md` and "Acceptance criteria" in `docs/reports/evaluation.md`.
- Docs-only ticket: the proof is the command output and the checks of each criterion, as in `docs/reports/TASK-013/ac-results.md`.
