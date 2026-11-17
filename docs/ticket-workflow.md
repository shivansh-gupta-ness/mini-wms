# Ticket workflow (local, replaces Jira and Confluence)

```
tickets/FEAT-001.md --/start-ticket--> ticket-scout brief --owner approves plan--> change-implementer + test-writer
      (draft->ready)                    (read-only, 1 page)                         (worktree, allowed_paths only)
                                                                                         |
   merge <-- pr-guard + Code Owner review <-- PR (owner explains every change) <-- owner reviews diff, /code-review
```

1. **Create** the ticket from `tickets/_templates/`. Fill component, risk, `allowed_paths`, acceptance criteria. Set `status: ready`.
2. **Start**: `/start-ticket FEAT-001`. The skill creates branch `feat/FEAT-001-<slug>`, runs `ticket-scout`, prints the brief and a plan, then stops.
3. **Approve**: the owner approves, edits or rejects the plan. Nothing is edited before this.
4. **Build**: `change-implementer` edits code, `test-writer` writes tests from the acceptance criteria. Scope and command guards run on every tool call.
5. **Review**: owner reads the diff, runs `/code-review`, then opens the PR using the template. `ai-miss` label if a reviewer had to rewrite agent code.
6. **Merge**: `pr-guard` green plus Code Owner review. Only a person merges.

Specs and decisions live in `docs/` (specs in `docs/specs/`, decisions in `docs/adr/`). Code facts live in the code; if a doc disagrees with the code, the code wins and the doc gets a fix.

## Learning loop
Each miss gets a cause and the narrowest fix: wrong or missing project knowledge -> `REPO_CONTEXT.md`; brief missed code -> knowledge-map entry;
unclear skill step -> `SKILL.md`; agent went too far -> agent `tools` / `model` / `maxTurns`; unsafe action not blocked -> hook or permission rule;
ticket lacked acceptance criteria -> ticket template. Log it in `docs/learning-log.md`, fix by PR.
