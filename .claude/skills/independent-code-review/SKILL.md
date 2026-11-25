---
name: independent-code-review
description: Fresh-context review of a ticket branch or git range by an agent that never sees the plan or the implementer's reasoning. Changes nothing.
disable-model-invocation: true
argument-hint: <TICKET-ID | base..head | TICKET-ID base..head>
---

Review `$ARGUMENTS` independently. Change nothing in this skill.

Preconditions (check first; STOP and tell the owner if any fails)
- A ticket ID was given and `tickets/<ID>.md` exists, or a git range was given, or both.
- With a ticket ID only, the current branch is `<feat|bug|task>/<ID>-*`. For a merged ticket, ask the owner for a range.

Method
1. Run a fresh `independent-code-review` agent (never a fork, never a continued agent). Its whole prompt is the ticket ID and the range, nothing else. Do NOT pass the plan, the scout's brief, the `/own-code-review` report, a summary of the change or any hint about where to look: that would end its independence.
2. Show its report unchanged. Do not argue with, soften or drop findings.
3. STOP and ask the owner which findings to act on. Do not fix anything here.
4. If the ticket risk is high, remind the owner that `/security-review` is still required.
