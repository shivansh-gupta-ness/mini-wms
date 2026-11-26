# Mini WMS: agents, skills, plugins and hooks

What exists, what each piece does, and when it runs. For the repository rules see `REPO_CONTEXT.md`.

## 1. The big picture

A ticket in `tickets/` moves through a fixed path. A person approves the plan; agents do the edits; guards check every step.

```
tickets/<ID>.md
   |
   v
/start-ticket <ID>          (skill)   ticket-scout reads, you approve the plan
   |
   v
/run-feature-be | /run-feature-fe   (skills, features)   or the agents directly (small BUG/TASK)
   |   change-implementer  -> code
   |   test-writer         -> tests
   v
/own-code-review -> /test-critique -> /independent-code-review (+ /ef-verify when queries change)   (skills, read-only)
   |
   v
You review the diff -> /code-review (+ /security-review on high risk) -> open PR
   |
   v
pr-guard + Code Owner review -> merge (nobody pushes to main)
```

Terms used below:

| Term | Meaning here |
|---|---|
| **Skill** | A slash command (`/name`) that runs written instructions in the main session. Manual only here. |
| **Agent (subagent)** | A helper with its own model, tools and context. A skill or the main session hands work to it. |
| **Plugin** | A package that carries skills and agents so other repos can install them. |
| **Hook** | A script the harness runs automatically on an event (before an edit, when a turn ends). |

## 2. Skills

All skills live in `.claude/skills/<name>/SKILL.md`. All of them are manual: they run only when you type them (`disable-model-invocation: true`).

| Skill | You type | When to use | What it does |
|---|---|---|---|
| `start-ticket` | `/start-ticket FEAT-001` | First step of every ticket | Checks the ID and that the ticket is `ready`, checks the tree is clean, creates the branch, runs `ticket-scout`, shows the brief and plan, then stops for your approval. After approval it writes `.claude/state/active-ticket.json` and starts the editing agents. |
| `run-feature-be` | `/run-feature-be FEAT-001 [resume stage]` | Backend feature ticket, after approval | Runs `.claude/prompts/run-feature-be.md` (which loads `per-ticket-feature-prompts.md`). Stages A, 2, 3, 4, B, 5 sections 7-8, each ending in a STOP REPORT. |
| `run-feature-fe` | `/run-feature-fe FEAT-002 [A or B]` | Frontend feature ticket, after approval | Runs `.claude/prompts/agent-prompt-fe.md` (Flow A or B). Needs the API contract in `docs/specs/` if the ticket has `depends_on`. |
| `own-code-review` | `/own-code-review FEAT-001` | When the editing agents finish | Checks the change against the approved plan and the criteria, scope and leftovers; runs build and tests. |
| `test-critique` | `/test-critique FEAT-001 [range]` | After `test-writer` | Criterion-to-test table, weak assertions, missing cases, changes the tests would not catch, and a ranked list of tests to add or strengthen (recommendations only). |
| `independent-code-review` | `/independent-code-review FEAT-001`, a range, or both | Before the PR, after `own-code-review` | Review by a fresh agent that gets only the ticket ID and range, never the plan or the author's reasoning. |
| `ef-verify` | `/ef-verify [FEAT-001 or path]` | When EF Core queries or persistence change | Reads LINQ, configurations and migrations; reports SQL shape, index, paging and concurrency risks. It does not capture the real SQL. |
| `spec-drift` | `/spec-drift [component or FEAT-001]` | After several merges | Compares specs, contracts, ADRs and context files with code and tests. |

The five review skills change nothing: they show the agent's report and stop.

### Skill inputs and outputs

"Reads" is what the skill or its agent opens; "Produces" is what you get back and every file it writes.

| Skill | Input you give | Reads | Produces |
|---|---|---|---|
| `/start-ticket` | A ticket ID (`FEAT-001`, `BUG-001`, `TASK-001`) | `tickets/<ID>.md` on `main`, `git status` | The branch `<feat\|bug\|task>/<ID>-<slug>` and the scout's one-page brief, then it stops. After your approval: `.claude/state/active-ticket.json`, the agents' changes carried back to the branch (unstaged), `docs/reports/<ID>/ac-results.md` and a row in `docs/reports/ac-log.md`, and the changed files, test results and criterion totals. A learning-log row is proposed, not written |
| `/run-feature-be` | A ticket ID, optionally a stage to resume from (`A`, `2`, `3`, `4`, `B`, `5.8`) | The ticket, `.claude/prompts/run-feature-be.md` and `per-ticket-feature-prompts.md`, the context chain | Code and tests for each stage (staged, not committed), a STOP REPORT at every stop, then `docs/reports/<ID>/` (with `ac-results.md`), the `## Delivery` section in the ticket, the `ac-log.md` row and a "Pipeline complete" block with the criterion totals |
| `/run-feature-fe` | A ticket ID, optionally flow `A` (API integration) or `B` (new feature) | The ticket, `.claude/prompts/agent-prompt-fe.md`, `src/frontend/REPO_CONTEXT.md` and its children, the API contract in `docs/specs/` | Components and specs (staged, not committed), updates to the frontend context file, and a final report including `docs/reports/<ID>/ac-results.md` |
| `/own-code-review` | A ticket ID. The skill also passes the plan you approved (it asks if the plan is not in the conversation) | The ticket, the plan, the diff against `main` including untracked files | The `own-code-review` agent's report (below). No file changes |
| `/test-critique` | A ticket ID, optionally a git range for a merged ticket | The ticket, the diff, the tests, `docs/reports/<ID>/ac-results.md` | The `test-critic` agent's report (below). No file changes |
| `/independent-code-review` | A ticket ID, a git range, or both. Nothing else is passed | The ticket up to `## Notes and links`, the diff without reports, tickets or logs | The `independent-code-review` agent's report (below). No file changes |
| `/ef-verify` | Optionally a ticket ID or a path under `src/backend/MiniWms.Infrastructure/` (default: `Locations/`) | Queries, entity configurations, migrations, ADR-002, `data-model.md` | The `ef-query-verifier` agent's report (below). No file changes |
| `/spec-drift` | Optionally a component from `docs/knowledge-map.yaml` or a ticket ID (default: `locations`) | `docs/specs/` (product, contracts, data model, UI), ADRs, the context files, code and tests | The `spec-drift-auditor` agent's report (below). No file changes |

Preconditions for both pipeline skills: `/start-ticket` was run and approved, the ticket's component and `allowed_paths` match the stack, risk is low or medium, the branch is `<feat|bug|task>/<ID>-*`, the tree is clean.

### Backend stages

| Stage | Name | Runs in |
|---|---|---|
| A | Feature implementation (scope from the ticket) | `change-implementer` |
| 2 | Developer unit tests | `test-writer` |
| 3 | Coverage check against `docs/qa/<ID>.md` (or the ticket ACs) and gap fill | `test-writer` |
| 4 | Run tests, check gates (mapping 100, pass 100, coverage 90), fix gaps | `test-writer` |
| B | Cross-service acceptance-criteria check | `change-implementer` |
| 5 sections 7-8 | Story summary and reports into `docs/reports/<ID>/`, the `## Delivery` note, and the roll-up row in `docs/reports/ac-log.md` | main session |

B also writes `docs/reports/<ID>/ac-results.md`: every acceptance criterion with PASS, FAIL, N/A or NOT RUN and the evidence. The frontend pipeline writes the same file in its final report.

The prompts hold the pipeline logic. The skills only add the preconditions, the config filled in from the ticket, and the agent hand-offs. Originals are in `dev_prompts/original/`. The skills run the adapted prompts in `.claude/prompts/`, which mirrors the plugin's `prompts/` folder. For exactly what was changed and why, see [docs/prompt-adaptation.md](docs/prompt-adaptation.md).

### Built-in skills you also use

| Skill | When |
|---|---|
| `/code-review` | After the agents finish, before opening the PR |
| `/security-review` | On high-risk tickets, before the PR |

## 3. Agents

Defined in `.claude/agents/`.

| Agent | Model / effort | Tools | Edits files? | Isolated worktree? | Invoked by |
|---|---|---|---|---|---|
| `ticket-scout` | opus, medium | Read, Grep, Glob | No (read-only) | No | `/start-ticket`, at the start of any ticket |
| `change-implementer` | sonnet, medium | Read, Grep, Glob, Edit, Write, Bash, PowerShell | Yes, code, inside `allowed_paths` | Yes | `/run-feature-*` (BE stages A and B; FE code), or directly on small BUG/TASK tickets, only after plan approval |
| `test-writer` | sonnet, medium | same as above | Yes, test paths only | Yes | `/run-feature-*` (BE stages 2, 3, 4; FE specs), or directly on small tickets |
| `own-code-review` | opus, high | Read, Grep, Glob, Bash, PowerShell | No (by instruction) | No | `/own-code-review` |
| `independent-code-review` | opus, high | same as above | No (by instruction) | No | `/independent-code-review` |
| `test-critic` | opus, high | same as above | No (by instruction) | No | `/test-critique` |
| `ef-query-verifier` | sonnet, high | same as above | No (by instruction) | No | `/ef-verify` |
| `spec-drift-auditor` | opus, high | same as above | No (by instruction) | No | `/spec-drift` |

### Agent inputs and outputs

| Agent | Input it gets | Output |
|---|---|---|
| `ticket-scout` | A ticket ID | A one-page brief: ticket, goal, criteria copied word for word, relevant code and docs (with any doc and code disagreement), context files, allowed and protected paths, risks and unknowns, a 3 to 7 step plan, and what the risk level allows |
| `change-implementer` | The approved plan and the ticket's criteria | Code changes inside `allowed_paths`, in its own worktree (never committed), and a report: files changed, commands run with results, anything skipped or uncertain, and any scope it wanted but did not have |
| `test-writer` | The criteria and the brief (or a list of tests to add) | Tests in the test paths of `allowed_paths`, in its own worktree, and a report: criterion-to-test mapping, commands run with results, and any gap. It never changes production code |
| `own-code-review` | A ticket ID and the approved plan | Plan and criteria table, scope check, findings (severity, file and line, evidence, fix), commands run, and a verdict: pass or fix first, with a punch list |
| `independent-code-review` | A ticket ID, a git range, or both, nothing else | Summary from the diff only, findings, what was checked and clean, what was not verified, and a verdict: approve, approve with changes, or block |
| `test-critic` | A ticket ID, optionally a git range | Criterion-to-test table, numbered findings, numbered surviving mutations (each `confirmed` or `reasoned`, with the closest test), a ranked list of tests to add or strengthen (`must` or `should`), and a verdict: ready or fix first |
| `ef-query-verifier` | A ticket ID or an Infrastructure path (default: `Locations/`) | Query table (LINQ, expected SQL shape, verdict), findings with the recommended rewrite or index, what needs real SQL capture, and a verdict: clean or fix first |
| `spec-drift-auditor` | A component or a ticket ID (default: `locations`) | Baseline (`ac-coverage.mjs` and the Spectral lint), a drift table (spec claim and code reality, each with file and line, and which side to fix), spec IDs with no test, what was not verified, and a verdict: in sync or drift found |

Every review finding cites a file and line, and an agent writes "unknown" instead of guessing.

The five review agents have no Edit or Write tool but do have a shell, for diffs and tests. Their shell is kept read-only by their instructions, the approval prompts and `command-guard`, not by a hard block.

Rules that apply to the editing agents:
- Work only after the owner approves the plan.
- Stay inside the ticket's `allowed_paths`. If the scope must widen, stop and ask.
- High risk: read and propose only; a person leads the edit. Blocked: no agent edits.
- Stage only. Never commit, push or merge.

Context files: the pipelines and agents load only the `REPO_CONTEXT.md` files that apply to the ticket's paths, and a file over 500 lines is split into child files. The rule is in the root `REPO_CONTEXT.md` ("Context files"). The size and the index are tested; the loading is an instruction.

Agents are not run automatically by Claude on its own. A skill or you start them.

## 4. Hooks (always on, no command needed)

Configured in `.claude/settings.json`, scripts in `.claude/hooks/`. They also apply inside subagents, which is why the guards live here and not in the plugin.

| Event | Hook | What it does |
|---|---|---|
| Before Edit / Write | `scope-guard` | Always blocks secrets. During a ticket or when an editing agent is the caller: blocks protected paths (`.github/`, `.claude/`, `scripts/`, `CLAUDE.md`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`, `prompt_logs/`; the module `src/backend/REPO_CONTEXT.md` and `src/frontend/REPO_CONTEXT.md` are ordinary code files), blocks risk high or blocked, and enforces `allowed_paths`. Outside a ticket, setup work is allowed. |
| Before Bash / PowerShell | `command-guard` | Blocks force push, push to main, `reset --hard`, `clean -f`, `--no-verify`, recursive deletes of root or parent, network commands (curl, wget and similar), `gh pr merge`, `gh repo delete`. |
| After Edit / Write | `format` | Formats the file. |
| When a turn or subagent ends | `token-report` | Updates `prompt_logs/token_usage.md`, `token_usage.json` and the `Tokens:` line under each prompt in `prompt_logs/logs.md`. |
| When a turn or subagent ends | `run-tests` | Runs tests after an editing agent finished. |
| An editing agent starts | `worktree-create` (`WorktreeCreate`) | Creates the agent's isolated worktree under `.claude/worktrees/<name>` on its own branch, from the task branch plus a snapshot of the current uncommitted work. It builds the path with an uppercase drive letter, which avoids a Claude Code bug on Windows where the worktree is refused because `c:` and `C:/` compare as different (issue 91618) |
| An agent's worktree is removed | `worktree-remove` (`WorktreeRemove`) | Removes the worktree and its throwaway branch |

Hand-off from a worktree: `node .claude/hooks/worktree-apply.mjs "<worktree path>" --remove` applies only what the agent changed to the task branch. It stages and commits nothing. `/start-ticket` runs it after each editing agent.
The scope guard understands worktree paths: an agent's edit to `.claude/worktrees/x/src/a.cs` is judged as `src/a.cs`, the active ticket is read from the main repository, and blocks are logged there. Tests: `node .claude/hooks/tests/guard-tests.mjs` (temporary folders only).
Hook commands in `.claude/settings.json` start from `"$CLAUDE_PROJECT_DIR"` and run through bash, so they work whatever folder the session is in. (A path relative to the current folder made the guards silently fail whenever the shell had moved.) `run-tests` tests the folder named by the hook input's `cwd`, which is the agent's worktree, not the main checkout, and writes a line to `.claude/state/hook-log.jsonl` showing which folder it used.

Blocked actions are logged to `.claude/state/guard-log.jsonl`.

## 5. GitHub-side checks

| Check | When | What it does |
|---|---|---|
| `pr-guard` (required) | On every PR | Branch name, ticket file exists on `main` (a PR cannot widen its own ticket), changed files inside `allowed_paths`, no blocked paths, size at most 10000 lines and 60 files, knowledge-map globs that match nothing, npm lockfile licences. No AI involved. The workflow runs the copy of `pr-guard.mjs` that is on `main`, and its actions are pinned to commit SHAs. `setup/` and `chore/` branches and the `bootstrap` label skip the ticket and size rules; only `setup/` branches may change the blocked paths (harness, CI, root context files). |
| gitleaks | After `pr-guard` in the same workflow | Secret scan. |
| `protect-main` ruleset | Always | No deletion, no force push, PR required with `pr-guard` passing. 0 required approvals, because GitHub never lets an author approve their own PR. |
| `CODEOWNERS` | On every PR | Requests review from the owner. |

## 6. Plugins

Plugins are a way to reuse the skills and agents in other repositories.

| Item | Detail |
|---|---|
| Marketplace repo | `shivansh-gupta-ness/claude-plugins` (public on GitHub, checked 2026-11-26), marketplace name `mini-wms-plugins` |
| Plugin | `core` |
| Contents | Agents `ticket-scout`, `change-implementer`, `test-writer`; skills `start-ticket`, `run-feature-be`, `run-feature-fe`; the three adapted prompts in `plugins/core/prompts/` (from 0.3.0). From 0.4.0 also the five review agents and their five skills |
| Versions | `v0.1.0`, `v0.3.0`, `v0.3.1`, `v0.3.2` and `v0.4.0` tagged (0.2.0 was not); plugin PRs #1 to #6 merged. 0.4.0 added the review agents and skills and the context file rule |
| Install in another repo | `/plugin marketplace add shivansh-gupta-ness/claude-plugins`, then `/plugin install core@mini-wms-plugins` |
| Invoked | The same way as repo skills and agents once installed. In this repo the `.claude/` copies are used and win over the plugin copies. |

Limits to know:
- Plugin agents ignore `hooks`, `mcpServers` and `permissionMode`. Each repo must keep its own guards in its `.claude/settings.json`.
- The plugin's `run-feature-*` skills read their prompts from the plugin (`${CLAUDE_PLUGIN_ROOT}/prompts`). Installed plugin skills are namespaced (`/core:run-feature-be`); this repo's own `.claude/skills/` versions use `.claude/prompts/`.
- The review agents name files of this repository (ADRs, `docs/knowledge-map.yaml`, `docs/specs/`, the Locations module), so another repo must adapt them.
- The repo and plugin copies of the agents, skills and prompts are duplicated. Change both, or the plugin drifts. The plugin copies of the prompts use `<PLUGIN_PROMPTS>/` in place of `.claude/prompts/`. In this repo, edit `.claude/prompts/` (the only adapted copy), then sync the plugin.

Planned: the `csharp-lsp` code-intelligence plugin, installed by you through `/plugin`.

## 7. Cheat sheet: what runs when

| Moment | What runs |
|---|---|
| You type `/start-ticket FEAT-001` | `start-ticket` skill, then `ticket-scout` |
| You approve the plan | `.claude/state/active-ticket.json` is written; the scope guard now enforces the ticket |
| You type `/run-feature-be` or `/run-feature-fe` | Pipeline prompt, delegating to `change-implementer` and `test-writer` per stage |
| Any agent edits a file | `scope-guard` before, `format` after |
| Any agent runs a shell command | `command-guard` before |
| A turn or agent ends | `token-report`, `run-tests` |
| The editing agents finish | You type `/own-code-review`, `/test-critique`, `/independent-code-review` (and `/ef-verify` when queries change) |
| You finish reviewing | `/code-review`, `/security-review` (high risk) |
| You open a PR | `pr-guard`, gitleaks, Code Owner review |
| Any prompt you type | You append it to `prompt_logs/logs.md` (rule in `REPO_CONTEXT.md`) |

## 8. Known gaps

- `/start-ticket` has run on TASK-002 to TASK-017, BUG-001 and FEAT-001 (TASK-001 was done by the main session). `/run-feature-be` and `/run-feature-fe` have never run on a ticket: FEAT-001 used the agents directly (owner decision, `docs/reports/FEAT-001/ac-results.md`). The review skills have run on TASK-015 (as a test), TASK-016 and TASK-017.
- The plugin's review agents and skills (0.4.0) have not been installed and run as a plugin.
- `.claude/state/active-ticket.json` is not removed automatically, and `command-guard` blocks agents from deleting it. A stale file keeps the scope guard on; you delete it when a ticket is done.
- `prompt_logs/` changes every turn, so the tree is never clean. `/start-ticket` ignores that folder; commit it in periodic `chore/` PRs. If `git switch main` refuses because of those files, update `main` first with `git fetch origin main:main`.
- Hooks need `node` on your PATH.
- Direct push to `main` rejection has not been confirmed. Try `git push origin main` once.
