---
name: start-ticket
description: Start work on a local ticket. Creates the branch, runs ticket-scout for a one-page brief, proposes a plan, then stops for the owner's approval.
disable-model-invocation: true
argument-hint: <TICKET-ID, e.g. FEAT-001>
---

Start ticket `$ARGUMENTS`. Follow these steps in order and STOP at step 6. Do not edit any code in this skill.

1. **Validate the ID**: it must look like FEAT-001, BUG-001 or TASK-001 and `tickets/<ID>.md` must exist. If not, say so and stop.
2. **Check the ticket is ready**: `status` is `ready`, and owner, component, risk, `allowed_paths` and acceptance criteria are all filled. If anything is missing, list what is missing and stop. Do not guess values.
3. **Check the working tree**: run `git status --short`. If there are uncommitted changes, tell the owner and stop.
4. **Create the branch** from up-to-date `main`: `<feat|bug|task>/<ID>-<short-slug>` (slug from the ticket title, lowercase, hyphens, max 5 words). Show the branch name. Do not push.
5. **Run the `ticket-scout` agent** with the ID and show its one-page brief unchanged. If the risk is `high` or `blocked`, say that agents may only read and propose, and a person leads the edit.
6. **STOP and ask the owner**: "Approve this plan, edit it, or reject it?" Nothing is edited until the owner approves.

After the owner approves (only then):
- Write `.claude/state/active-ticket.json` with `id`, `risk`, `allowed_paths` (copied from the ticket), so the scope guard enforces them. This file is local state and is git-ignored.
- Run `change-implementer` with the approved plan, then `test-writer` with the acceptance criteria.
- When both finish, list changed files, show the test results, and remind the owner to review the diff, run `/code-review`, and open the PR with the template. Delete `.claude/state/active-ticket.json` when the ticket's work is done.
- If anything went wrong or scope had to widen, add one row to `docs/learning-log.md` proposal for the owner (do not edit it yourself; it is a shared file reviewed by PR).
