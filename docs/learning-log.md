# Learning log

Blameless. One row per miss; fix by PR using the narrowest change. Weekly triage. Never put secrets here.

| Date | Signal (ai-miss PR, guard block, brief miss, token spike) | Cause | Fix (file / PR) | Status |
|---|---|---|---|---|
| 2026-11-18 | Phase 0 control test: direct write to main (empty commit) | Expected block | Rejected by ruleset protect-main: GH013, "Changes must be made through a pull request" and required check pr-guard. No change needed | Closed |
| 2026-11-19 | Agent worktree refused on Windows: Claude Code compared `c:...` with `C:/...` as different folders (first real ticket, TASK-001) | Harness bug (issue 91618); our scope guard also had no worktree path handling | `WorktreeCreate` and `WorktreeRemove` hooks build the worktree with an uppercase drive letter; `lib.mjs` strips the worktree prefix and reads the ticket from the main repo; `worktree-apply.mjs` hand-off; tests in `.claude/hooks/tests/guard-tests.mjs` (this PR) | Fix in review |
