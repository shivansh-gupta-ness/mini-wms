# Claude Code adoption and spec-driven development: final report

Mini WMS evaluation, 2026-11-17 to 2026-11-20. Owner: shivansh-gupta-ness. Companion to `docs/reports/evaluation.md` (the one-page numbers) and `Claude Code Adoption Plan.md` (the plan this run tested).

## 1. Summary

**Verdict: the human-owned, AI-powered model worked on a small, well-specified codebase, and the spec-first flow is what made it work.** In four days one person, with Claude Code inside the harness, delivered 16 tickets (49 merged PRs counting setup and chore work), 133 acceptance criteria (132 PASS, 1 owner-accepted NOT RUN), a Locations module end to end (API and screens), and 692 backend plus 383 frontend tests with coverage gates met (backend 97%, screens 96.52%). No change reached `main` without a PR, and the harness blocked or caught every bad edit that was tested on purpose.

What it does **not** prove: it was one owner, one module, no baseline, no first-pass measurement and no managed-settings or sandbox layer (section 7). It supports "widen to a second squad with the fixes in section 9", not "adopt everywhere".

| Question | Answer | Evidence |
|---|---|---|
| Did the spec-first flow deliver working software? | Yes, for Locations (design for the other five modules) | `docs/reports/FEAT-001/ac-results.md`, `docs/reports/ac-log.md` |
| Did the controls hold? | Yes, with gaps found and closed (M1, M2, L1, L4, L7) | `docs/learning-log.md`, PRs 43, 44, 47, 49 |
| Did agents make mistakes? | Yes, about 15 logged misses, none reached `main` unreviewed; one open repeat was fixed last | `docs/reports/evaluation.md` section "Misses" |
| What did it cost? | 424.5 M tokens (1,792 API calls, 2.30 M output); 61% on the 17 ticket branches | `prompt_logs/token_usage.md` |
| Is the evidence enough to scale? | No: needs a baseline, first-pass logging and a second owner | Section 7 |

## 2. What was tested

The plan (`Claude Code Adoption Plan.md`, summary and A1 to A18) describes a 10-week programme for one squad. This evaluation is a compressed, single-owner rehearsal of the same model on a new repository, so the **harness, flow and controls** are the subject, not team productivity.

| Plan | What was actually done | Why |
|---|---|---|
| Jira and Confluence | Local `tickets/` and `docs/` (`docs/ticket-workflow.md`, `tickets/README.md`) | No access; keeps everything in version control |
| PR limit 400 lines, 15 files | 10,000 lines, 60 files (`scripts/pr-guard.mjs`, learning log) | Owner decision after generated code and scaffolds hit smaller limits |
| WSL2 sandbox, managed settings | Off; project `.claude/settings.json` (bypass mode disabled, deny rules) plus hooks and `pr-guard` | Windows machine, no WSL2 (`REPO_CONTEXT.md`, known gotchas) |
| Pilot scope: scout, implementer, test-writer, `/start-ticket` | Those four, plus `run-feature-be`, `run-feature-fe` and a hand-off script | The feature pipeline was needed for FEAT-001 |
| Six modules | Locations built; Products, Inventory, Inbound, Outbound, Auth design only | Owner scope decision, 2026-11-19 (`docs/sdd-roadmap.md`) |

## 3. Spec-driven development

The roadmap (`docs/sdd-roadmap.md`) ran in eight phases. Each produced a reviewed artefact before the next started.

| Phase | Output | Where |
|---|---|---|
| 0 Setup | Harness, hooks, CI check, ruleset, AC-log format, push test | `.claude/`, `.github/`, `scripts/pr-guard.mjs`, `docs/learning-log.md` (control test) |
| 1 Product spec | Modules, stories, numbered acceptance criteria (LOC-n.n, PRD-n.n) | `docs/specs/product.md` |
| 2 Design | Six ADRs, data model, UI spec, OpenAPI contracts | `docs/adr/ADR-001` to `ADR-006`, `docs/specs/data-model.md`, `docs/specs/ui.md`, `docs/specs/api/*.yaml` |
| 3 Scaffold | 12 TASK tickets: solution, host, EF Core, API building blocks, Angular app, HTTP layer, coverage gates, dev proxy, contract lint | `tickets/TASK-001` to `TASK-012`, `docs/reports/TASK-0xx/` |
| 4 Prove the harness | Deliberate bad edits blocked; first BUG ticket; findings logged | `tickets/BUG-001.md`, `docs/learning-log.md` |
| 5 Feature delivery | FEAT-001 Locations, API (PR 37) and screens (PR 41) | `tickets/FEAT-001.md`, `docs/reports/FEAT-001/ac-results.md` |
| 6 Hardening | Security review, docs-versus-code audit, TASK-013 (docs refresh), TASK-014 (64 KB request limit), harness parts 4a and 4b | `docs/reports/TASK-013/`, `docs/reports/TASK-014/`, PRs 43, 44, 47, 49 |
| 7 Evaluate | `docs/reports/evaluation.md` and this report | `docs/reports/` |

**What the spec-first order bought:**
- Numbered criteria turned into tests one to one. `node docs/specs/api/tools/ac-coverage.mjs` shows 19 of 19 product criteria covered by the contracts, in both directions.
- The contracts were linted with Spectral (`.spectral.yaml`, six rule tests) before any endpoint existed (TASK-012).
- When code and docs disagreed, the rule "code wins, flag it in the brief" turned the Phase 6 audit into a short fix list (TASK-013) instead of a rewrite. The audit found drift in ADRs, specs and context files (timestamps stored as binary UTC ticks, the Development seeder, duplicate code as 400 not 409, CORS order).
- ADRs recorded decisions that agents then followed without re-asking: layering (ADR-001), SQLite and EF Core (ADR-002), problem+json and status codes (ADR-003), testing and the AC log (ADR-005), Angular structure (ADR-006). ADR-004 (auth) is accepted but not implemented.

**Where it cost:** spec drift. Several ADR statements went stale as soon as code diverged, and agent-written docs repeated stale facts (the PR limits). Code-wins plus a docs refresh ticket handled it, but a docs check in `pr-guard` would be cheaper.

## 4. The Claude Code harness

### Agents, skills and prompts
- Agents (`.claude/agents/`): `ticket-scout` (read only brief), `change-implementer` and `test-writer` (editing, isolated worktree, no network tools).
- Skills (`.claude/skills/`): `start-ticket`, `run-feature-be`, `run-feature-fe`. Adapted prompts live in `.claude/prompts/`; originals in `dev_prompts/original/` (never edited; `docs/prompt-adaptation.md` records the changes).
- The same agents and skills ship as the `core` plugin (`claude-plugins` repo, release v0.3.2) so another repository can install them; the hooks cannot ship in a plugin and stay per repository.

### Controls: planned versus built (plan, "Control model")
| Control | Status | Notes and evidence |
|---|---|---|
| Branch protection and required check | Built, one gap | `protect-main` ruleset rejects direct pushes and requires `pr-guard` (learning log, 2026-11-18). Requiring Code Owner review with no bypass is still a GitHub setting for the owner |
| `pr-guard` (plain script, no AI) | Built and hardened | Ticket and allowed paths, blocked paths, size, knowledge-map globs, licences, gitleaks. Since part 4b it runs from the base branch, reads the ticket from the base branch, and only `setup/` PRs may change the harness (PR 47). Actions are pinned to commit SHAs |
| Scope guard | Built | Blocks edits outside `allowed_paths` and protected paths; handles Windows path case, junctions and worktree paths |
| Command guard | Built, hardened | Force push, push to main, destructive deletes, network commands, merge commands; since 4b also shell writes to protected paths and many bypass forms; fails closed if it cannot run |
| Isolated worktrees and hand-off | Built | `worktree-create`, `worktree-apply` (refuses protected and out-of-scope files), `worktree-remove`; removal made reliable in PR 49 |
| Plan approval before edits | Built | `/start-ticket` stops for the owner's approval every time |
| AC log and reports | Built | One `ac-results.md` per ticket and a row in `docs/reports/ac-log.md`, always from real output |
| Learning loop | Built | `docs/learning-log.md` (blameless, one row per miss, fix in the narrowest place) |
| Autonomy by risk | Built, lightly exercised | `docs/autonomy-by-risk.md`, `docs/knowledge-map.yaml`; every ticket here was low risk |
| WSL2 sandbox, managed settings | Not built | See section 7 |

The harness has its own test suite: `node .claude/hooks/tests/guard-tests.mjs`, 133 checks, all passing (80 at the start of Phase 6).

## 5. Results

### Delivery and quality
| Measure | Result | Source |
|---|---|---|
| Tickets | 16: 14 TASK, 1 BUG, 1 FEAT (two PRs), all low risk | `tickets/`, `docs/reports/ac-log.md` |
| Acceptance criteria | 133: 132 PASS, 0 FAIL, 0 N/A, 1 NOT RUN | `docs/reports/ac-log.md` |
| NOT RUN | TASK-011 AC 1 (real proxy forwarding needs a browser and both servers); accepted by the owner 2026-11-20; manual check still open | `docs/reports/TASK-011/ac-results.md` |
| Tests | Backend 692 (Domain 63, Application 199, Api 430), frontend 383, harness 133 | `docs/reports/TASK-014/ac-results.md`, `docs/reports/TASK-013/ac-results.md` |
| Coverage gates (90%) | Backend 97% combined (Domain 100, Application 97.6, Api 95), screens 96.52% | `src/backend/coverage.ps1`, `npm run test:coverage` |
| Builds | `dotnet build` 0 warnings, 0 errors; `npm run build` no warning | each ticket report |
| Contracts | Spectral clean with warnings as failures; 19 of 19 criteria mapped | `docs/specs/api/` |
| Security review | 0 high, 3 medium harness findings; all addressed except the GitHub setting | `docs/learning-log.md` (2026-11-20), PRs 43, 44, 47 |

### Cost
From `prompt_logs/token_usage.md`: 1,792 API calls, 2.30 M output tokens (0.60 M thinking), 424.5 M total of which 413.9 M is cache read. Sonnet 5.5 carried 97%. The 17 ticket branches took 260.2 M (61%); FEAT-001 alone 70.5 M. Harness setup PRs (worktree and hook fixes) cost 29 M, more than most tickets: the harness was the expensive part, the product code was not. Cost in money is not estimated; apply your own rates. Per-ticket numbers are in `docs/reports/evaluation.md`.

### Misses
About 15 logged, by cause: harness bugs on Windows (4), agent output wrong or untested (4), agent runs that stopped (2), rule or doc wording (3), process slips (3). The most useful: FEAT-001's first API test run had 68 failures because the implementer's queries could not be translated by EF Core and nothing had run them; TASK-014's new test found a 500 with an empty body for `Accept: text/html`. Details and fixes: `docs/learning-log.md`.

## 6. Against the plan's own yardsticks

**Four outcomes (plan, "Goals and principles")**
| Outcome | Result |
|---|---|
| Ownership: every ticket and PR has a named human owner | Met. Every ticket names the owner; every PR was opened and merged by the owner; no agent opened or merged anything |
| Evidence: widening rests on measured value against a baseline | Not met. No baseline was taken, so there is no before and after |
| Shared context: maintained, vendor-neutral `REPO_CONTEXT.md` and knowledge map | Met. Three context files kept in step by tickets; a docs audit (TASK-013) corrected drift; the root file and the map are protected |
| Visibility: value, quality, cost and risk in one view | Partly met. `ac-log.md`, token report and the evaluation exist; there is no single weekly dashboard |

**Six principles:** people own outcomes (held), spec before code (held, it was the main success factor), verification grows first (held: coverage gates and the AC log came before feature code), context is a team asset (held), guardrails live in configuration (held, and tested), measure teams not individuals (not tested with one owner).

**Stop criteria (plan)**
| Criterion | Result |
|---|---|
| Out-of-scope change or secret on a protected branch | None merged. The control test (direct push to `main`) was rejected. Scope-guard gaps (shell writes, a PR editing its own ticket or check) were found by review and closed before any exploit |
| Severity 1 or 2 escaped defect from a Claude Code PR | None known. Defects found were caught by tests or review before merge |
| Rework or review wait 25% worse than baseline | Not measurable: no baseline |
| Weekly spend above 120% of budget | Not applicable: no budget was set |
| Legal or security withdraws approval | Not applicable |

## 7. Limits of this evaluation

State these before quoting any number.
1. **One owner, four days, one module, all low risk.** Nothing here shows how medium or high risk work, team review queues or several people behave.
2. **No baseline.** Speed and rework cannot be compared with "before". Time per ticket is not recoverable from the repository (PR open to merge is minutes because review happens before the PR).
3. **"Passed first time" was not recorded per criterion.** Only the final result is logged.
4. **Controls were weaker than the plan on purpose:** no WSL2 sandbox, no managed settings, no Jira, larger PR limits. The hooks run on the developer's machine and can fail; `pr-guard` and branch protection are the real backstops.
5. **Self-review risk:** the same assistant wrote code, tests and docs, and the owner reviewed in large PRs (several above 4,000 lines). Tests found real defects, but review depth was not measured.
6. **Manual checks outstanding:** browser checks for TASK-008, TASK-011 and the FEAT-001 screens, and the real Kestrel 413.
7. **Misses grouping is approximate.** The cause categories were assigned by the assistant from the learning log.

## 8. What worked and what did not

**Worked**
- Scout, brief, approval, then edit: caught problems before code (TASK-004 tests writing the real database, TASK-013 audit findings already fixed).
- Tests as the check on agent output, with the AC log recording real command output.
- Small harness scripts with their own tests; every control was tried with a bad edit.
- Code-wins rule plus a refresh ticket for spec drift.

**Did not**
- Large first attempts: one agent asked for 1,300 lines of tests ran out of turns; one implementer shipped queries that never ran.
- Routing around a block instead of stopping (twice, caught by the harness).
- Windows specifics cost the most tokens: worktree path casing, relative hook paths, a worktree folder that could not be deleted.
- Guard rules that match command text also match the same words inside a commit message; long text now goes in a file.

## 9. Recommendations

Do before widening to a second squad:
1. Turn on Code Owner review and no-bypass in the branch ruleset (owner-only GitHub setting).
2. Take a baseline (cycle time, review wait, rework) for the pilot service before the first real ticket.
3. Log the first-run result per acceptance criterion in `ac-log.md`.
4. Add the sandbox (WSL2) and managed settings the plan calls for; keep the guards as the second layer.
5. Run at least one medium-risk module and one high-risk ticket (the Auth ADR is ready) to test autonomy by risk.
6. Keep the pipeline prompts' lessons: run API tests before accepting an implementer stage that adds queries, split test jobs by area, and ask agents for a status list before long work.
7. Put a docs-versus-code check on a schedule (the TASK-013 audit) rather than at the end.
8. Keep the `core` plugin and the repository copies of agents and skills identical (README in `claude-plugins`).

Owner-only open items: the manual browser checks, the real Kestrel 413 check, branch protection, optional token-log redaction, optional dependency audits (need an explicit yes because they use the network).

## 10. Reference index

| Topic | Document |
|---|---|
| The plan being tested | `Claude Code Adoption Plan.md` |
| Roadmap and scope decision | `docs/sdd-roadmap.md` |
| Numbers and misses (one page) | `docs/reports/evaluation.md` |
| AC roll-up and per-ticket results | `docs/reports/ac-log.md`, `docs/reports/<ID>/ac-results.md`, `docs/reports/README.md` |
| Product spec, data model, UI | `docs/specs/product.md`, `docs/specs/data-model.md`, `docs/specs/ui.md` |
| API contracts and lint | `docs/specs/api/locations.yaml`, `products.yaml`, `README.md`, `.spectral.yaml` |
| Decisions | `docs/adr/ADR-001` to `ADR-006` |
| Workflow and autonomy | `docs/ticket-workflow.md`, `docs/autonomy-by-risk.md`, `docs/knowledge-map.yaml`, `GUIDE.md` |
| Learning loop | `docs/learning-log.md` |
| Token usage | `prompt_logs/token_usage.md`, `prompt_logs/token_usage.json` |
| Prompt history | `prompt_logs/logs.md` (every user prompt) |
| Context for agents | `REPO_CONTEXT.md`, `src/backend/REPO_CONTEXT.md`, `src/frontend/REPO_CONTEXT.md` |
| Harness code and tests | `.claude/`, `.github/workflows/pr-guard.yml`, `scripts/pr-guard.mjs`, `.claude/hooks/tests/guard-tests.mjs` |
| Tickets | `tickets/` |
| Reusable plugin | `claude-plugins` repo, plugin `core`, release v0.3.2 |
| Pull requests | GitHub `shivansh-gupta-ness/mini-wms`, PRs 1 to 49 |
