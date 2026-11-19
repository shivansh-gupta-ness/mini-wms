# Mini WMS: repository context

Vendor-neutral context for people and coding agents. Keep under 200 lines; only what the code cannot tell you.
`CLAUDE.md` is a one-line shim that imports this file. Update this file in the same PR as the change it describes.

## What this is
Mini Warehouse Management System: production-grade in quality, never deployed. Built spec-first to evaluate
Claude Code on .NET 10 and Angular. A named person owns every ticket and every PR.
Scope (owner, 2026-11-19): only Locations is built end to end (API and screens). Products, Inventory, Inbound, Outbound and Auth
are design only (`docs/specs/product.md` section 9, decision 7).

## Layout (all application code is under `src/`)
| Path | Purpose | Status |
|---|---|---|
| `src/backend/` | .NET 10 solution (API, domain, infrastructure, tests); see `src/backend/REPO_CONTEXT.md` | active |
| `src/frontend/` | Angular app (standalone, strict, Material, Vitest); see `src/frontend/REPO_CONTEXT.md` | active |
| `tickets/` | Local tickets (replaces Jira): `FEAT-001`, `BUG-001` and the `TASK-nnn` tickets, plus `_templates/` | active |
| `docs/` | Specs, ADRs, knowledge map (replaces Confluence) | active |
| `dev_prompts/` | Original feature-pipeline prompts in `dev_prompts/original/` (never edited). The adapted prompts the skills run are in `.claude/prompts/` | active |
| `prompt_logs/` | `logs.md` (every user prompt) and `token_usage.md` (token report) | active |
| `.claude/` | Agents, skills, prompts, settings, hooks (the harness) | active |
| `.github/` | CODEOWNERS, PR template, `pr-guard` workflow | active |
| `scripts/` | `pr-guard.mjs`, `token-report.mjs` | active |
| `.spectral.yaml`, `.config/`, `global.json` | Contract lint rules, local tool manifest (`dotnet-ef`, ReportGenerator), SDK pin | active |

## How work flows
1. A ticket file in `tickets/` has an owner, a component, risk, acceptance criteria and `allowed_paths`.
2. `/start-ticket <ID>` runs `ticket-scout`, shows a one-page brief and a plan, and waits for the owner's approval.
3. After approval `change-implementer` and `test-writer` edit in a worktree, only inside `allowed_paths`. Feature tickets run through the
   stack pipeline skills `/run-feature-be` or `/run-feature-fe` (they run the prompts in `.claude/prompts/`), which call those two agents per stage and add the
   gates (mapping 100, pass 100, coverage 90) and STOP rules. Small BUG/TASK tickets use the agents directly. A feature that spans layers or
   repos is one ticket per layer/repo, linked by `parent:` / `depends_on:`; the API contract in `docs/specs/` is the hand-off.
4. The owner reviews the diff, runs `/code-review` (and `/security-review` on high risk), then opens a PR.
5. `pr-guard` (required check) and a Code Owner review must pass before merge. Nobody pushes to `main`.

Branches: `feat/FEAT-001-short-name`, `bug/BUG-001-...`, `task/TASK-001-...`. Doc-only changes use `chore/`; harness, CI and root context file changes use `setup/` (the only branches pr-guard lets change them).
PR limits: at most 10000 changed lines and 60 files.

## Autonomy by risk (`docs/knowledge-map.yaml` sets the value per component)
low and medium: agent edits after plan approval. high: agent reads and proposes, a person leads the edit.
blocked (secrets, CI, harness): no agent edits.

## Commands
- Backend: `dotnet build src/backend` and `dotnet test src/backend`
- Frontend: `npm ci` then `npm test` and `npm run build` in `src/frontend`
- Coverage gates (90% line coverage): `powershell -NoProfile -ExecutionPolicy Bypass -File src/backend/coverage.ps1` and `npm run test:coverage` in `src/frontend`
- Contract lint: `npx @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml"` (rules in `.spectral.yaml`); AC coverage: `node docs/specs/api/tools/ac-coverage.mjs`
- Token report: `node scripts/token-report.mjs` (also runs automatically when a turn ends)

## Rules agents must follow
- Never edit `.github/`, `.claude/`, `scripts/`, `CLAUDE.md`, this root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`, secrets or `.env*`.
  The module files `src/backend/REPO_CONTEXT.md` and `src/frontend/REPO_CONTEXT.md` are part of the code: a ticket updates them (inside its `allowed_paths`; backend file under 80 lines, frontend file at most 78 counting the trailing newline) in the same PR as the change they describe.
- Stay inside the ticket's `allowed_paths`. If scope must widen, stop and ask the owner.
- No network commands. Do not push, merge or force-push; the owner does that.
- Code wins over docs: if a doc disagrees with the code, flag it in the brief, do not silently follow the doc.
- Every user prompt is appended to `prompt_logs/logs.md`.

## Known gotchas
- Windows machine, no WSL2: the Claude Code sandbox is off, so the hooks, deny rules and `pr-guard` are the controls.
- Tools may not be on PATH in agent shells; use `C:\Program Files\dotnet`, `C:\Program Files\nodejs`, `C:\Program Files\Git\cmd`.
