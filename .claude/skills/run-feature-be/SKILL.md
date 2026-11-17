---
name: run-feature-be
description: Run the backend feature pipeline (Prompts A, 2, 3, 4, B, 5 §7-8) for an approved ticket, using the local-adapted dev prompts. Backend (.NET) work only.
disable-model-invocation: true
argument-hint: <TICKET-ID> [resume-from: A|2|3|4|B|5.8]
---

Run the backend pipeline for `$ARGUMENTS`.

Preconditions (check first; STOP and tell the owner if any fails)
- `/start-ticket` was run for this ID, the owner approved the plan, and `.claude/state/active-ticket.json` names this ticket.
- The ticket's component is a backend component and `allowed_paths` covers `src/backend/**` and `docs/reports/<ID>/**`.
- The ticket `risk` is low or medium. For high or blocked, stop: agents read and propose only.
- `git status` is clean apart from this ticket's work, and the current branch is `<feat|bug|task>/<ID>-*`.

Method
1. Read `dev_prompts/local/run-feature-be.md` (master runner) and follow its "Execution Instructions for the Agent". Fill its Configuration from the ticket:
   `STORY_KEY` = the ID, `SERVICE_PATHS` = the `src/backend/...` projects inside `allowed_paths`, `REPO_CONTEXT_PATHS` = the root and `src/backend` context files,
   `BASE_BRANCH` = `main`. Resume point = the second argument if given, else `A`. The pipeline definition is `dev_prompts/local/per-ticket-feature-prompts.md`.
2. Delegate by stage:
   - Prompts A and B (scope, Target Contract, implementation, AC verification): the `change-implementer` agent, with the pipeline prompt text as its task.
   - Prompts 2, 3 and 4 (unit tests, QA coverage, gates): the `test-writer` agent. Gates (mapping 100, pass 100, coverage 90) are computed from real tool output, never asserted.
   - Prompt 5 §7-8 (story summary, `docs/reports/<ID>/`): the main session.
3. Stop at every STOP condition in the pipeline and print the STOP REPORT; resume only with the owner's answers.
4. Stage only; never commit or push. At the end print the "Pipeline complete" block and remind the owner to review `git diff --staged`, run `/code-review`, and open the PR.
5. Use `node scripts/token-report.mjs` output (or `prompt_logs/token_usage.md`) to quote the token cost of the run in your final message.
