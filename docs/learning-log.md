# Learning log

Blameless. One row per miss; fix by PR using the narrowest change. Weekly triage. Never put secrets here.

| Date | Signal (ai-miss PR, guard block, brief miss, token spike) | Cause | Fix (file / PR) | Status |
|---|---|---|---|---|
| 2026-11-18 | Phase 0 control test: direct write to main (empty commit) | Expected block | Rejected by ruleset protect-main: GH013, "Changes must be made through a pull request" and required check pr-guard. No change needed | Closed |
| 2026-11-19 | Agent worktree refused on Windows: Claude Code compared `c:...` with `C:/...` as different folders (first real ticket, TASK-001) | Harness bug (issue 91618); our scope guard also had no worktree path handling | `WorktreeCreate` and `WorktreeRemove` hooks build the worktree with an uppercase drive letter; `lib.mjs` strips the worktree prefix and reads the ticket from the main repo; `worktree-apply.mjs` hand-off; tests in `.claude/hooks/tests/guard-tests.mjs` (this PR) | Fix in review |
| 2026-11-19 | Two hook defects found on BUG-001: (1) the first change-implementer start failed because hook commands used paths relative to the shell folder, which had moved into src/backend (a guard hook that cannot find its script fails open); (2) run-tests tested the main checkout, so an agent saw failures its own worktree did not have | Relative hook paths; run-tests used CLAUDE_PROJECT_DIR (the main repo) instead of the hook input cwd (the agent worktree) | All hook commands now `node "$CLAUDE_PROJECT_DIR/..."` with `"shell": "bash"`; `workRoot(input)` in lib.mjs and run-tests use the worktree; hook-log.jsonl trace; 12 new tests (63 total); verified with a real agent run (this PR) | Fix in review |
