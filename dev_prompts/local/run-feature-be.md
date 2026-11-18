# Feature Pipeline — Master Runner Prompt (backend, local adaptation)

> **LOCAL ADAPTATION** of `dev_prompts/original/run-feature-BE-v1 1.md` (the original is unchanged). Owner-approved substitutions:
> ticket system = `tickets/<ID>.md` (FEAT-001 / BUG-001 / TASK-001) instead of Jira; specs/ADRs = `docs/` instead of Confluence;
> QA cases = `docs/qa/<ID>.md` instead of AgileTest; reports = `docs/reports/<ID>/` (committed in the PR) instead of a Jira attachment;
> all paths are repo-relative (code lives under `src/`); git via the `git` CLI instead of GitHub MCP tools; branch `<feat|bug|task>/<ID>-<slug>` from `main`.
> Harness rules still apply: stay inside the ticket's `allowed_paths`; no network commands; stage only, never commit, push or create branches (`/start-ticket` creates the branch; the branch gate only verifies it).
> Run it as the `run-feature-be` skill, or paste this file as a message. The pipeline definition is `dev_prompts/local/per-ticket-feature-prompts.md`.

> **How to use this file:**
> 1. Fill in the `## Configuration` block below for your story.
> 2. If you are **resuming** after a previous STOP, set `START_FROM_PROMPT` to the prompt
>    letter / number where you want to re-enter (e.g. `"2"` or `"B"`) and fill in
>    `RESUME_ANSWERS` with the human answers to any questions the agent asked before it stopped.
> 3. Paste the entire contents of this file as your message to the agent (or use the
>    "attach file" option if your IDE supports it).
> 4. The agent will read the pipeline definition from `PIPELINE_DOC_PATH`, load the
>    repository context documents from `REPO_CONTEXT_PATHS`, apply your configuration, skip
>    all prompts before `START_FROM_PROMPT`, inject `RESUME_ANSWERS` as if the human had
>    already answered them, and then run the remaining prompts in order.
> 5. The agent resolves scope from the context documents rather than exploring the repo, and
>    patches those documents to reflect whatever it changed before staging them.

---

## REPOSITORY CONTEXT PROTOCOL (mandatory — follow exactly)

Each service in `SERVICE_PATHS` has a checked-in repository context document listed in
`REPO_CONTEXT_PATHS`. That document is the **primary source of structural knowledge** for
its service. It already contains the route inventory, DTO and entity shapes, layer layout,
conventions, and the canonical paths for adding new code.

**Read the context. Do not rediscover what it already states.**

### Rule 1 — Load context before anything else

Before any discovery, search, or file read, load every file in `REPO_CONTEXT_PATHS` in full.
This is the only wholesale read permitted in the entire run.

If a service has no entry in `REPO_CONTEXT_PATHS`, or the file is missing, say so explicitly
in the run log and fall back to normal discovery for that service only. Do not silently
proceed as if a context existed.

### Rule 2 — Answer from context first

Resolve as much as possible from the context documents alone:

- which controller, route, or handler owns a behaviour
- request / response / DTO / entity shapes and field names
- which layer and folder a change belongs in ("Where to add things")
- existing conventions the change must follow
- cross-service call boundaries and integration surface
- schema tables, columns, and relationships

If the context answers it, that is your answer. Do not open the file to confirm something
the context already states.

### Rule 3 — Open source only for these reasons

You may read a source file when, and only when:

a. **You are about to modify it.** Never edit a file you have not read.
b. **You must verify an exact contract you are changing.** Reading the current controller
   action or DTO before altering it is required.
c. **The context does not contain what you need.**
d. **The context contradicts itself, or contradicts something you have already read.**

Reason (c) triggers the escalation ladder below. Reasons (a), (b), and (d) are direct reads.

### Rule 4 — Escalation ladder for anything the context lacks

When the context does not answer a question, do **not** fall back to a repo-wide search.
Narrow using the context's own Structure section, then widen one step at a time:

1. **Targeted path.** Use the context's Structure / layout section to identify the single
   most likely directory. `list_files` that directory only.
2. **Sibling paths.** Still not found: extend to the sibling directories in the same layer
   (e.g. the rest of that feature folder, or the peer folders in the same project).
3. **Layer-wide.** Still not found: `search_files` across that one layer or project only,
   with a specific symbol name — never a generic term.
4. **Stop and record.** If three steps do not find it, record a context gap and STOP if the
   answer is load-bearing for scope. Only if it is not load-bearing may you run a wider
   search, and you must log that you did.

Never skip straight to step 3 or 4. Never search across `SERVICE_PATHS` entries you are not
currently working in.

### Rule 5 — Record every context defect

Whenever you open a file because the context was missing, wrong, or ambiguous, record an
entry in the **Context Defect Log**:

`{ service, context file, section, what was wrong or missing, what source says, file:lines }`

This log feeds the context update step and is reported at the end of the run. A context that
sent you to the wrong place is a defect worth fixing even when the story does not touch that
code.

### Rule 6 — Standing discovery rules (unchanged)

These still apply to any read the rules above permit:

- **Scope-filter first.** Restrict all reads to the in-scope service slice. Never open files
  belonging to unrelated services.
- **Ignore non-source paths** entirely: `bin/`, `obj/`, `node_modules/`, `dist/`, `build/`,
  `.git/`, `*.min.*`, lock files, generated clients, migration data, test fixtures, binary assets.
- **Index SQL / schema, don't read it whole.** The context's data section covers most of this;
  where it does not, `search_files` for the specific object name, then read only that body.
- **Read each file once.** Summarize, then refer to it by name. Do not re-read after summarizing.
- **Reference, don't reproduce.** Cite evidence as `relative/path.cs:120-145`. Never paste
  large code blocks into documents.
- **Batch** small related reads; **stop reading** a file the moment you have what you need.
- **Process in deterministic order** and **write docs incrementally** so partial progress
  survives a context reset.

---

## CONTEXT MAINTENANCE PROTOCOL (mandatory — follow exactly)

The repository context is only useful while it is accurate. Every prompt that changes files
must leave the context matching the code.

### When this runs

At the end of **every prompt that modified files** (A, 2, 3, 4, B), after the file edits are
complete and before the staging step.

### Rule 0 — Which context files may be edited (local harness rule)

Only a context file that is **not protected** and lies **inside the ticket's `allowed_paths`** may be edited (normally
`src/backend/REPO_CONTEXT.md`). The root `REPO_CONTEXT.md` is protected: read it, never edit it. If the file you would
update is protected, outside `allowed_paths`, or does not exist yet, do not edit it: list the exact proposed change under
"Context changes proposed" in the STOP REPORT and the Story Summary so the owner can apply it in a separate PR.
Rules 1-6 below apply only to files that Rule 0 allows.

### Rule 1 — Incremental only, never regenerate

Do **not** re-scan the repository. Do **not** regenerate the context document. You already
know exactly what changed: it is the set of files you edited in this prompt.

Drive the update from that change set alone. Nothing else in the document gets touched.

### Rule 2 — What to update, by change type

| You changed | Update in the context |
|---|---|
| Added an endpoint | New row in the controller / routes table: verb, route, auth, request, response, status codes |
| Modified an endpoint | Amend that row only. If the contract changed, update request / response / status codes |
| Removed an endpoint | Delete that row |
| Added a DTO or type | Add it to Contracts and types, with its fields, under the correct grouping |
| Modified a DTO or type | Amend the field list for that type only |
| Added or changed an entity | Update the entity's row: fields, keys, indexes, relationships |
| Added a migration | Append it to the migration list |
| Added a service, repository, or interface | Add it to the relevant inventory table |
| Added an outbound call or config key | Update the Integration surface section. Names and purpose only, never values |
| Established a new convention or hit a footgun | Add a short entry to Conventions. These are the highest-value lines in the document |
| Added a new kind of artefact | Add or amend a recipe in "Where to add things" |

### Rule 3 — Also apply the Context Defect Log

Fold in every entry recorded under Repository Context Protocol Rule 5. Correct the wrong or
missing content and remove the corresponding entry from any Open questions section it
resolves. Fixing a defect you hit is part of the same edit.

### Rule 4 — Header and provenance

Update the document header with a maintenance line rather than a fabricated commit:

`> Last updated <ISO date> by <STORY_KEY> (incremental). Full regeneration: <existing marker>.`

Leave the original generation marker intact. It records when the document was last verified
wholesale, which is different from when it was last patched.

### Rule 5 — Same accuracy bar

Everything written into the context must come from the change you just made. Do not infer,
do not extrapolate to adjacent code you did not touch, and do not record a value for any
secret, token, or connection string.

### Rule 6 — Stage the context file

The context document is shared by every developer and agent working in the repo, so it must
be staged with the story-scope changes.

Add each modified file in `REPO_CONTEXT_PATHS` to the explicit `git add` list, alongside the
source files. List it individually. Never `git add .` or `git add -A`. The no-commit rule
still applies: stage only, never commit.

If a context file was modified but not staged, that is a gate failure — STOP and report.

---

---

## Configuration

```ini
# ── Story identity ─────────────────────────────────────────────────────────────
STORY_KEY              = "FEAT-001"            # ticket ID; reads tickets/<STORY_KEY>.md

# ── Service paths (all microservices that need to change for this story) ───────
# Add or remove entries. Prompt A will only touch files inside these paths.
SERVICE_PATHS          = [
  "src/backend/<ProjectA>",
  "src/backend/<ProjectB>"
]

# ── Repository context documents ───────────────────────────────────────────────
# One entry per SERVICE_PATHS entry, in the SAME ORDER. These are the checked-in
# context documents the agent reads INSTEAD OF exploring the repository.
# The agent reads these first, resolves as much scope as it can from them, and only
# opens source files it must modify or that the context does not cover.
# Leave an entry as "" if that service has no context document yet — the agent will
# fall back to normal discovery for that service and say so in the run log.
REPO_CONTEXT_PATHS     = [
  "REPO_CONTEXT.md",
  "src/backend/REPO_CONTEXT.md"
]

# Update the context document(s) after any file change, incrementally, and stage them.
# Set to false only for a throwaway spike you do not intend to merge.
CONTEXT_UPDATE_ENABLED = true

# ── Schema and test paths ──────────────────────────────────────────────────────
SCHEMA_FILE            = "src/backend/db/schema.sql"   # repo-relative; blank if none yet
TEST_PROJECT_PATH      = ""                   # leave blank — agent auto-detects

# ── Endpoint override (leave blank to let the agent derive from the story) ─────
ENDPOINTS_IN_SCOPE     = ""

# ── Developer technical notes ──────────────────────────────────────────────────
# Paste any implementation details gathered before running the pipeline: design
# decisions, agreed patterns, table/column choices, third-party API contracts,
# performance constraints, gotchas found during spike, etc.
# Prompt A reads these alongside tickets/<STORY_KEY>.md to guide scope and implementation.
# Leave blank if none — the agent derives everything from the ticket + existing code.
TECHNICAL_NOTES        = """

"""

# ── QA test references (leave blank — agent reads docs/qa/<STORY_KEY>.md, else the ticket ACs) ─
QA_TEST_REFERENCES     = ""

# ── Branching ──────────────────────────────────────────────────────────────────
BASE_BRANCH            = "main"
WORK_BRANCH_PREFIX     = "<feat|bug|task>/"   # by ticket type

# ── Quality gates ──────────────────────────────────────────────────────────────
MAPPING_THRESHOLD      = 100
PASS_THRESHOLD         = 100
MIN_COVERAGE           = 90
MAX_FIX_ITERATIONS     = 5
ALLOW_TEST_DELETION    = false
ALLOW_ASSERTION_WEAKENING = false

# ── External coverage gate ─────────────────────────────────────────────────────
EXTERNAL_GATE_ENABLED          = true
SONAR_ANALYZER_FAIL_LEVEL      = "error"
EXTERNAL_GATE_COVERAGE_FORMAT  = "cobertura"
EXTERNAL_GATE_REPORT_OUTPUT    = "TestResults/<STORY_KEY>/external-gate"

# ── Pipeline document (do not change) ─────────────────────────────────────────
PIPELINE_DOC_PATH      = "dev_prompts/local/per-ticket-feature-prompts.md"

# ── Report filing (do not change) ─────────────────────────────────────────────
REPORTS_PATH           = "docs/reports/<STORY_KEY>"   # replaces the report filing helper
```

---

## Resume Control

```ini
# ── Where to start ─────────────────────────────────────────────────────────────
# Set to "A" for a fresh run.
# Set to "2", "3", "4", "B", or "5.8" to resume at that prompt after a previous STOP.
# Set to "5.8" to run only the report-filing step (docs/reports/<STORY_KEY>/).
START_FROM_PROMPT = "A"

# ── Branch override (only used when START_FROM_PROMPT != "A") ──────────────────
# If the branch was already created in a prior session, paste its exact name here.
# The agent will check it out instead of the branch gate (Prompt A §0).
# Leave blank on a fresh run.
EXISTING_BRANCH   = ""

# ── Human answers to inject (only needed when resuming after a STOP) ──────────
# When the agent STOPs and asks a question, paste the question + your answer here
# before re-running. The agent will treat these as already-answered.
#
# Format: one Q/A block per question, separated by blank lines.
# Example:
#   Q: "Is the IsExpress flag stored on the CoreService OrderLine entity or directly on
#       the OrderService order line table?"
#   A: "It lives on the CoreService entity. OrderService reads it via the core API."
RESUME_ANSWERS = """

"""
```

---

## Execution Instructions for the Agent

Read this entire file first, then follow the steps below exactly.

### Step 0 — Load the pipeline definition

Read `PIPELINE_DOC_PATH` (`dev_prompts/local/per-ticket-feature-prompts.md`) in full.
That document defines Prompts A, 2, 3, 4, B, and 5 §7-8.
The configuration values above override the placeholder values in the pipeline document.
The global rules in the pipeline document apply throughout this run.

### Step 0.5 — Load the repository context documents

Read every non-empty entry in `REPO_CONTEXT_PATHS` in full, before any other repository
access. This is the only wholesale read permitted in the run.

Report which contexts loaded, which entries were blank or missing, and for any that are
missing, state that discovery for that service will fall back to normal exploration.

From this point on the Repository Context Protocol governs every read: answer from context
first, open source only for the four permitted reasons, and use the escalation ladder rather
than a repo-wide search when the context falls short.

### Step 1 — Apply resume control

1. Parse `START_FROM_PROMPT`. Valid values: `"A"`, `"2"`, `"3"`, `"4"`, `"B"`, `"5.8"`.
2. If `START_FROM_PROMPT != "A"`:
   - Skip all prompts with an identifier that comes before `START_FROM_PROMPT` in the
     execution order: A → 2 → 3 → 4 → B → 5.8.
   - If `EXISTING_BRANCH` is non-empty, check it out (`git switch <branch>`) instead
     of the branch gate (Prompt A §0). Confirm with `git rev-parse --abbrev-ref HEAD`.
   - If `EXISTING_BRANCH` is blank, assume the branch is already checked out. Confirm with
     `current_branch`; if it does not match `<WORK_BRANCH_PREFIX><STORY_KEY>-*`, STOP and ask
     the human which branch to use.
3. Parse `RESUME_ANSWERS`. For each Q/A pair, treat the answer as the human's confirmed
   response. When the corresponding decision point is reached, use the pre-supplied answer
   instead of stopping to ask.
4. If `START_FROM_PROMPT = "5.8"`, skip directly to Prompt 5 §7-8 (report filing under
   `docs/reports/<STORY_KEY>/`). Assume all previous prompts completed successfully.

### Step 2 — Execute prompts in sequence

Run each prompt in the order defined in `PIPELINE_DOC_PATH`, starting from `START_FROM_PROMPT`.

For each prompt:
- Apply the configuration values from the `## Configuration` block above.
- Apply any relevant `RESUME_ANSWERS` at the first matching decision point.
- If a STOP condition is reached that is **not** covered by `RESUME_ANSWERS`:
  - Emit a **STOP REPORT** (see format below) and halt.
  - Do NOT guess, skip, or work around the ambiguity.
- Before the staging step of any prompt that modified files, run the **Context Maintenance
  Protocol**: patch the affected sections of the relevant `REPO_CONTEXT_PATHS` documents
  incrementally from the change set, fold in the Context Defect Log, and stage the context
  file explicitly alongside the source files.
- On successful completion of each prompt, emit a brief one-paragraph summary before moving on.
  Include one line naming which context sections were updated, or `context: no change` if the
  prompt modified no files.

**Prompt execution order:**

| Prompt | Description | Entry condition | Skipped when |
|---|---|---|---|
| A (Feature Implementation) | Scope + implementation across SERVICE_PATHS | `START_FROM_PROMPT = "A"` | `START_FROM_PROMPT != "A"` |
| 2 (Developer Tests) | Unit tests from Target Contract | `START_FROM_PROMPT <= "2"` | `START_FROM_PROMPT > "2"` |
| 3 (QA Coverage) | QA reference check + gap fill | `START_FROM_PROMPT <= "3"` | `START_FROM_PROMPT > "3"` |
| 4 (Gate Validation) | Real test run + gate computation | `START_FROM_PROMPT <= "4"` | `START_FROM_PROMPT > "4"` |
| B (AC Verification) | AC check + cross-service contracts | `START_FROM_PROMPT <= "B"` | `START_FROM_PROMPT = "5.8"` only |
| 5.8 (Report Filing) | Story summary + file reports in docs/reports/<STORY_KEY>/ | always (last step) | never |

### Step 3 — STOP REPORT format

If the agent must stop for human input, emit this exact structure:

```
## STOP — Human input required

**Stopped at:** Prompt <letter/number>, step <sub-step>
**Story:** <STORY_KEY>
**Branch:** <current branch name>
**Prompts completed so far:** <comma-separated list, e.g. "A, 2">
**Context updated:** <yes / no — and which files, if any, are already staged>
**Context gaps hit:** <count, or "none" — from the Context Defect Log>

### Question(s) requiring human answer

Q1: <exact question text>
Q2: <exact question text — if more than one>

### How to resume

1. Answer the question(s) above.
2. Open `dev_prompts/local/run-feature-be.md` (or re-run the `run-feature-be` skill).
3. Set `START_FROM_PROMPT = "<letter/number that stopped>"`.
4. Set `EXISTING_BRANCH = "<branch name shown above>"`.
5. Paste your answers into `RESUME_ANSWERS` using the Q/A format shown.
6. Re-run this file as a new message to the agent.
```

### Step 4 — Final confirmation

After all prompts (including 5.8) complete successfully, emit:

```
## Pipeline complete

Story    : <STORY_KEY>
Branch   : <branch name>
Services : <SERVICE_PATHS entries>
Ticket   : tickets/<STORY_KEY>.md

Prompts run  : A, 2, 3, 4, B, 5.8
Reports      : <count> files filed under docs/reports/<STORY_KEY>/ (<comma-separated names>)
AC results   : PASS <x>, FAIL <y>, N/A <z>, NOT RUN <w> (docs/reports/<STORY_KEY>/ac-results.md; roll-up row added to docs/reports/ac-log.md)
Tests added  : <count> new [Fact]/[Theory] methods across all services
Gates        : PASS / NOT READY / NEEDS HUMAN INTERVENTION

Context updated : <context file paths, and the sections patched in each>
Context defects : <count fixed> fixed, <count outstanding> left as TODO in the document
Discovery       : <count> source files opened; <count> opened because the context fell short

Changes are STAGED but NOT committed. The staged set includes the updated repository
context document(s) — review those alongside the code, since other developers and agents
depend on them. Review the diff, then commit and push:
  git diff --staged          # review everything the agent staged
  git commit -m "feat(<scope>): <description> [<STORY_KEY>]"
  git push --set-upstream origin <branch name>
```

---

## Quick-start examples

### Fresh run — two services, new story

```ini
STORY_KEY          = "FEAT-001"
SERVICE_PATHS      = [
  "src/backend/CoreService",
  "src/backend/OrderService"
]
REPO_CONTEXT_PATHS = [
  "src/backend/CoreService/REPO_CONTEXT.md",
  "src/backend/OrderService/REPO_CONTEXT.md"
]
SCHEMA_FILE        = "src/backend/db/schema.sql"
START_FROM_PROMPT  = "A"
EXISTING_BRANCH    = ""
RESUME_ANSWERS     = ""
```

### Fresh run — single service

```ini
STORY_KEY          = "FEAT-002"
SERVICE_PATHS      = [
  "src/backend/InventoryService"
]
REPO_CONTEXT_PATHS = [
  "src/backend/InventoryService/REPO_CONTEXT.md"
]
SCHEMA_FILE        = "src/backend/db/schema.sql"
START_FROM_PROMPT  = "A"
EXISTING_BRANCH    = ""
RESUME_ANSWERS     = ""
```

### Fresh run — service with no context document yet

The agent falls back to normal discovery for that service and says so in the run log.
It will still generate no context file on its own; run the context builder separately.

```ini
STORY_KEY          = "FEAT-003"
SERVICE_PATHS      = [
  "src/backend/NewService"
]
REPO_CONTEXT_PATHS = [
  ""
]
START_FROM_PROMPT  = "A"
EXISTING_BRANCH    = ""
RESUME_ANSWERS     = ""
```

### Resume at Prompt 2 after a scope confirmation

```ini
STORY_KEY         = "FEAT-001"
SERVICE_PATHS     = [
  "src/backend/CoreService",
  "src/backend/OrderService"
]
START_FROM_PROMPT = "2"
EXISTING_BRANCH   = "feat/FEAT-001-add-express-shipping-flag"
RESUME_ANSWERS    = """
Q: "Is the IsExpress flag stored on the CoreService OrderLine entity or directly on the
    OrderService order line table?"
A: "It lives on the CoreService entity. OrderService reads it via the core API call."
"""
```

### Resume at Prompt B after gates passed

```ini
STORY_KEY         = "FEAT-001"
START_FROM_PROMPT = "B"
EXISTING_BRANCH   = "feat/FEAT-001-add-express-shipping-flag"
RESUME_ANSWERS    = ""
```

### Resume at Prompt 3 with QA references pre-supplied

```ini
STORY_KEY         = "FEAT-001"
START_FROM_PROMPT = "3"
EXISTING_BRANCH   = "feat/FEAT-001-add-express-shipping-flag"
QA_TEST_REFERENCES = """
QA-001: Verify IsExpress flag is saved correctly when set to true
QA-002: Verify IsExpress flag is saved correctly when set to false
QA-003: Verify GET /api/orderlines/{id} returns IsExpress in the response
QA-004: Verify flag defaults to false when not supplied on create
"""
RESUME_ANSWERS    = ""
```

### Run only the report-filing step (all prompts already done)

```ini
STORY_KEY         = "FEAT-001"
START_FROM_PROMPT = "5.8"
EXISTING_BRANCH   = "feat/FEAT-001-add-express-shipping-flag"
RESUME_ANSWERS    = ""
```

---

*This file is the single entry point for the feature pipeline. Edit only the
`## Configuration` and `## Resume Control` blocks. Do not modify the execution
instructions or the pipeline document.*
