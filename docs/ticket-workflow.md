# Ticket workflow (local, replaces Jira and Confluence)

```mermaid
flowchart TB
    T["tickets/&lt;ID&gt;.md<br/>status: ready"] -->|chore/ PR| M[main]
    M -->|/start-ticket| P1

    subgraph P1 ["Phase 1: before any edit"]
        direction LR
        C["Steps 1-3: checks<br/>ID, ready ticket on main,<br/>clean working tree"] --> B["Step 4: branch<br/>&lt;feat|bug|task&gt;/&lt;ID&gt;-&lt;slug&gt;"]
        B --> S(["Step 5: ticket-scout<br/>one-page brief · opus"])
        S --> S1{{"STOP 1: Step 6<br/>plan saved as ticket_plan.md<br/>approve, edit or reject plan"}}
    end

    S1 -->|approved| P2

    subgraph P2 ["Phase 2: build (allowed_paths only, worktrees)"]
        direction LR
        A["active-ticket.json<br/>scope guard on"] --> CI(["change-implementer<br/>edits code"])
        CI -->|worktree-apply.mjs| TW(["test-writer<br/>tests from the ACs"])
        TW -->|worktree-apply.mjs| R["Reports<br/>ac-results.md + ac-log row"]
    end

    P2 --> P3

    subgraph P3 ["Phase 3: review chain (review-ticket Method, one at a time, read-only)"]
        direction LR
        R1(["1 own-code-review<br/>vs approved plan, build, tests · opus"]) --> R3(["2 independent-code-review<br/>ID + range only · opus"])
        R3 --> SR(["3 security-reviewer<br/>every risk · opus"])
        SR --> R2(["4 test-critic<br/>must/should tests · opus"])
        R2 -.->|only if Infrastructure/ changed| R4(["5 ef-query-verifier<br/>SQL, indexes · sonnet"])
        R4 -.-> R5(["6 spec-drift-auditor<br/>docs still match · opus"])
        R2 --> R5
    end

    R1 -->|build or tests fail: skip 2-6| S2
    P3 --> S2{{"STOP 2: all reports + summary table<br/>saved as code_summary.md, review_findings.md<br/>owner picks findings; drift or a<br/>critical/high security finding blocks the PR"}}
    S2 -.->|fixes: change-implementer / test-writer,<br/>then /review-ticket or one review skill| P3
    S2 --> O["Owner: read diff, /code-review<br/>(+ built-in /security-review if high risk)"]
    O --> PR["PR (owner explains every change)"] --> CO["Close-out<br/>PR number, status: done, Delivery"]
    CO --> MG["Merge: pr-guard + Code Owner review<br/>then owner deletes active-ticket.json"]
```

Rounded boxes are agents, hexagons are owner stops, dotted lines are conditional. The same diagram, with a table per step, is in [start-ticket-chain.pdf](start-ticket-chain.pdf).

1. **Create** the ticket from `tickets/_templates/`. Fill component, risk, `allowed_paths`, acceptance criteria. Set `status: ready` before it is committed, then merge it to `main` through a `chore/` PR. `/start-ticket` and `pr-guard` read the ticket from `main`, so a ticket that is not merged, or merged as `draft`, cannot start.
2. **Start**: `/start-ticket FEAT-001`. The skill creates branch `feat/FEAT-001-<slug>`, runs `ticket-scout`, prints the brief and a plan, saves the plan as `docs/reports/<ID>/ticket_plan.md`, then stops. A doc and code disagreement in the brief is named above the plan but does not stop the ticket; `spec-drift-auditor` checks the component in the review chain (step 5).
3. **Approve**: the owner approves, edits or rejects the plan. Nothing but `ticket_plan.md` is written before this; the skill then records the approved text and the status (approved or rejected, with the date) in it.
4. **Build**: `change-implementer` edits code, `test-writer` writes tests from the acceptance criteria. Scope and command guards run on every tool call.
5. **Review**:
   - `/start-ticket` runs the review chain after the build; `/review-ticket <ID>` runs the same chain on its own (for example after fixes). It runs, one after the other, the agents behind the review skills: `/own-code-review <ID>` (the change against the approved plan, plus build and tests), `/independent-code-review <ID>` (a fresh agent that never sees the plan), `/security-check <ID>` (the `security-reviewer` agent, on every ticket whatever its risk), `/test-critique <ID>` (weak tests, missing cases, a ranked list of tests to add), and `/ef-verify <ID>` when persistence changes, then `/spec-drift` on the component. Fail fast: if the build or tests fail in `own-code-review`, the chain stops there, skips the other five, and the fix goes back before a rerun. Drift found here stops the PR: fix the docs or the code, or record a decision to continue in the ticket Notes. A critical or high security finding stops it until fixed. The chain shows all reports and a summary, saves them as `docs/reports/<ID>/review_findings.md` and a change summary (changed files, agent reports, test results, AC totals) as `code_summary.md` (a rerun overwrites both), then stops once; the owner picks the findings to act on. Each review skill still runs alone.
   - Fixes go back to `change-implementer` (code) or `test-writer` (tests) under the active ticket.
   - Then the owner reads the diff, runs `/code-review` (plus the built-in `/security-review` as a second look on high risk) and opens the PR using the template. `ai-miss` label if a reviewer had to rewrite agent code.
6. **Close out**, before the PR merges: the PR number in `docs/reports/ac-log.md`, `status: done` and a `## Delivery` section in the ticket, pushed to the same PR. A ticket PR that merges first needs a second `chore/` PR for this.
7. **Merge**: `pr-guard` green plus Code Owner review. Only a person merges. Then the owner deletes `.claude/state/active-ticket.json` (the command guard blocks agents from deleting it).

Outside tickets, `/spec-drift` audits specs, contracts and context files against the code at any time.

Specs and decisions live in `docs/` (specs in `docs/specs/`, decisions in `docs/adr/`). Code facts live in the code; if a doc disagrees with the code, the code wins and the doc gets a fix.

## Learning loop
Each miss gets a cause and the narrowest fix: wrong or missing project knowledge -> `REPO_CONTEXT.md`; brief missed code -> knowledge-map entry;
unclear skill step -> `SKILL.md`; agent went too far -> agent `tools` / `model` / `maxTurns`; unsafe action not blocked -> hook or permission rule;
ticket lacked acceptance criteria -> ticket template. Log it in `docs/learning-log.md`, fix by PR.
