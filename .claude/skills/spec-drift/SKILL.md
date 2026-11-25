---
name: spec-drift
description: Audit specs, API contracts, data model, ADRs and context files against code and tests with the read-only spec-drift-auditor agent. Changes nothing.
disable-model-invocation: true
argument-hint: "[component | TICKET-ID]"
---

Audit drift for `$ARGUMENTS` (no argument: the `locations` component). Change nothing in this skill.

Preconditions (check first; STOP and tell the owner if any fails)
- A component must be a key in `docs/knowledge-map.yaml`. A ticket ID must have a `tickets/<ID>.md`.

Method
1. Run the `spec-drift-auditor` agent with the argument.
2. Show its report unchanged. Code wins over docs, but the owner decides which side is fixed.
3. STOP. Ask the owner which rows to act on: doc fixes go in a `chore/` PR, code fixes need a ticket, and protected files (root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`) go in a `setup/` PR. Do not fix anything here.
