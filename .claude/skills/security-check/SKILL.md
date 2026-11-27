---
name: security-check
description: Security review of a ticket branch or git range by the security-reviewer agent, for every risk level. It never sees the plan. Changes nothing.
disable-model-invocation: true
argument-hint: <TICKET-ID | base..head | TICKET-ID base..head>
---

Run a security review of `$ARGUMENTS`. Change nothing in this skill. `/review-ticket` (and `/start-ticket` after its build) runs the same agent in the review chain.

Preconditions (check first; STOP and tell the owner if any fails)
- A ticket ID was given and `tickets/<ID>.md` exists, or a git range was given, or both.
- With a ticket ID only, the current branch is `<feat|bug|task>/<ID>-*`. For a merged ticket, ask the owner for a range.

Method
1. Run a fresh `security-reviewer` agent (never a fork, never a continued agent). Its whole prompt is the ticket ID and the range, nothing else. Do NOT pass the plan, the brief, earlier reports or hints about where to look.
2. Show its report unchanged. Do not argue with, soften or drop findings.
3. STOP and ask the owner which findings to act on. Do not fix anything here.
4. If the ticket risk is high, remind the owner that the built-in `/security-review` is still required as a second look.
