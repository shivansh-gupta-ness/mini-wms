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

All skills live in `.claude/skills/<name>/SKILL.md`. All three below are manual: they run only when you type them (`disable-model-invocation: true`).

| Skill | You type | When to use | What it does |
|---|---|---|---|
| `start-ticket` | `/start-ticket FEAT-001` | First step of every ticket | Checks the ID and that the ticket is `ready`, checks the tree is clean, creates the branch, runs `ticket-scout`, shows the brief and plan, then stops for your approval. After approval it writes `.claude/state/active-ticket.json` and starts the editing agents. |
| `run-feature-be` | `/run-feature-be FEAT-001 [resume stage]` | Backend feature ticket, after approval | Runs `.claude/prompts/run-feature-be.md` (which loads `per-ticket-feature-prompts.md`). Stages A, 2, 3, 4, B, 5 sections 7-8, each ending in a STOP REPORT. |
| `run-feature-fe` | `/run-feature-fe FEAT-002 [A or B]` | Frontend feature ticket, after approval | Runs `.claude/prompts/agent-prompt-fe.md` (Flow A or B). Needs the API contract in `docs/specs/` if the ticket has `depends_on`. |

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
| `ticket-scout` | haiku, low | Read, Grep, Glob | No (read-only) | No | `/start-ticket`, at the start of any ticket |
| `change-implementer` | sonnet, medium | Read, Grep, Glob, Edit, Write, Bash, PowerShell | Yes, code, inside `allowed_paths` | Yes | `/run-feature-*` (BE stages A and B; FE code), or directly on small BUG/TASK tickets, only after plan approval |
| `test-writer` | sonnet, medium | same as above | Yes, test paths only | Yes | `/run-feature-*` (BE stages 2, 3, 4; FE specs), or directly on small tickets |

Rules that apply to the editing agents:
- Work only after the owner approves the plan.
- Stay inside the ticket's `allowed_paths`. If the scope must widen, stop and ask.
- High risk: read and propose only; a person leads the edit. Blocked: no agent edits.
- Stage only. Never commit, push or merge.

Agents are not run automatically by Claude on its own. A skill or you start them.

## 4. Hooks (always on, no command needed)

Configured in `.claude/settings.json`, scripts in `.claude/hooks/`. They also apply inside subagents, which is why the guards live here and not in the plugin.

| Event | Hook | What it does |
|---|---|---|
| Before Edit / Write | `scope-guard` | Always blocks secrets. During a ticket or when an editing agent is the caller: blocks protected paths (`.github/`, `.claude/`, `scripts/`, `CLAUDE.md`, `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`, `prompt_logs/`), blocks risk high or blocked, and enforces `allowed_paths`. Outside a ticket, setup work is allowed. |
| Before Bash / PowerShell | `command-guard` | Blocks force push, push to main, `reset --hard`, `clean -f`, `--no-verify`, recursive deletes of root or parent, network commands (curl, wget and similar), `gh pr merge`, `gh repo delete`. |
| After Edit / Write | `format` | Formats the file. Does nothing until `src/` has a project. |
| When a turn or subagent ends | `token-report` | Updates `prompt_logs/token_usage.md`, `token_usage.json` and the `Tokens:` line under each prompt in `prompt_logs/logs.md`. |
| When a turn or subagent ends | `run-tests` | Runs tests after an editing agent finished. Does nothing until `src/` has projects. |

Blocked actions are logged to `.claude/state/guard-log.jsonl`.

## 5. GitHub-side checks

| Check | When | What it does |
|---|---|---|
| `pr-guard` (required) | On every PR | Branch name, ticket file exists, changed files inside `allowed_paths`, no blocked paths, size at most 1000 lines and 30 files, knowledge-map globs that match nothing, npm lockfile licences. No AI involved. `setup/` and `chore/` branches and the `bootstrap` label are exempt. |
| gitleaks | After `pr-guard` in the same workflow | Secret scan. |
| `protect-main` ruleset | Always | No deletion, no force push, PR required with `pr-guard` passing. 0 required approvals, because GitHub never lets an author approve their own PR. |
| `CODEOWNERS` | On every PR | Requests review from the owner. |

## 6. Plugins

Plugins are a way to reuse the skills and agents in other repositories.

| Item | Detail |
|---|---|
| Marketplace repo | `shivansh-gupta-ness/claude-plugins` (private), marketplace name `mini-wms-plugins` |
| Plugin | `core` |
| Contents | Agents `ticket-scout`, `change-implementer`, `test-writer`; skills `start-ticket`, `run-feature-be`, `run-feature-fe`; the three adapted prompts in `plugins/core/prompts/` (from 0.3.0) |
| Versions | `v0.1.0` tagged. 0.2.0 (editing agents) is PR #1 on `feat/editing-agents`. 0.3.0 (pipeline skills and prompts) is PR #2 on `feat/pipeline-skills`, based on PR #1. Neither is merged or tagged |
| Install in another repo | `/plugin marketplace add shivansh-gupta-ness/claude-plugins`, then `/plugin install core@mini-wms-plugins` |
| Invoked | The same way as repo skills and agents once installed. In this repo the `.claude/` copies are used and win over the plugin copies. |

Limits to know:
- Plugin agents ignore `hooks`, `mcpServers` and `permissionMode`. Each repo must keep its own guards in its `.claude/settings.json`.
- The plugin's `run-feature-*` skills read their prompts from the plugin (`${CLAUDE_PLUGIN_ROOT}/prompts`). Installed plugin skills are namespaced (`/core:run-feature-be`); this repo's own `.claude/skills/` versions use `.claude/prompts/`.
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
| You finish reviewing | `/code-review`, `/security-review` (high risk) |
| You open a PR | `pr-guard`, gitleaks, Code Owner review |
| Any prompt you type | You append it to `prompt_logs/logs.md` (rule in `REPO_CONTEXT.md`) |

## 8. Known gaps

- `/start-ticket`, `/run-feature-be` and `/run-feature-fe` have not run on a real ticket yet. `src/` is not scaffolded.
- `.claude/state/active-ticket.json` is not removed automatically. A stale file keeps the scope guard on; delete it when a ticket is done.
- `prompt_logs/` changes every turn, so the tree is never clean. Commit it in periodic `chore/` PRs, or `/start-ticket` will stop on the dirty tree.
- Hooks need `node` on your PATH.
- Direct push to `main` rejection has not been confirmed. Try `git push origin main` once.
