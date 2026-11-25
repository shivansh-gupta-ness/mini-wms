---
name: test-critique
description: Critique a ticket's tests with the read-only test-critic agent (AC mapping, assertion strength, missing cases, surviving mutations). Changes nothing.
disable-model-invocation: true
argument-hint: <TICKET-ID> [base..head]
---

Critique the tests for `$ARGUMENTS`. Change nothing in this skill.

Preconditions (check first; STOP and tell the owner if any fails)
- `tickets/<ID>.md` exists and has acceptance criteria.
- The ticket's tests exist: either the current branch is `<feat|bug|task>/<ID>-*`, or the ticket is already merged. For a merged ticket the owner may give a range.

Method
1. Run the `test-critic` agent with the ticket ID and the range, if one was given.
2. Show its report unchanged.
3. STOP. Ask the owner which tests to add or strengthen, `must` rows first. Doing so is `test-writer` work under the active ticket; do not write tests here.
