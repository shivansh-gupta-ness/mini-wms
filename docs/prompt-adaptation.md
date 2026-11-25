# How the adopted prompts differ from the originals

The three original feature-pipeline prompts are kept unchanged in `dev_prompts/original/`. The adapted versions the skills run are in `.claude/prompts/` (the same layout as the plugin's `prompts/` folder). The earlier `dev_prompts/local/` folder was removed; `.claude/prompts/` is the only adapted copy in this repo. Each adapted file starts with a `LOCAL ADAPTATION` header that lists its substitutions.

Rule: the originals are never edited. Change the adapted copies in `.claude/prompts/` only, and update this file in the same PR.

## Files

| Original (`dev_prompts/original/`) | Adopted (`.claude/prompts/`) | Run by |
|---|---|---|
| `run-feature-BE-v1 1.md` (master runner) | `run-feature-be.md` | `/run-feature-be` |
| `new-per_ticket_feature_prompts 3.md` (Prompts A, 2, 3, 4, B, 5) | `per-ticket-feature-prompts.md` | loaded by the BE runner |
| `agent-prompt-FE-v1 1.md` (Angular agent) | `agent-prompt-fe.md` | `/run-feature-fe` |

## What did not change

The logic and quality rules are the same:
- Stage order A, 2, 3, 4, B, 5 sections 7-8, and what each stage does.
- Gates: mapping 100, pass 100, coverage 90.
- The STOP protocol and STOP REPORT, resume control (`START_FROM_PROMPT`, `EXISTING_BRANCH`, `RESUME_ANSWERS`).
- Current/Target Contract, the scope-resolution trail, the Context Maintenance rules.
- FE: Flow A and Flow B, the Prime Directives, the "never claim what you did not do" rules.

## What changed

### 1. Systems replaced (all three files)

| Original | Adopted |
|---|---|
| Jira story (`PLN-5814`), read through the Atlassian MCP | Local ticket file `tickets/<ID>.md` (`FEAT-001`, `BUG-001`, `TASK-001`) |
| Confluence specs and ADRs | Files under `docs/` (listed in the ticket's `docs:` field) |
| AgileTest cases linked to the story, or a QA attachment | `docs/qa/<ID>.md`; if absent, the ticket's acceptance criteria; if neither, STOP |
| QA case keys (`PLN-9711`) | QA case IDs (`QA-001`) |
| Jira attachment upload (`attach-to-jira.ps1`, REST call, credentials file) | Reports filed in `docs/reports/<ID>/`, committed in the PR |
| Jira "new tickets" checklist | New ticket files in `tickets/` |
| GitHub MCP tools (`status`, `current_branch`, `create_branch_from`, `switch_branch`) | `git` CLI (`git status --porcelain`, `git rev-parse --abbrev-ref HEAD`, `git switch`) |

### 2. Config values and paths

| Setting | Original | Adopted |
|---|---|---|
| `STORY_KEY` | Jira key, e.g. `PLN-18161` | Ticket ID, e.g. `FEAT-001` |
| FE `JIRA_STORY` | Jira key | `TICKET_ID` |
| `SERVICE_PATHS`, `SCHEMA_FILE`, `REPO_CONTEXT_PATHS`, `PIPELINE_DOC_PATH` | Absolute `C:\Codebase\...` paths | Repo-relative paths under `src/` (and `.claude/prompts/`) |
| FE `UI_CODE_PATH`, `REPO_CONTEXT_PATH` | `<not-provided>` | `src/frontend`, `src/frontend/REPO_CONTEXT.md` |
| `BASE_BRANCH` | `Dev` / `demo/base-branch.ppillay` | `main` |
| `WORK_BRANCH_PREFIX` | `feature/` | `<feat|bug|task>/` by ticket type |
| `ATTACH_SCRIPT_PATH` | Jira upload helper | `REPORTS_PATH = docs/reports/<STORY_KEY>` |
| Story Summary location | `SERVICE_PATHS[0]/docs/<STORY_KEY>/` | `docs/reports/<STORY_KEY>/` |
| Worked examples | `PLN-` keys, `C:\Codebase` paths | `FEAT-` keys, `src/backend` paths (still placeholders) |

### 3. Behaviour changes

| Area | Original | Adopted |
|---|---|---|
| Branch creation (BE Prompt A section 0) | Computes a slug from the Jira summary, asks you to confirm it, creates the branch from `Dev` | Verify only. The branch must match `<feat|bug|task>/<ID>-*`; otherwise STOP and tell the user to run `/start-ticket`. Never creates, renames or switches branches. Steps 0.a to 0.f became 0.a to 0.c. |
| Branch creation (FE) | Not allowed | Same, and now stated in the header |
| Stage 3 QA source | Linked AgileTest issues, then a Jira attachment | QA file, then ticket ACs |
| Stage 5 section 8 | Upload to Jira, then clean up | File reports in `docs/reports/<ID>/`; a `## Delivery` section is appended to the ticket. Section 8.b (upload helper scripts) removed. |
| Scope guard | Not mentioned | New global rule: stay inside the ticket's `allowed_paths`; never edit harness files (`.github/`, `.claude/`, `scripts/`, `CLAUDE.md`, `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`) |
| Commits | Stage only | Stage only; also never push or create branches |
| FE ticket reading | Jira content fetched by MCP, including attachments, comments, linked issues | `tickets/<TICKET_ID>.md`: ACs, `allowed_paths`, `parent` / `depends_on`, and the API contract named in the spec |
| Dirty-tree check | Any uncommitted change stops the run | `prompt_logs/` is ignored (it changes every turn); same in `/start-ticket` and the run-feature skills |
| Stage 4 fix loop | One agent fixes everything | Routed by root cause: test-side fixes to the test-writing agent, code defects and missing validation to the implementing agent; the test-writing agent never edits production code |
| Ticket file and reports | Jira / attachments | `allowed_paths` of feature tickets must include `tickets/<ID>.md` (the `## Delivery` note) and `docs/reports/<ID>/**`; the FEAT template does this by default |
| Context maintenance | Updates the context document after every stage | Only a non-protected context file inside `allowed_paths` (e.g. `src/backend/REPO_CONTEXT.md`) is edited; the root `REPO_CONTEXT.md` is read-only and changes are proposed in the report |
| Hierarchical context | Every listed context document is read in full; no size limit | Only the context chain of the ticket is read (root `REPO_CONTEXT.md`, "Context files"): the parents plus the child files that apply. A file that would pass 500 lines is split into child files in the same PR |
| AC results | Prompt B reports SATISFIED / PARTIAL / NOT SATISFIED in the chat; nothing is filed per AC | Every AC gets PASS / FAIL / N/A / NOT RUN in `docs/reports/<ID>/ac-results.md` (SATISFIED = PASS, PARTIAL and NOT SATISFIED = FAIL), plus one roll-up row in `docs/reports/ac-log.md` (Prompt B step 5, Prompt 5 section 8.e-8.f, FE section 9.5) |
| Who runs it | You paste the file and fill the config by hand | The skill fills the config from the ticket and hands stages to `change-implementer` and `test-writer` (see `GUIDE.md`) |

## Header added to each local file

Every adopted file begins with a `LOCAL ADAPTATION` block that names its original, lists the substitutions above, and restates the harness rules (stay in `allowed_paths`, no network commands, stage only). The local BE files also say they can be run as the `run-feature-be` skill.

## Line counts

| File | Original | Adopted |
|---|---|---|
| Master runner (BE) | 522 | 531 |
| Per-ticket prompts | 564 | 544 |
| Angular agent (FE) | 762 | 768 |

The per-ticket prompts shrank mainly because the Jira upload section and the MCP branch steps were replaced by shorter local steps.

## Not verified

The adaptations have only been checked by reading and searching for leftover Jira, Atlassian and absolute-path references. No pipeline has run on a real ticket yet. The remaining Jira mentions in the local prompts are intentional: the header lines saying what each item replaces, and the report-filing step, which names "the original Jira attachment step".

## Keeping it in sync

- If an original prompt changes, re-apply the substitutions to the local copy; do not copy the original over it.
- If a local prompt changes, update the tables above and the matching skill (`.claude/skills/run-feature-*/SKILL.md`) if the config or stages move.
