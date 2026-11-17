---
name: run-feature-fe
description: Run the Angular frontend agent (Flow A API integration or Flow B new feature) for an approved ticket, using the local-adapted FE prompt. Frontend work only.
disable-model-invocation: true
argument-hint: <TICKET-ID> [A|B]
---

Run the frontend agent for `$ARGUMENTS`.

Preconditions (check first; STOP and tell the owner if any fails)
- `/start-ticket` was run for this ID, the owner approved the plan, and `.claude/state/active-ticket.json` names this ticket.
- The ticket's component is a frontend component and `allowed_paths` covers `src/frontend/**` (and `docs/reports/<ID>/**` if a report is wanted).
- The ticket `risk` is low or medium; for high or blocked, stop.
- If the ticket has `depends_on` backend tickets, their API contract must exist in `docs/specs/`. If not, STOP: do not invent a contract.

Method
1. Read `dev_prompts/local/agent-prompt-fe.md` in full. Fill its CONFIGURATION: `UI_CODE_PATH` = `src/frontend`, `REPO_CONTEXT_PATH` = `src/frontend/REPO_CONTEXT.md`
   (or the root file if the folder has none yet), `TICKET_ID` = the ID, `TECHNICAL_NOTES` from the ticket's notes. The second argument selects Flow A or B; if absent, ask the question in its STEP 0.
2. Follow the prompt exactly: Prime Directives, STOP protocol, build and stage rules. Delegate code edits to `change-implementer` and spec files to `test-writer`,
   with the relevant prompt sections as their task. The Angular build and tests must pass before staging.
3. Stage only; never commit, push or create a branch. End with the prompt's final report and remind the owner to review `git diff --staged`, run `/code-review`, and open the PR.
