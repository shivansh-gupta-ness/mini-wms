---
name: review-ticket
description: Run the review chain on a ticket's change (own-code-review, independent-code-review, security-reviewer, test-critic, ef-query-verifier when persistence changes, and spec-drift-auditor), one after the other, then stop once with all reports, or early if the build or tests fail. Changes no code; saves the stop output to docs/reports/<ID>/code_summary.md and review_findings.md.
disable-model-invocation: true
argument-hint: <TICKET-ID> [base..head]
---

Run the review chain for `$ARGUMENTS`. Change no code in this skill; the only writes are the two report files under Report files. `/start-ticket` follows the Method below after its build; each review also exists as its own skill (`/own-code-review`, `/independent-code-review`, `/security-check`, `/test-critique`, `/ef-verify`, `/spec-drift`).

Preconditions (check first; STOP and tell the owner if any fails)
- `tickets/<ID>.md` exists.
- Either the current branch is `<feat|bug|task>/<ID>-*` and the editing agents' work is carried back, or the ticket is merged and a range (`base..head`) is given.

Method
Run the agents one after the other, never in parallel: two `dotnet` builds at once clash on file locks.
1. **`own-code-review`**: its prompt is the ticket ID, the range if given, and the full text of the plan the owner approved. If the plan is not in this conversation, ask the owner for it, or say that none is available and continue without it.
   **Fail fast**: read its "Commands run". If any build or test command failed (`dotnet build`, `dotnet test`, `npm test`, `npm run build`: a non-zero exit or any failing test), STOP the chain here. Show its report unchanged, list steps 2 to 6 in the summary table as "skipped: build or tests fail", and ask the owner to send the fix to `change-implementer` (code) or `test-writer` (tests), then rerun `/review-ticket`. Do not stop for a "fix first" verdict with other reasons (scope, leftovers, plan gaps): those wait for the one stop at the end. If the commands could not run (blocked or unknown), continue and say so in the summary table.
2. **`independent-code-review`**: a fresh agent (never a fork, never a continued agent) whose whole prompt is the ticket ID and the range if given. Do NOT pass the plan, the brief, the earlier reports, a summary of the change or any hint about where to look.
3. **`security-reviewer`**, for every ticket whatever its risk: a fresh agent (never a fork, never a continued agent) whose whole prompt is the ticket ID and the range if given, like step 2.
4. **`test-critic`**: its prompt is the ticket ID and the range if given.
5. **`ef-query-verifier`**, only if the change touches persistence: any path under `src/backend/MiniWms.Infrastructure/` (queries, stores, `Persistence/`, `Migrations/`). Check with `git diff --name-only --merge-base main` and `git status --short -uall` on the ticket branch, or `git diff --name-only <range>`. Its prompt is the ticket ID. If no such path changed, skip it and say so, naming the check.
6. **`spec-drift-auditor`**: its prompt is the ticket's `component` (from the ticket). It checks whether the docs and the code still agree after the change.

Output
- Each agent's report unchanged, in the order above. Do not argue with, soften or drop findings.
- Then one summary table: agent | verdict | blockers | majors | `must` rows (test-critic) | critical and high findings (security-reviewer) | drift rows (spec-drift-auditor) | run or skipped, and why.

Report files (copies of what the chat shows; never edit, soften or drop anything)
- `docs/reports/<ID>/code_summary.md`: a header (ticket, branch, range, date), the changed files (`git diff --stat --merge-base main` plus untracked files from `git status --short -uall`, or `git diff --stat <range>`), what `change-implementer` and `test-writer` reported if they ran in this conversation, the build and test results from `own-code-review`'s "Commands run", and the AC totals from `docs/reports/<ID>/ac-results.md`.
- `docs/reports/<ID>/review_findings.md`: a header (ticket, range, date, first run or rerun after fixes), each agent's report unchanged in the order above, the summary table, and the stop notices below (security block, drift). If fail fast stopped the chain, it holds the `own-code-review` report and the "skipped" rows.
- Write both on the ticket branch with the ticket active, overwriting earlier copies so they match the latest review. If `docs/reports/<ID>/**` is outside `allowed_paths`, the ticket is `high` or `blocked`, or no ticket is active (a merged ticket reviewed by range), do not write: say so and the owner saves them from the chat.

Then STOP, once
- If `spec-drift-auditor` says "drift found", say first that any drift in the component stops the ticket (owner rule): the docs or the code must be fixed, or the owner records in the ticket Notes a decision to continue, before `/code-review` and the PR.
- Ask the owner which findings to act on. Code fixes go to `change-implementer`, test fixes to `test-writer`, under the active ticket; after fixes the owner reruns `/review-ticket` or a single review skill. Do not fix anything here.
- If `security-reviewer` says "block", say so before the other findings: a critical or high finding must be fixed before `/code-review` and the PR.
- Remind the owner that `/code-review` comes next, and the built-in `/security-review` as a second look if the ticket risk is high.
