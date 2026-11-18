# Learning log

Blameless. One row per miss; fix by PR using the narrowest change. Weekly triage. Never put secrets here.

| Date | Signal (ai-miss PR, guard block, brief miss, token spike) | Cause | Fix (file / PR) | Status |
|---|---|---|---|---|
| 2026-11-18 | Phase 0 control test: direct write to main (empty commit) | Expected block | Rejected by ruleset protect-main: GH013, "Changes must be made through a pull request" and required check pr-guard. No change needed | Closed |
