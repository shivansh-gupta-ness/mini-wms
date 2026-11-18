# Mini WMS

Mini Warehouse Management System, built spec-first with Claude Code (.NET 10 backend, Angular frontend) to evaluate
a human-owned, AI-powered SDLC. Production-grade quality; never deployed.

- Application code: `src/` (`src/backend`, `src/frontend`)
- Tickets: `tickets/`  ·  Specs, ADRs, knowledge map: `docs/`
- Agent harness: `.claude/` (agents, skills, hooks, settings), checks in `scripts/` and `.github/`
- Evaluation record: `prompt_logs/logs.md` (every prompt, with token usage and model) and `prompt_logs/token_usage.md`
- Plan this follows: `Claude Code Adoption Plan.md`. Original prompts: `dev_prompts/original/`, adapted: `dev_prompts/local/`, run by the skills from `.claude/prompts/`

Start with `REPO_CONTEXT.md`. Workflow: `docs/ticket-workflow.md`.
