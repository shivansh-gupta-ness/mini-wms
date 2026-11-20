# Mini WMS

Mini Warehouse Management System, built spec-first with Claude Code (.NET 10 backend, Angular frontend) to evaluate
a human-owned, AI-powered SDLC. Production-grade quality; never deployed.

- Application code: `src/` (`src/backend`, `src/frontend`)
- Tickets: `tickets/`  ·  Specs, ADRs, knowledge map: `docs/`
- Agent harness: `.claude/` (agents, skills, hooks, settings), checks in `scripts/` and `.github/`
- Evaluation record: `prompt_logs/logs.md` (every prompt, with token usage and model) and `prompt_logs/token_usage.md`
- Plan this follows: `Claude Code Adoption Plan.md`. Original prompts: `dev_prompts/original/`, adapted and run by the skills from `.claude/prompts/`

Start with [`REPO_CONTEXT.md`](REPO_CONTEXT.md). Workflow: [`docs/ticket-workflow.md`](docs/ticket-workflow.md).

## Important documents
| Topic | Document |
|---|---|
| Final report (adoption and spec-driven development) | [docs/reports/adoption-report.md](docs/reports/adoption-report.md) ([PDF](docs/reports/adoption-report.pdf)) |
| Evaluation, one page (numbers, tokens, misses) | [docs/reports/evaluation.md](docs/reports/evaluation.md) |
| The plan being tested | [Claude Code Adoption Plan.md](Claude%20Code%20Adoption%20Plan.md) |
| Roadmap and scope decision | [docs/sdd-roadmap.md](docs/sdd-roadmap.md) |
| Acceptance-criteria log and per-ticket results | [docs/reports/ac-log.md](docs/reports/ac-log.md), [docs/reports/README.md](docs/reports/README.md) |
| Product spec, data model, UI | [product.md](docs/specs/product.md), [data-model.md](docs/specs/data-model.md), [ui.md](docs/specs/ui.md) |
| API contracts | [docs/specs/api/](docs/specs/api/README.md) |
| Architecture decisions | [docs/adr/](docs/adr/) (ADR-001 to ADR-006) |
| Autonomy by risk, knowledge map | [docs/autonomy-by-risk.md](docs/autonomy-by-risk.md), [docs/knowledge-map.yaml](docs/knowledge-map.yaml) |
| How to use the harness | [GUIDE.md](GUIDE.md) |
| Learning log (misses and fixes) | [docs/learning-log.md](docs/learning-log.md) |
| Module context for agents | [backend](src/backend/REPO_CONTEXT.md), [frontend](src/frontend/REPO_CONTEXT.md) |
| Reusable plugin (agents and skills) | `claude-plugins` repo, plugin `core` (release v0.3.2) |
