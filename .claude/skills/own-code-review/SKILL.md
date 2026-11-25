---
name: own-code-review
description: Self-review of a ticket's change against its approved plan and acceptance criteria, with build and tests, before the owner reviews. Changes nothing.
disable-model-invocation: true
argument-hint: <TICKET-ID>
---

Self-review the change for `$ARGUMENTS`. Change nothing in this skill.

Preconditions (check first; STOP and tell the owner if any fails)
- `tickets/<ID>.md` exists and the current branch is `<feat|bug|task>/<ID>-*`.
- `change-implementer` and `test-writer` have finished and their worktrees were carried back.

Method
1. Run the `own-code-review` agent with the ticket ID and the full text of the plan the owner approved. If the plan is not in this conversation, ask the owner for it, or say that none is available and continue without it.
2. Show its report unchanged.
3. STOP. On "fix first", ask the owner whether to send the punch list to `change-implementer` or `test-writer`. Do not fix anything here.
4. On "pass", remind the owner that `/independent-code-review <ID>` is next.
