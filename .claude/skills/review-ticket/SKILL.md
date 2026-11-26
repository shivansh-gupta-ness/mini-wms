---
name: review-ticket
description: Run the review chain on a ticket's change (own-code-review, test-critic, independent-code-review, ef-query-verifier when persistence changes, and spec-drift-auditor), one after the other, then stop once with all reports. Changes nothing.
disable-model-invocation: true
argument-hint: <TICKET-ID> [base..head]
---

Run the review chain for `$ARGUMENTS`. Change nothing in this skill. `/start-ticket` follows the Method below after its build; each review also exists as its own skill (`/own-code-review`, `/test-critique`, `/independent-code-review`, `/ef-verify`, `/spec-drift`).

Preconditions (check first; STOP and tell the owner if any fails)
- `tickets/<ID>.md` exists.
- Either the current branch is `<feat|bug|task>/<ID>-*` and the editing agents' work is carried back, or the ticket is merged and a range (`base..head`) is given.

Method
Run the agents one after the other, never in parallel: two `dotnet` builds at once clash on file locks.
1. **`own-code-review`**: its prompt is the ticket ID, the range if given, and the full text of the plan the owner approved. If the plan is not in this conversation, ask the owner for it, or say that none is available and continue without it.
2. **`test-critic`**: its prompt is the ticket ID and the range if given.
3. **`independent-code-review`**: a fresh agent (never a fork, never a continued agent) whose whole prompt is the ticket ID and the range if given. Do NOT pass the plan, the brief, the earlier reports, a summary of the change or any hint about where to look.
4. **`ef-query-verifier`**, only if the change touches persistence: any path under `src/backend/MiniWms.Infrastructure/` (queries, stores, `Persistence/`, `Migrations/`). Check with `git diff --name-only --merge-base main` and `git status --short -uall` on the ticket branch, or `git diff --name-only <range>`. Its prompt is the ticket ID. If no such path changed, skip it and say so, naming the check.
5. **`spec-drift-auditor`**: its prompt is the ticket's `component` (from the ticket). It checks whether the docs and the code still agree after the change.

Output
- Each agent's report unchanged, in the order above. Do not argue with, soften or drop findings.
- Then one summary table: agent | verdict | blockers | majors | `must` rows (test-critic) | drift rows (spec-drift-auditor) | run or skipped, and why.

Then STOP, once
- If `spec-drift-auditor` says "drift found", say first that any drift in the component stops the ticket (owner rule): the docs or the code must be fixed, or the owner records in the ticket Notes a decision to continue, before `/code-review` and the PR.
- Ask the owner which findings to act on. Code fixes go to `change-implementer`, test fixes to `test-writer`, under the active ticket; after fixes the owner reruns `/review-ticket` or a single review skill. Do not fix anything here.
- Remind the owner that `/code-review` comes next, and `/security-review` if the ticket risk is high.
