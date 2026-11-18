# Mini WMS: spec-driven development roadmap

How to build Mini WMS spec-first with Claude Code, following `Claude Code Adoption Plan.md` (A12 autonomy by risk, A14 SDLC by stage, A17 checklist, the learning loop). Tooling and flow are described in `GUIDE.md`.

**Spec-driven rule:** no code without a spec. Every ticket names the spec sections it implements in `docs:`. If the code must differ from the spec, update the spec in the same PR (code wins, but the spec is never left wrong).

**No deployment.** The app runs only on this machine. There is no release, hosting or deploy step in any phase. Builds and tests run locally; the only thing that runs on GitHub is the existing `pr-guard` check on pull requests.

**Every acceptance criterion gets a logged result.** See "Acceptance-criteria log" below.

**PR limits:** at most 1000 changed lines (tests included) and 15 files, enforced by `pr-guard`.

**Stack:** .NET 10 Web API + EF Core with SQLite (a local file database), Angular frontend. DB Browser for SQLite is used to inspect the database.

## Toolchain check (2026-11-18)

| Tool | Status | Action |
|---|---|---|
| .NET SDK | 10.0.300 | none |
| Node / npm | v24.22.3 / 11.19.1 | none |
| SQLite engine | Not installed separately, and not needed: EF Core's SQLite provider bundles it through NuGet | none |
| `sqlite3` command-line tool | Not installed | Optional. Useful for scripts; install only if wanted |
| DB Browser for SQLite | 3.13.1 installed | Use it to open the `.db` file |
| `dotnet-ef` | 11.0.0, global | Pin a 10.x version in a local tool manifest (`dotnet new tool-manifest`, `dotnet tool install dotnet-ef --version 10.*`) so it matches EF Core 10 |
| Angular CLI | Not installed globally | Use `npx @angular/cli@<version> new` once; afterwards `npx ng` inside `src/frontend` |

## Acceptance-criteria log

Every AC of every ticket ends with a recorded result: **PASS**, **FAIL**, **N/A** (with reason) or **NOT RUN** (with reason). Nothing is reported as passed without test evidence.

| Where | What | Written by |
|---|---|---|
| `docs/reports/<ID>/ac-results.md` | One row per AC: AC number and text, result, test name(s) that prove it, test run output reference, date | Prompt B (backend) / the FE final report, from real test output |
| `docs/reports/ac-log.md` | Running roll-up, one row per ticket: ID, module, ACs total / passed / failed / N/A, gates (mapping, pass, coverage), PR link | The main session at Prompt 5 §7-8, staged for your review; you confirm when the PR merges |
| Ticket `## Delivery` section | Link to `ac-results.md` and the pass count | Prompt 5 §8.e |

A ticket is not done while any AC is FAIL or NOT RUN without an owner decision written in `ac-results.md`. The current prompts already verify ACs (Prompt B) and file reports; writing `ac-results.md` and updating `ac-log.md` was added to the local prompts, skills and plugin copies in Phase 0 (Prompt B step 5, Prompt 5 sections 8.e-8.f, FE section 9.5). Feature tickets then also list `docs/reports/ac-log.md` in `allowed_paths`.

## Phase 0: Finish setup (1 day)
- Merge mini-wms PR #1, commit the pending local changes (including the 1000-line limit) in a `setup/` PR.
- Merge claude-plugins PR #3, tag `v0.3.0`.
- Try `git push origin main` once and confirm it is rejected.
- Add the AC-log step to the adapted prompts (now `.claude/prompts/`) and the run-feature skills (and the plugin copies); create `docs/reports/ac-log.md` with its header row.
- Decide the test frameworks (proposal: xUnit, Angular's default runner) and the coverage tool.

**Exit:** clean `main`, all setup merged, the AC-log format agreed, the push test recorded in `docs/learning-log.md`.

## Phase 1: Product spec (plan stage, 2 to 3 days)
Claude drafts, you decide scope. Files in `docs/specs/`:
- `product.md`: problem, users and roles (admin, warehouse clerk, viewer), modules, out of scope (including deployment), non-functional needs (local only, audit trail, performance targets).
- Modules (one section each, numbered so tickets can cite them): Locations (warehouses, zones, bins), Products (SKU catalogue), Inventory (stock on hand, adjustments, movements), Inbound (receiving, put-away), Outbound (orders, picking, shipping), Auth (login, roles).
- Each module section: user stories with numbered Given / When / Then acceptance criteria. These numbers are what the AC log reports on.

**Exit:** `product.md` merged through a `chore/` PR; every module has numbered stories and ACs.

## Phase 2: Design (design stage, 3 to 5 days)
- ADRs in `docs/adr/`: `ADR-001` solution layering (Api, Application, Domain, Infrastructure, Tests); `ADR-002` EF Core + SQLite, the migration approach, and where the local `.db` file lives (git-ignored); `ADR-003` API conventions (routes, errors as ProblemDetails, validation, paging); `ADR-004` authentication and roles; `ADR-005` testing, coverage and the AC log (gates mapping 100 / pass 100 / coverage 90); `ADR-006` Angular structure (standalone components, feature folders, state, HTTP layer).
- `docs/specs/data-model.md`: entities, keys, relationships, constraints.
- `docs/specs/api/<module>.yaml`: OpenAPI contract per module. This is the hand-off from backend to frontend tickets.
- `docs/specs/ui.md`: screens per module and navigation.
- QA cases per module in `docs/qa/` (or rely on the ticket ACs).

**Exit:** ADRs accepted; data model and contracts for the first two modules (Locations, Products) merged.

## Phase 3: Scaffold (build stage, 2 to 3 days, TASK tickets)
| Ticket | Scope | Risk | Who |
|---|---|---|---|
| TASK-001 | .NET solution per ADR-001, health endpoint, test projects | low | agents |
| TASK-002 | EF Core SQLite, `DbContext`, first migration, local tool manifest with `dotnet-ef` 10 | low | agents |
| TASK-003 | Angular app per ADR-006, routing, HTTP client, test runner | low | agents |
| TASK-004 | Frontend-backend wiring for local runs (dev proxy, CORS) | low | agents |

After the scaffold (you, in a `setup/` PR, because these are protected files):
- Fill the Commands section of `REPO_CONTEXT.md` (build, test, run locally); add `src/backend/REPO_CONTEXT.md` and `src/frontend/REPO_CONTEXT.md`.
- Add knowledge-map components with real globs: `locations` (low), `products` (low), `inventory`, `inbound`, `outbound` (medium), `auth` (high).
- The format and run-tests hooks start working automatically once projects exist; confirm they do.

**Exit:** `dotnet build`, `dotnet test`, `npm test`, `npm run build` pass locally on `main`; the app runs locally against the SQLite file.

## Phase 4: Prove the harness (1 day)
- Run one trivial TASK end to end (`/start-ticket`, approval, an editing agent, PR) and check that its ACs land in `ac-results.md` and `ac-log.md`.
- Check the two open audit items: does the scope guard work inside the agents' worktrees, and how do worktree changes reach your branch? Fix before features.
- Deliberate bad edits (A17): outside `allowed_paths`, a protected path, a secret file, a force push. Each must be blocked and appear in `.claude/state/guard-log.jsonl`.

**Exit:** every control blocked its bad edit; the AC log works; findings logged in `docs/learning-log.md`.

## Phase 5: Feature delivery (most of the time, module by module)
Order: Locations, Products, Inventory, Inbound, Outbound, then Auth (high risk: agents propose, you edit).

Per module:
1. Freeze the module's spec section, data-model part and API contract (merged before tickets start).
2. Create a backend FEAT ticket and a frontend FEAT ticket (frontend `depends_on` the backend). Copy the spec's numbered ACs into each. Slice so each PR stays within 1000 lines and 15 files.
3. Backend: `/start-ticket FEAT-00x`, approve the plan, `/run-feature-be FEAT-00x`, answer each STOP REPORT.
4. Frontend: same with `/run-feature-fe FEAT-00y B`.
5. Check `ac-results.md`: every AC PASS, or a written decision for any other result.
6. Review `git diff --staged`, run `/code-review` (`/security-review` for Auth), open the PR, `pr-guard`, merge, confirm the row in `ac-log.md`.
7. Label `ai-miss` on any PR where the agent got something wrong, with two lines on what happened.

**Exit per module:** all ACs PASS in the log, gates met, reports in `docs/reports/<ID>/`.

## Phase 6: Hardening (2 to 3 days)
- Cross-module flows (receive, put away, pick, ship) as API tests, each linked to the ACs it proves and logged the same way.
- `/security-review` across the codebase; fix findings by ticket.
- Refresh the `REPO_CONTEXT.md` files and the knowledge map against the code.

## Phase 7: Evaluate and close (1 day)
One page in `docs/reports/evaluation.md`:
- AC results from `ac-log.md`: total ACs, passed first time, passed after fixes, failed, N/A.
- Tickets delivered by type and risk; time per ticket.
- Token cost per ticket and per stage (`prompt_logs/token_usage.md`, by branch/ticket).
- Misses: count, causes, where fixed, repeats (should be close to zero).
- What Claude did well and badly, and what to change in the agents, skills and prompts.

## Throughout
- Every prompt goes into `prompt_logs/logs.md`; tokens are added automatically.
- Weekly: 30 minutes on misses, guard blocks and failed ACs; fix each in the one right place (plan A9).
- `prompt_logs/` changes go in periodic `chore/` PRs.
