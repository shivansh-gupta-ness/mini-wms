# Per-Ticket Feature Implementation & Validation Prompts (local adaptation)

> **LOCAL ADAPTATION** of `dev_prompts/original/new-per_ticket_feature_prompts 3.md` (the original is unchanged). Owner-approved substitutions:
> ticket system = `tickets/<ID>.md` (FEAT-001 / BUG-001 / TASK-001) instead of Jira; specs/ADRs = `docs/` instead of Confluence;
> QA cases = `docs/qa/<ID>.md` instead of AgileTest; reports = `docs/reports/<ID>/` (committed in the PR) instead of a Jira attachment;
> all paths are repo-relative (code lives under `src/`); git via the `git` CLI instead of GitHub MCP tools; branch `<feat|bug|task>/<ID>-<slug>` from `main`.
> Harness rules still apply: stay inside the ticket's `allowed_paths`; no network commands; stage only, never commit, push or create branches (`/start-ticket` creates the branch; the branch gate only verifies it).
> Worked examples at the bottom are illustrative; project names such as CoreService/OrderService are placeholders.

> A five-step pipeline to run per ticket **when the microservice code is already migrated**
> and you are adding a new feature or modifying existing behaviour on top of it.
>
> **Pipeline order: Prompt A → 2 → 3 → 4 → Prompt B → 5 (§7-8 only)**
>
> | Prompt | Purpose | Replaces |
> |---|---|---|
> | **A** | Multi-service feature implementation — scope from the ticket + existing service contracts | Prompt 1 (Code Conversion) |
> | **2** | Developer unit tests | Unchanged |
> | **3** | QA reference coverage check | Unchanged |
> | **4** | Gate validation (tests + coverage) | Unchanged |
> | **B** | AC verification + cross-service contract consistency | Prompt 5 §1-6 (legacy equivalence) |
> | **5 §7-8** | Story Summary Doc + report filing | Unchanged |
>
> **What is different from the migration pipeline:**
> - No `LEGACY_CODE_PATH` or `ARCHITECTURE_DOC_PATH` — the existing microservice contracts
>   *are* the baseline. Scope comes entirely from the ticket and the code already in
>   `SERVICE_PATHS`.
> - Multiple services can be targeted in one run. Prompt A implements changes across all of
>   them in dependency order; Prompt B verifies the cross-service contracts hold.
> - No legacy-vs-new diff in Prompt B. The oracle is the ticket ACs and the Target Contract
>   agreed in Prompt A.
>
> **Decisions that carry over unchanged from the migration pipeline:**
> - **AD-02:** QA test references are read from `docs/qa/<STORY_KEY>.md` (or `QA_TEST_REFERENCES`). Prompt 3 checks coverage and fills gaps.
> - Gates (mapping / pass / coverage) are computed from real tool output — never asserted
>   by the model (Prompt 4).
> - No self-certification. Back every "pass" with actual command output or a computed number.

---

## Configuration (shared by all prompts)

```ini
STORY_KEY              = "<ticket ID, e.g. FEAT-001; the file is tickets/<STORY_KEY>.md>"

# All microservices that need to change for this story.
# List every service path involved. Prompt A will only touch files inside these paths.
#
# Example — two services:
#   SERVICE_PATHS = [
#     "src/backend/CoreService",
#     "src/backend/OrderService"
#   ]
SERVICE_PATHS          = [
  "<repo-relative path to the first project/service, under src/backend or src/frontend>",
  "<repo-relative path to the second project/service — add or remove entries as needed>"
]

SCHEMA_FILE            = "<repo-relative path to the schema or migrations folder, e.g. src/backend/db/schema.sql>"
TEST_PROJECT_PATH      = "<repo-relative path to the .NET test project; leave blank to auto-detect>"

# Endpoints / methods to implement. Leave blank to derive from the ticket description (Prompt A §1).
ENDPOINTS_IN_SCOPE     = ""

# Developer technical notes — paste here any implementation details gathered BEFORE running
# the pipeline: design decisions, agreed patterns, known constraints, table/column choices,
# third-party API contracts, performance considerations, gotchas found during spike, etc.
# Prompt A reads these first and uses them to guide scope resolution and implementation.
# Leave blank if no pre-gathered notes exist; the agent will derive everything from the ticket + code.
TECHNICAL_NOTES        = """
<paste your technical implementation notes here — free-form, as much detail as you have>
"""

# QA-supplied unit-test references (AD-02). Leave blank — Prompt 3 reads docs/qa/<STORY_KEY>.md (or the ticket ACs).
QA_TEST_REFERENCES     = """
<paste QA test case references here only if they are not yet in docs/qa/<STORY_KEY>.md>
"""

# Branching
BASE_BRANCH            = "main"
WORK_BRANCH_PREFIX     = "<feat|bug|task>/"   # by ticket type: FEAT -> feat/, BUG -> bug/, TASK -> task/
# Branch name is computed as <WORK_BRANCH_PREFIX><STORY_KEY>-<slug>.
# The branch is created by /start-ticket; Prompt A only verifies it matches this pattern.

# Gates (mirror the TRD defaults)
MAPPING_THRESHOLD      = 100      # % QA test references covered by >= 1 UT
PASS_THRESHOLD         = 100      # % unit tests passing
MIN_COVERAGE           = 90       # % code coverage
MAX_FIX_ITERATIONS     = 5
ALLOW_TEST_DELETION    = false
ALLOW_ASSERTION_WEAKENING = false

# External coverage gate (Prompt 4 §5). Local-only — no SonarQube server required.
EXTERNAL_GATE_ENABLED          = true
SONAR_ANALYZER_FAIL_LEVEL      = "error"   # none | warning | error
EXTERNAL_GATE_COVERAGE_FORMAT  = "cobertura"
EXTERNAL_GATE_REPORT_OUTPUT    = "TestResults/<STORY_KEY>/external-gate"
```

## Global rules (apply to every prompt)

- **Bounded context.** Load only the schema, the in-scope story, and relevant files in
  `SERVICE_PATHS`. Do not scan any repository wholesale.
- **Ticket scope.** Every edit must stay inside the ticket's `allowed_paths` (and `docs/reports/<STORY_KEY>/**` once Prompt 5 §8 runs). Never edit `.github/`, `.claude/`, `scripts/`, `CLAUDE.md`, `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`, secrets or `.env*`; if needed, STOP and ask the owner.
- **Scope discipline.** Modify only story-scope files inside `SERVICE_PATHS`. Never touch
  shared framework code or unrelated services. If a cross-service contract change is unavoidable,
  document and request approval before touching the shared file.
- **No self-certification.** Back every "pass" with actual command output or a computed number.
- **No faking green.** Never delete tests, skip tests, or weaken assertions to pass a gate.
- **Flag, don't guess.** If the ticket description or existing contracts are insufficient to
  determine scope, STOP and produce a scope-gap report.
- **Implement in dependency order.** When Service A calls Service B, implement and test B first,
  then A.
- **Traceability.** Tie every change and test to `STORY_KEY` and the relevant AC / QA reference.
- **No commits — stage only.** After all file edits are complete, stage the story-scope changes
  with `git add <files>` (list files individually — never `git add .` or `git add -A`). Do NOT
  run `git commit`. The developer reviews the staged diff (`git diff --staged`) and commits manually.
  This rule applies at the end of every prompt that modifies files (A, 2, 3, 4, B). If git staging
  fails for any reason, STOP and report — do not leave changes unstaged or auto-commit.
- Output is a **draft for human review**. Ready != approved.

---

## PROMPT A — Multi-Service Feature Implementation

```
Implement the feature or change described in STORY_KEY across the services listed in
SERVICE_PATHS. The code is already migrated; this story adds new functionality or modifies
existing behaviour on top of the current codebase. No legacy code lookup is needed.

0. BRANCH GATE (runs BEFORE any file edit). VERIFY ONLY: this prompt never creates a branch.
   `/start-ticket` owns branch creation (owner-approved local change; the original prompt created the branch in 0.d).

   0.a Run `git status --porcelain -- . ":!prompt_logs"` and `git rev-parse --abbrev-ref HEAD` in the repository root.
       `prompt_logs/` changes every turn and is ignored. If the working tree is otherwise dirty, STOP and ask the user how to handle the uncommitted work —
       never auto-stash or auto-commit unrelated changes. If clean, proceed.

   0.b The current branch must match `<feat|bug|task>/<STORY_KEY>-*`
       (prefix: feat for FEAT, bug for BUG, task for TASK; example: `feat/FEAT-001-add-low-stock-flag`).
       If it matches, record it and go to 0.c.
       If it does not match, STOP and tell the user: "Run `/start-ticket <STORY_KEY>` first; it creates and checks out the branch."
       Do NOT create, rename or switch branches, and do not fall back to another branch.

   0.c Record the branch decision in the scope-resolution trail (step 5): BASE_BRANCH, final branch name,
       and that it was created by `/start-ticket`.

1. Determine scope from the ticket and developer technical notes.

   1.a OVERRIDE — if ENDPOINTS_IN_SCOPE is non-empty, use it as authoritative. Skip 1.b/1.c/1.d.

   1.b TECHNICAL_NOTES — if TECHNICAL_NOTES is non-empty, parse it first. Extract any explicit
       identifiers it contains (table names, column names, endpoint routes, service/repository
       method names, DTO shapes, third-party API contracts, design patterns to follow, known
       constraints or gotchas). These notes are developer-supplied implementation intent and take
       precedence over anything inferred from code discovery. Record each note that influences a
       decision in the scope-resolution trail so the human reviewer can audit the reasoning.
       If TECHNICAL_NOTES is blank, skip to 1.c.

   1.c TICKET-DIRECT — read `tickets/<STORY_KEY>.md` in full (description, acceptance criteria, component, risk, allowed_paths) and every
       file listed in its `docs:` field (specs, ADRs, CSV or diagram files under `docs/`). Merge what the ticket and those docs
       say with what TECHNICAL_NOTES already established. If the description, AC, or attachments
       name specific endpoints, routes, controllers, domain entities, repository methods, or DTOs
       explicitly, use them directly. Where the ticket and TECHNICAL_NOTES agree, note the confirmation.
       Where they conflict, flag the conflict for human review — do not silently pick one.

   1.d TICKET + SERVICE CONTRACTS — if 1.c yields no explicit identifiers, map the ticket's workflow
       vocabulary (and any terms surfaced from TECHNICAL_NOTES) against the existing code in
       SERVICE_PATHS:
         - For each service in SERVICE_PATHS, search controllers, domain entities, and repository
           interfaces for names that match the ticket / TECHNICAL_NOTES terms (e.g. "express shipping
           flag", "workstation registration", "order line status update").
         - For each match, read the controller route(s) and the service / repository method
           signature to determine whether the AC can be satisfied by extending an existing
           endpoint or requires a new one.
         - Build a scope table: { service, layer, file, endpoint / method, AC reference }.
         - Identify cross-service dependencies: if Service A needs data from Service B, document
           the dependency direction and the interface that must be updated or extended.
       Record which files you searched and which matched in the scope-resolution trail (step 5).

   1.e STOP — if 1.b + 1.c + 1.d together cannot pin a single, unambiguous set of files and
       endpoints across SERVICE_PATHS, produce a scope-gap report: (i) candidate files / endpoints
       found, (ii) AC items that could not be mapped, (iii) any conflicts between TECHNICAL_NOTES
       and the ticket, (iv) exact questions the human must answer to disambiguate. Stop here and emit
       the report.

2. Extract the CURRENT contract for each in-scope endpoint / method:
   - In each service, read the relevant controller, service layer, repository, and DTO files.
   - Document the Current Contract per endpoint: HTTP verb + route, request shape + validation,
     response shape and values, status codes, error responses, business rules, observable side
     effects, and schema tables / columns used.
   - Summarize each DTO by shape and name. Do not re-read a file after summarizing it.
   - Do not read files outside the scope table from step 1 unless a cross-service dependency
     explicitly demands it.

3. Derive the TARGET contract from the ticket ACs:
   - For each AC, state what changes to the Current Contract are required: new fields, changed
     validation, new endpoint, changed business rule, new cross-service call, etc.
   - For cross-service dependencies, specify the exact interface change required on the downstream
     service and the contract the upstream caller must satisfy after the change.
   - If a database schema change is required (new column, new table, altered constraint), document
     the required DDL. Do NOT auto-apply DDL — STOP and surface it for the tech lead to review
     and apply before Prompt A implementation continues.

4. Implement the changes across SERVICE_PATHS:
   - Implement in dependency order: downstream / shared service first, then callers.
   - Make only the changes required by the ticket ACs. Do not refactor unrelated code, do not
     introduce new abstractions beyond what the story requires, and do not add error handling or
     validation for scenarios that cannot occur in the current call chain.
   - Update or add DTOs, validators, service-layer methods, repository methods, and controllers
     as needed. Follow each project's existing naming conventions and layering patterns.
   - If any file outside SERVICE_PATHS must change (e.g. a shared contract NuGet package),
     STOP and document the required change — do not touch it without explicit approval.

5. Produce:
   - Scope-resolution trail (1.a / 1.b / 1.c path taken, files searched, files matched).
   - Scope table: { service, layer, file, endpoint / method, AC reference }.
   - Current Contract for each in-scope endpoint (before this story).
   - Target Contract for each endpoint (after this story, per ticket ACs).
   - Cross-service dependency map (if applicable): caller → callee, interface before → after.
   - Files changed per service: path, change type (added / modified / deleted), layer, one-line
     purpose — grouped by service.
   - Schema changes (DDL) required, if any — flagged for human application.
   - Assumptions and open conflicts for human review.
   Do NOT assert correctness — that is validated in Prompts 4 and B.

   Then stage all story-scope file changes: `git add <each changed file by path>`. Do NOT
   run `git commit`. The developer will review `git diff --staged` and commit manually.
```

---

## PROMPT 2 — Write the Developer's Initial Unit Tests

```
Write unit tests covering the in-scope endpoint(s) / method(s) across SERVICE_PATHS, based on
the Target Contract from Prompt A and STORY_KEY's acceptance criteria. These are the developer's
own tests; QA's references are checked separately in Prompt 3.

1. Load the Target Contract from Prompt A and the ticket acceptance criteria. For each service in
   SERVICE_PATHS, detect the test framework (xUnit / NUnit / MSTest) and the project's existing
   conventions — do not assume a framework.

2. Write tests for: happy path, request validation failures, error / edge cases, and each business
   rule in the Target Contract. Place them in TEST_PROJECT_PATH following the detected conventions.
   For multi-service stories, write tests in the appropriate test project for each service.

3. Cross-service call paths: where Prompt A identified that Service A calls Service B, write at
   least one test that validates the caller's handling of the callee's response and error contracts.

4. Assertions must validate actual behaviour and values (not just Assert.NotNull). Do not weaken
   assertions. Do not delete or skip existing passing tests.

5. Produce: a list of tests added per service and the specific behaviour each covers.
```

---

## PROMPT 3 — Coverage Check Against QA References, Fill Gaps  [AD-02]

```
QA has supplied the unit-test references for this story (typically as a QA case file `docs/qa/<STORY_KEY>.md`). Check whether
the existing unit tests already cover them, and add only what's missing.

1. Resolve the QA reference set (AD-02). Use the FIRST resolution path that succeeds —
   do NOT skip ahead, and never invent test references.

   1.a OVERRIDE — if QA_TEST_REFERENCES is non-empty, treat it as authoritative. Parse it
       into a list of discrete cases / scenarios. Skip 1.b/1.c.

   1.b QA-CASE FILE — if QA_TEST_REFERENCES is blank, read `docs/qa/<STORY_KEY>.md`. Each case is a numbered row or heading with an ID
       (e.g. QA-001), a title and steps / expected result. The full list of case IDs is the QA reference set. Cite case IDs in the mapping table.

   1.c TICKET FALLBACK — if 1.b finds no file, use the acceptance criteria in `tickets/<STORY_KEY>.md`: parse each numbered
       Given / When / Then as a discrete case. Cite the ticket AC number in the mapping table.

   1.d STOP — if none of the above yields a non-empty case list, do NOT proceed. Report that QA references are not yet available
       and ask to either paste them into QA_TEST_REFERENCES, create `docs/qa/<STORY_KEY>.md`, or add acceptance criteria to the ticket.

2. Inventory the existing unit tests in TEST_PROJECT_PATH across all services in SERVICE_PATHS
   (developer tests from Prompt 2 plus any pre-existing ones).

3. For EACH QA reference, determine whether an existing test already covers it. Build a mapping
   table: QA reference → covering test(s), or GAP. Mark pure-UI cases (layout, tab order, icon
   presence) as N/A — UI only with a one-line justification.

4. For each GAP, write a new unit test that covers it, following the same framework, conventions,
   and assertion-strength rules as Prompt 2. Annotate each new test with the QA reference it
   covers (e.g. `// Covers QA-001`). Do not modify or weaken existing passing tests.

5. Produce: the resolution trail (which of 1.a / 1.b / 1.c yielded the case list, with citation),
   the mapping table (every QA reference → covered / added / N/A-with-reason / uncoverable-with-reason),
   and the list of tests added. Every QA reference must end as covered, added, N/A with reason, or
   explicitly flagged — never silently skipped. This table feeds the mapping gate in Prompt 4.
```

---

## PROMPT 4 — Run Tests, Validate Gates, Fix Gaps (deterministic)

```
Execute the tests, compute the gates from REAL output, and fix gaps until the gates pass or
MAX_FIX_ITERATIONS is reached.

1. Run `dotnet test` with coverage against SERVICE_PATHS / TEST_PROJECT_PATH. Capture raw output,
   structured results, and the coverage report. Use the ACTUAL output — do not summarize from
   reasoning.

2. Compute the gates as numbers and compare to thresholds:
   - QA-reference → UT mapping %   vs MAPPING_THRESHOLD  (N/A entries count as covered but are
     listed separately in the report)
   - Unit-test pass %              vs PASS_THRESHOLD
   - Code coverage %               vs MIN_COVERAGE

3. If any gate fails, enter a fix loop:
   a. Read the actual failure output; classify the root cause (code defect / missing mock /
      wrong test / data-setup / missing validation).
   b. Propose a fix; apply it within story scope only. Route by root cause: test-side causes (wrong test,
      missing mock, data-setup, missing test) are fixed by the test-writing agent; code defects and missing
      validation are fixed by the implementing agent. The test-writing agent never edits production code.
   c. Re-run tests + coverage; log the iteration (failure, root cause, fix, files changed, result).
   d. Repeat until all gates pass or MAX_FIX_ITERATIONS is hit, then STOP and escalate.

4. Never report a gate as passed without the computed number backing it. Honor
   ALLOW_TEST_DELETION=false and ALLOW_ASSERTION_WEAKENING=false.

5. EXTERNAL COVERAGE GATE (run only if EXTERNAL_GATE_ENABLED = true). Adds a second, independent
   toolchain so coverage and static-analysis numbers come from a tool the agent did not write.
   Local-only — no SonarQube / SonarCloud server required.

   5.a One-time bootstrap (idempotent — skip steps that are already done in SERVICE_PATHS[0]):
       - `dotnet new tool-manifest`  (if `.config/dotnet-tools.json` does not exist)
       - `dotnet tool install dotnet-coverage`
       - `dotnet tool install dotnet-reportgenerator-globaltool`
       - Add `SonarAnalyzer.CSharp` with `PrivateAssets="all"` to the test project csproj.
         Pin version centrally in `Directory.Packages.props` if Central Package Management is on.
       - If SONAR_ANALYZER_FAIL_LEVEL = "error", extend `.editorconfig` to elevate SonarAnalyzer
         CRITICAL / BLOCKER rules to compile-time errors. "warning" leaves them as warnings.
         "none" suppresses them entirely.

   5.b Static-analysis run:
       - `dotnet build SERVICE_PATHS[0] /warnaserror /clp:Summary`  (repeat per service)
       - Capture verbatim. Count `Sxxxx` warnings / errors per file.
       - If SONAR_ANALYZER_FAIL_LEVEL = "error" and any S-prefixed errors appear, the build
         fails the gate — enter the fix loop.

   5.c Coverage run (independent of step 1's coverlet run):
       - `dotnet-coverage collect -f EXTERNAL_GATE_COVERAGE_FORMAT \
            -o EXTERNAL_GATE_REPORT_OUTPUT/coverage.cobertura.xml \
            -- dotnet test SERVICE_PATHS[0] --no-build`
         (Run per service or merge reports if the toolchain supports it.)
       - `dotnet tool run reportgenerator \
            -reports:EXTERNAL_GATE_REPORT_OUTPUT/coverage.cobertura.xml \
            -targetdir:EXTERNAL_GATE_REPORT_OUTPUT/report \
            -reporttypes:"MarkdownSummaryGithub;HtmlInline;TextSummary"`
       - Quote the markdown summary at `SummaryGithub.md` (and `Summary.txt`) verbatim —
         do not transcribe percentages from memory.

   5.d Reconcile the two coverage tools. If they differ by > 5 percentage points on the same
       files, investigate before declaring the gate green.

   5.e Append the external-gate evidence to the gate-report markdown: summary table from
       ReportGenerator's `Summary.txt`, SonarAnalyzer issue count by rule id and severity,
       reconciliation table from 5.d, and both raw report paths.

6. Produce: a gate table (required vs actual), the iteration log, files changed, external-gate
   evidence, and a final status: PASS / NOT READY / NEEDS HUMAN INTERVENTION.
```

---

## PROMPT B — Cross-Service AC Verification

```
Verify that the implementation from Prompt A satisfies every ticket AC and that the cross-service
contracts across SERVICE_PATHS are consistent. There is no legacy baseline to diff — the oracle
is the ticket ACs and the Target Contract from Prompt A.

1. For each AC in STORY_KEY:
   - Identify the endpoint(s) / method(s) in SERVICE_PATHS that implement it.
   - Verify the implementation matches the Target Contract from Prompt A: request validation,
     response shape and values, status codes, error responses, business rules, and observable
     side effects.
   - Confirm that at least one unit test from Prompts 2-3 covers the AC and passes.
   - Mark pure-UI ACs (tab order, icon presence, layout) as N/A — UI only with a one-line
     justification; do not flag them as implementation gaps.

2. Cross-service contract consistency:
   For each service-to-service dependency identified in Prompt A step 3, verify:
   a. The caller passes the correct request shape (type, field names, validation rules)
      to the callee.
   b. The callee's response shape matches what the caller expects (field names, types, nullability).
   c. Error contracts are consistent — callee error codes and response envelopes are handled
      correctly by the caller (not silently swallowed, not incorrectly re-thrown).
   d. If integration tests exist that exercise the cross-service call, run them and cite output.
   e. If no integration tests exist, produce a consistency checklist with evidence (file + line
      citations for both sides of each call) and mark items that need human verification.

3. Classify each AC:
   - SATISFIED     — implementation matches the Target Contract AND at least one UT covers it.
   - PARTIAL       — implementation exists but a UT is missing, coverage is thin, or an
                     assertion is too weak to catch a regression.
   - NOT SATISFIED — implementation is missing or incorrect; this is a gap to fix.
   - N/A           — pure-UI concern with no backend surface to assert.

4. Fix loop for NOT SATISFIED and PARTIAL items (bounded by MAX_FIX_ITERATIONS):
   a. Implement the missing or incorrect behaviour within story scope only. If a gap in the
      cross-service contract is found, fix both sides (callee interface + caller handling)
      together in the same iteration.
   b. Re-run tests (`dotnet test --no-build` across SERVICE_PATHS) and re-check the AC.
   c. Log each iteration: what was missing, what was changed, which files were touched, result.
   d. If MAX_FIX_ITERATIONS is reached and items remain NOT SATISFIED, STOP and escalate
      with a list of unresolved items for developer + tech lead review.

5. Produce:
   - AC verification table: every AC → SATISFIED / PARTIAL / NOT SATISFIED / N/A,
     with the test method(s) that cover it and current pass / fail status.
   - Cross-service consistency check: each dependency → consistent / inconsistent
     (with file + line citations for both sides), and any items flagged for human verification.
   - Fix iteration log (gap description, change made, files changed, result per iteration).
   - Final status: PASS / NOT READY / NEEDS HUMAN INTERVENTION.
   - AC RESULTS LOG (local harness rule): every AC ends with a recorded result. Write `docs/reports/STORY_KEY/ac-results.md` from the real test
     output above (never from memory) as a table with one row per AC: `AC #` | `AC text` | `Result` | `Test(s) that prove it` | `Evidence` (test run / report file) | `Date`.
     Result is exactly one of PASS, FAIL, N/A (with the reason) or NOT RUN (with the reason). Map the table above as: SATISFIED -> PASS;
     PARTIAL and NOT SATISFIED -> FAIL; N/A -> N/A; anything not exercised -> NOT RUN. Add a final line `Totals: PASS x, FAIL y, N/A z, NOT RUN w`.
     A ticket is not done while any AC is FAIL or NOT RUN without a written owner decision in this file.
   This output replaces the equivalence report from Prompt 5 §6. Cite it as "AC Verification
   Report (Prompt B)" in the Story Summary produced in Prompt 5 §7.

   Then stage any file changes made during the fix loop: `git add <each changed file by path>`.
   Do NOT run `git commit`. The developer reviews `git diff --staged` and commits manually.
```

---

## PROMPT 5 §7-8 — Story Summary Doc + Report Filing

*(Run after Prompt B completes. Identical to the migration pipeline Prompt 5 §7-8, with the
substitutions noted below.)*

**Substitutions vs the migration pipeline:**
- Replace "Prompt 1 Contract Spec" with "Prompt A Target Contract".
- Replace "legacy→new data-model mapping" with "Current Contract → Target Contract per service".
- Replace "equivalence report (Prompt 5 §6)" with "AC Verification Report (Prompt B)".
- Replace "legacy-vs-new diff" with "cross-service consistency check".

The Story Summary Doc must live at `docs/reports/STORY_KEY/<NN>-story-summary.md`
and contain — at minimum:

**§7.a HEADER** — story key, title, ticket file path, one-line resolved scope, final status.

**§7.b FILES CHANGED** — table per service: Path, Change type, Layer, Purpose.
Group by service first, then by layer within each service. Derive from
`git diff --name-status STORY_BASELINE...HEAD`.

**§7.c QA COVERAGE** — Prompt 3 mapping table verbatim. End with roll-up:
`Covered: X / Y`, `N/A (with reason): Z / Y`.

**§7.d UNIT-TEST INVENTORY** — every test method added by this story (per service, per file,
per method), the behaviour it asserts (one line). Include `[Theory]` row counts.

**§7.e COVERAGE** — quote verbatim from both Prompt 4 coverage reports (coverlet + dotnet-coverage
/ ReportGenerator). Per-file table for story-scope files only. Cite both raw cobertura paths.

**§7.f STATIC ANALYSIS** — SonarAnalyzer finding count for story-scope files (target = 0).
Quote Prompt 4 §5.b table filtered to story-scope files. List each finding with rule id, file,
and line if non-zero.

**§7.g AC VERIFICATION** — summary of Prompt B output: `satisfied: X`, `partial: Y`,
`not-satisfied-fixed: Z`, `n/a: W`, `unresolved: V`. List any unresolved items inline.
Cross-service consistency check result (consistent / inconsistent, items flagged).

**§7.h FOLLOW-UPS / OUT-OF-SCOPE** — every "deferred to a follow-up" note any earlier prompt
produced, as a checklist a tech lead can turn into new ticket files.

**§7.i REPRODUCIBILITY** — single copy-pasteable PowerShell block:
`dotnet build /warnaserror`, `dotnet test --collect:"XPlat Code Coverage"`, and the
external-gate commands from Prompt 4 §5.c. One block per service if they are built separately.

After producing the summary, file the reports (local replacement for the original Jira attachment step — reports stay in the repository and are reviewed in the PR):

**§8.a** Create `docs/reports/<STORY_KEY>/`. Write the Story Summary Doc there as `<NN>-story-summary.md`, together with the other prompt outputs
  (scope-resolution trail, QA mapping table, AC Verification Report) as separate numbered `.md` files.

**§8.b** Copy `TestResults/<STORY_KEY>/*.ps1` helper scripts to `docs/reports/<STORY_KEY>/scripts/`. Do not copy raw coverage output
  (`TestResults/` is git-ignored); quote the numbers in the summary instead.

**§8.c** Verify: list `docs/reports/<STORY_KEY>/` and confirm every file named in the summary exists and is non-empty. Retry once; flag on second failure.

**§8.d** Stage `docs/reports/<STORY_KEY>/` explicitly with the source changes (list files individually; never `git add .`). Do NOT delete the local reports.

**§8.e** Append a `## Delivery` section to `tickets/<STORY_KEY>.md`: the report file list, a link to `docs/reports/<STORY_KEY>/ac-results.md` with its Totals line, the gate status and the PR checklist. Stage the ticket file.

**§8.f** Append one row to the roll-up `docs/reports/ac-log.md` (create it with the header below if it is missing): `Date | Ticket | Module | ACs | PASS | FAIL | N/A | NOT RUN | Mapping % | Pass % | Coverage % | PR`. Counts come from `ac-results.md`; the gate numbers come from Prompt 4; leave PR blank (the owner fills it when the PR is open). Stage the file. If `ac-log.md` is not in the ticket's `allowed_paths`, STOP and ask the owner to add it.
`| Date | Ticket | Module | ACs | PASS | FAIL | N/A | NOT RUN | Mapping % | Pass % | Coverage % | PR |`
  The ticket's `allowed_paths` must include `docs/reports/<STORY_KEY>/**` and `docs/reports/ac-log.md`; if it does not, STOP and ask the owner to add it.

**Why the reports are in the repo:** there is no external ticket system. The ticket file and `docs/reports/<STORY_KEY>/` are the single
source of truth for ticket-level artefacts, reviewed with the code in the PR.

---

## How the steps chain

1. **Prompt A** — Scope from the ticket ACs + existing service contracts. Branch created. Target Contract
   and cross-service dependency map produced. Changes implemented across SERVICE_PATHS in dependency
   order.
2. **Prompt 2** — Developer writes unit tests from the Target Contract, per service.
3. **Prompt 3** — QA references pulled from the ticket; coverage checked across all services; gaps filled.
4. **Prompt 4** — Real test runs; mapping / pass / coverage gates computed; external gate run; fix loop.
5. **Prompt B** — Each AC verified against the implementation; cross-service contracts checked; gaps fixed.
6. **Prompt 5 §7-8** — Story Summary written; reports filed under `docs/reports/<STORY_KEY>/` and staged.

**QA handoff:** Prompts A and 2 can run when the dev picks up the ticket. Prompt 3 onward runs once QA cases exist in
`docs/qa/<STORY_KEY>.md` (or the ticket has acceptance criteria). Every step ends in human review — none merge or approve.

---

## Worked example — two services, one ticket

**Story:** `FEAT-001` — `Order Service | Add Express Shipping Flag to Order Line`

```ini
SERVICE_PATHS = [
  "src/backend/CoreService",
  "src/backend/OrderService"
]
```

**Prompt A scope resolution:**
- **1.a** `ENDPOINTS_IN_SCOPE` blank → continue.
- **1.b** AC reads: *"An order line must expose an `IsExpress` boolean. CoreService stores it;
  OrderService surfaces it on `GET /api/orderlines/{id}`."* Entities and a route named
  explicitly → scope resolved directly from the ticket.

**Scope table:**
| Service | Layer | File | Endpoint / Method | AC |
|---|---|---|---|---|
| CoreService | Domain | Domain/OrderLine.cs | `IsExpress` property | AC-1 |
| CoreService | Infrastructure | Repositories/OrderLineRepository.cs | `UpdateExpressFlag` | AC-1 |
| CoreService | API | Controllers/OrderLineController.cs | `PATCH /api/core/orderlines/{id}` | AC-1 |
| OrderService | Application | Services/OrderLineService.cs | `GetOrderLine` | AC-2 |
| OrderService | API | Controllers/OrderLineController.cs | `GET /api/orderlines/{id}` | AC-2 |
| OrderService | API | DTOs/OrderLineResponse.cs | `IsExpress` field | AC-2 |

**Cross-service dependency:** OrderService → calls → CoreService `GET /api/core/orderlines/{id}`.
CoreService response must be extended with `IsExpress` before OrderService can surface it.

**Implementation order:** CoreService (entity + repository + controller) → OrderService (service +
DTO + controller pass-through).

**Prompt B verification:** AC-1 satisfied (CoreService stores and returns the flag, covered by 3
unit tests). AC-2 satisfied (OrderService maps and exposes the flag, covered by 2 unit tests).
Cross-service contract: `CoreOrderLineResponse.IsExpress` (bool) → `OrderLineResponse.IsExpress`
(bool) — consistent, no type mismatch. Final status: PASS.
