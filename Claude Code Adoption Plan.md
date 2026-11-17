# Human-Owned, AI-Powered SDLC Adoption Plan with Claude Code

Nov 6, 2026 · @Shivansh Gupta

## Summary

We will adopt Claude Code in a human-owned, AI-powered SDLC, starting with one squad and widening only on measured value. Every ticket, change and release stays owned by a named person, and agents only make that person faster and safer.

A three-week Phase 0 covers decisions, a security review and a legal review of at least one full week each, prerequisites and a baseline. One squad covering development and QA then runs a six-week pilot on a single well-tested service, with low- and medium-risk tickets only. Its value is measured against that baseline. An end-of-pilot review in week 10 then decides whether adoption widens to a second squad. Explicit stop criteria pause the pilot if quality, safety or cost slips.

The pilot scope is four items: `ticket-scout`, `change-implementer`, `test-writer` and `/start-ticket`. Everything else is backlog. A mandatory GitHub check, a plain script with no AI, validates allowed paths, secrets, licences and knowledge map globs on every PR. Implementation detail is in the Appendix at the end of this plan.

## Goals and principles

Our model sits between AI-assisted and AI-native. Claude Code is built into every stage, but a named person owns all work, and agents act only inside that person's session, plan and approval.

|  | AI-assisted | **Our model: human-owned, AI-powered** | AI-native |
| --- | --- | --- | --- |
| Who owns a ticket | A person | A named person, always | Often an agent, with oversight |
| How Claude is used | Personal, ad hoc prompts | Shared agents, skills and plugins built for our SDLC | Agents run most stages end to end |
| Who starts work | The person | The person, through `/start-ticket` | Agents pick up work from queues |
| Who edits code | The person | An agent inside the harness, after the owner approves the plan | Agents edit and open PRs on their own |
| Review | People only | Owner runs AI review, then a person reviews | AI review gates most changes |
| Guardrails | Personal judgement | Shared permissions, hooks and local checks | Platform autonomy controls |

By the end of the pilot we commit to four outcomes:

- **Ownership:** every ticket, PR and release has a named human owner, and no work is owned by an agent.
- **Evidence:** widening adoption rests on measured value against a baseline, not on usage alone.
- **Shared context:** the pilot repository has maintained, vendor-neutral context (`REPO_CONTEXT.md` and the knowledge map), reviewed like code.
- **Visibility:** leadership sees value, quality, cost and risk in one weekly view.

Six principles guide every decision:

1. **People own outcomes.** Whoever opens a PR owns it, whoever or whatever wrote the code.
2. **Spec before code.** Agent output is only as good as the intent it is given.
3. **Verification grows first.** Tests, scans and review capacity scale before code volume does.
4. **Context is a team asset.** Instructions, skills and hooks live in version control.
5. **Guardrails live in configuration.** Settings and hooks enforce policy, not memos.
6. **Measure teams, not individuals.** Usage data is never used to rank people.

Out of scope: widening adoption before the end-of-pilot review, client code or data before legal approval, Claude Code in CI/CD pipelines (a DevOps decision), headcount targets, handing work to autonomous agents, removing human review from production changes, and mandating one personal workflow.

## Adoption measures and pilot guardrails

Adoption is tracked on use, safety and cost, and value is measured against the Phase 0 baseline.

**Operational thresholds.** A PR changes at most 400 lines and 15 files, excluding generated and lock files. Larger PRs are split, or a lead adds the `size-exception` label. The first review comes within 8 working hours, and a merge decision within 2 working days.

**Stop criteria.** Any one of these pauses the pilot at once, and the steering group decides within 2 working days whether to fix and resume or stop:

1. A change outside the allowed paths, or a secret, reaches a protected branch.
2. A severity 1 or 2 escaped defect is traced to a Claude Code PR.
3. PR rework or review wait time for Claude Code PRs is more than 25% worse than baseline for two weeks running.
4. Weekly spend exceeds 120% of the approved budget.
5. Legal or security withdraws approval, for example over data residency.

**Adoption measures** are reviewed weekly by the steering group:

- **Weekly use:** pilot squad members using Claude Code for real work each week ([analytics](https://code.claude.com/docs/en/analytics) or telemetry).
- **Ticket flow:** the share of the squad's new tickets started with `/start-ticket`.
- **Safety:** results of the GitHub required check, with a target of zero out-of-scope changes merged.
- **Enablement:** pilot squad members who finished Foundations.

Lines of code and suggestion accept rate are diagnostics only. The analytics leaderboard is never used for performance decisions.

## Assumptions and prerequisites

**Confirmed:** code is in GitHub (github.com), with local working copies in each developer's Visual Studio. Documentation is in Confluence and work is tracked in Jira, where tickets can carry a component and acceptance criteria. The codebase is large and split into microservice repositories, such as Order Service and Inventory Service. We are primarily on Azure, and we already run a Bedrock-hosted assistant. Developers run Claude Code in WSL2 on their Windows machines, so its built-in sandbox is available for shell commands.

**Hard prerequisites.** The pilot does not start on a service repository without all four:

1. Branch protection with required CODEOWNERS review on the pilot repository (Appendix A18).
2. Automated tests that developers can run locally on the pilot service.
3. The GitHub required check (`pr-guard`) in place and tested.
4. Legal sign-off covering data, IP terms and client consent. Until then, the pilot runs on a dummy repository with no client code or data.

The rows below are still presumed and are confirmed during Phase 0. A wrong assumption changes the plan as the last column shows.

| Assumption | Status | If this is wrong |
| --- | --- | --- |
| Jira and Confluence are Atlassian Cloud, not Data Center | Presumed | Atlassian's remote MCP server serves Cloud sites, so Data Center needs a different connector. Confirm in Phase 0 |
| Key Confluence pages are mostly current and have owners | Presumed | Clean up the pilot service's top pages first, or agents repeat stale advice |
| The pilot service has useful automated tests that run locally | Presumed | Hard prerequisite: choose another service, or add tests before the pilot |
| We can require branch protection, checks and CODEOWNERS | Confirmed | The final human gate and the required check cannot be enforced |
| The deployment route is Claude for Enterprise, Amazon Bedrock or Microsoft Foundry | Presumed | On Bedrock or Foundry, server-managed settings and contribution metrics are unavailable, so OpenTelemetry supplies the measures |
| Company and client terms allow pilot code to be sent to Claude under commercial terms | Presumed | The pilot stays on the dummy repository, or some repositories stay out of scope |
| Observability uses Azure Monitor and Application Insights, and the cloud CLI is `az` | Confirmed | A different connector and telemetry target are needed |
| The pilot squad has about 6 developers and 2 QA engineers | Presumed | Budget, people-time and the 15-PR minimum per group are recalculated |
| Code intelligence plugins exist for the pilot service's main languages | Presumed | Search falls back to text, which uses more tokens |

## Target SDLC

At every stage Claude Code helps the owner, and a named person stays accountable. It drafts stories and acceptance criteria, maps code and drafts ADRs, implements on a branch, generates tests, reviews the diff before the PR, writes release notes and triages logs read-only. People keep priority and scope, design decisions, every change, the quality call, final PR approval, go or no-go, and production actions. The stage-by-stage table is in Appendix A14.

## Role playbooks

Everyone keeps ownership of their work, and Claude Code changes how they do it. Leaders use the tool themselves, because people follow what leaders do more than what they announce. Starting points, watch-outs and tools for each role are in Appendix A1.

| Role | Shift | Owns |
| --- | --- | --- |
| Junior developer | Works with Claude Code as a pair, while still writing and owning every change | Explaining every line of their PR without help |
| Senior developer | Still the main implementer of complex changes, with Claude doing repetitive, wide-reaching edits under their review | Repository REPO\_CONTEXT.md files, pilot agents and skill, mentoring juniors on review |
| Engineering lead | Still plans and assigns work to people, and sets how the squad uses Claude Code | Working agreement, knowledge map entries, definition of done, PR size limits, baseline and pilot measures |
| Architect | From guidance in documents to guidance agents follow and hooks enforce | Reference patterns, architecture rules in shared configuration, review of every new MCP integration |
| Manager | From tracking output to coaching people through the change, and setting standards across squads | Learning time, morale, pilot squad selection, budget, hiring profiles and career frameworks |
| Engineering director | From approving a tool to sponsoring a change in how people work, with ownership staying with people | Funding, the end-of-pilot decision, policy sign-off with security and legal, the link to business goals |
| QA team | Still owns testing and the quality call, now joining at the spec stage | Risk-based test strategy, quality gates, test data rules, defects in AI-assisted changes |
| DevOps team | Still owns infrastructure and releases, and also runs the team's Claude Code setup | Managed settings, the GitHub required check, hooks and their on-call, version pinning, telemetry, secrets handling |

## Timeline: Phase 0, pilot, review

The adoption plan takes about 10 weeks, counted from the start of Phase 0: a three-week Phase 0, a six-week pilot with one squad, and an end-of-pilot review in week 10. If legal sign-off runs long, it takes up to 12 weeks. Nothing scales before that review.

&#91;embedded content: Adoption timeline · 3-week Phase 0, 6-week pilot, review in week 10\]

1. **Phase 0, week 1: decisions.** Confirm sponsorship and the steering group, compare and pick the deployment route, confirm the pilot squad, service and ticket scope, approve the budget, and confirm Atlassian Cloud or Data Center. Pull the baseline. Security and legal get the review pack by day 3, once the route is chosen. DevOps and the champions start building on the dummy repository, which needs no sign-off.
2. **Phase 0, weeks 1-2: security review, at least one full week.** Security reviews the chosen route, data retention and zero data retention, managed settings and permission rules, network and shell rules, prompt-injection controls and telemetry privacy. It ends with signed-off managed settings, which reach squad machines only after sign-off.
3. **Phase 0, weeks 1-2: legal review, at least one full week, in parallel.** Legal reviews IP and licence terms, data residency and client consent for the service repository. Its approval lets Claude Code work on the service repository; until then, all work stays on the dummy repository.
4. **Phase 0, week 3: prerequisites.** Roll out the signed-off managed settings and pin the Claude Code version. Turn on the GitHub required check, branch protection and CODEOWNERS on the pilot repository. Write `REPO_CONTEXT.md` and knowledge map entries for the pilot service, finish the four pilot items, and set up Claude Code in WSL2 with its sandbox on. Test every control with a deliberate bad edit and run Foundations training.
5. **Pilot weeks 1-2 (weeks 4-5): ramp-up.** The squad starts on the dummy repository, then moves to low-risk real tickets on the service repository once legal approves. Champions pair with each member, and delivery is expected to dip while people learn.
6. **Pilot weeks 3-6 (weeks 6-9): steady use.** Medium-risk tickets join the mix. The steering group reviews the measures and stop criteria weekly, and a mid-pilot review at the end of pilot week 3 decides whether to continue, adjust the scope or stop.
7. **End-of-pilot review (week 10).** The engineering director reviews the adoption measures, the stop criteria and value against the baseline, and decides whether adoption widens to a second squad.

Security and legal each get at least one full week, because a few days is rarely enough for either. Legal sign-off can still run past Phase 0. If it does, the squad works on the dummy repository, the pilot clock starts only when real tickets do, and the plan stretches to about 12 weeks. The day-by-day checklist is in Appendix A17.

## Pilot scope and resourcing

One squad of developers and QA works on one well-tested service, with low- and medium-risk tickets only. Inventory Service is the proposed candidate. Work starts on a dummy repository with no client code or data, and moves to the service repository only after legal approval.

The pilot builds and measures four items. Everything else is backlog until the end-of-pilot review.

| Item | Kind | Job | Access |
| --- | --- | --- | --- |
| `/start-ticket` | Skill | Reads the Jira ticket, calls `ticket-scout`, writes the brief and asks the owner to approve the plan | Atlassian connector read-only, repository read |
| `ticket-scout` | Agent, read-only | Searches code and Confluence in its own context and returns about one page of excerpts with links | Read, Grep, Glob and Atlassian read; no edit, no shell |
| `change-implementer` | Agent, edits | Makes the approved, scoped edit in an isolated worktree | Read and Edit inside allowed paths; shell only with approval; no network tools |
| `test-writer` | Agent, edits tests | Writes tests from the acceptance criteria and runs them locally | Edit in test paths only; shell only with approval; no network tools |

**Backlog:** `failure-triager`, `design-reviewer`, `iac-reviewer`, `incident-triager`, `delivery-report`, the other custom skills (including `test-cases-from-ac` and `capture-learning`), extra plugins, the Azure, Slack and Figma connectors, and agent teams. Built-in commands such as `/code-review` need no setup and may be used, but they are outside the evaluated scope.

**People-time** is the larger cost. These estimates (proposed) assume about 6 developers and 2 QA engineers over a three-week Phase 0 and a six-week pilot:

| Who | Phase 0 (3 weeks) | Each pilot week | Total |
| --- | --- | --- | --- |
| Each pilot squad member (8) | 4 h Foundations and setup | 2 h learning, 15 min logging misses | About 19 h each, 150 h in all |
| Champions (1 senior developer, 1 QA) | 4 days building the four items, `REPO_CONTEXT.md` and knowledge map | 0.5 day support and office hours | About 7 days each |
| DevOps engineer | 6 days: managed settings, version pin, GitHub check, OpenTelemetry to Azure Monitor, WSL2 and sandbox setup | 1 day: check, hooks, on-call, upgrades | About 12 days |
| Engineering lead | 1.5 days: baseline, knowledge map review | 2 h: measures and steering | About 3 days |
| QA lead | 1 day: baseline and defect tracing | 2 h: escaped defect tracing | About 2.5 days |
| Architect | 1 day: `REPO_CONTEXT.md` pattern and control review | 1 h | About 2 days |
| Security and legal | About 3 days of security review and 2 days of legal review, spread over at least one full week each | As needed | About 5 days |

Deployment route

The deployment route is compared side by side in Phase 0:

|  | Claude for Enterprise | Amazon Bedrock | Microsoft Foundry |
| --- | --- | --- | --- |
| Where traffic goes | Anthropic | Our AWS account | Our Azure subscription |
| Fit with our Azure estate | Separate vendor and billing | Separate cloud; already hosts our assistant | Azure billing, identity and monitoring |
| Managed settings | Server-managed from the admin console, or a file | File through device management only | File through device management only |
| Contribution metrics and analytics | Yes, with github.com | No; OpenTelemetry instead | No; OpenTelemetry instead |
| Features needing a claude.ai account (Code Review, cloud sessions, Remote Control) | Available | Not available | Not available |
| Data residency for India-based teams | Confirm with Anthropic | Confirm region and cross-region inference | Confirm deployment region |
| IP indemnity | Anthropic Commercial Terms | Confirm with AWS | Confirm with Microsoft |

If security requires traffic to stay in our own cloud, we choose Foundry or Bedrock and accept losing contribution metrics. OpenTelemetry to Azure Monitor and Application Insights then supplies the measures. The engineering director and security decide in Phase 0.

## Platform and shared knowledge

The deployment route is chosen in Phase 0 from the comparison above. Claude for Enterprise, the default in the [admin setup guide](https://code.claude.com/docs/en/admin-setup), gives server-managed settings and contribution metrics. Amazon Bedrock and Microsoft Foundry are both viable and keep traffic in a cloud account we control. They lose the features that need a claude.ai account, and managed settings then go out as a file through device management.

Knowledge stays where it already lives, and everything Claude needs is in Git or a plugin, so a new team member gets the same setup from one clone and one install. We will not build a search index: Claude Code [navigates by search and needs no codebase index](https://claude.com/resources/articles/how-claude-code-works-in-large-codebases-best-practices-and-where-to-start), and works best with good starting context.

Each kind of knowledge lives in one place: managed settings, Git, the plugin marketplace, Confluence, Jira or local notes. Appendix A10 shows where each lives, when it loads and who owns it, and `capture-learning` turns useful personal notes into PRs to shared files.

- **Telemetry:** DevOps runs [OpenTelemetry export](https://code.claude.com/docs/en/monitoring-usage) of sessions, tokens, cost, commits and PRs to Azure Monitor and Application Insights from Phase 0. On a cloud route it also replaces contribution metrics for the value measures.
- **Pre-PR review:** authors may run the built-in `/code-review` and `/security-review` locally, outside the evaluated scope. Managed Code Review is not used, to control cost.
- **CI/CD:** one mandatory GitHub required check, `pr-guard`, runs on every PR as a plain script with no AI (see Control model). Whether Claude Code runs in pipelines stays a DevOps decision.
- **Budget:** Anthropic's [cost guidance](https://code.claude.com/docs/en/costs) reports enterprise averages of about $13 per developer per active day and $150-250 per developer per month. People-time is the larger cost (see Pilot scope and resourcing). Figures are illustrative, so confirm pricing for the chosen route.

### Repository context, Visual Studio and the knowledge map

Repository context stays vendor-neutral. `REPO_CONTEXT.md` remains the one source of repository knowledge for any tool, and Claude Code reads it through a one-line `CLAUDE.md` containing `@REPO_CONTEXT.md`, using [memory imports](https://code.claude.com/docs/en/memory). If even that file is unwanted in the repository, the setup script creates it locally and Git ignores it. Claude-specific agents, skills and hooks live in our plugin repository and in managed settings, not in service repositories. Switching tools means deleting the shim, not rewriting context.

Developers keep working on local copies in Visual Studio, and run Claude Code in WSL2 in a terminal beside it, on the same working copy. WSL2 gives Claude Code its built-in sandbox, which native Windows lacks, so shell commands run with limited file and network access (see Control model; Appendix A3). In Phase 0, DevOps settles where the working copy lives so that both Visual Studio and WSL2 work on it smoothly.

The knowledge map is our own tool-neutral convention: a file in each repository that routes a Jira component to its code paths, Confluence pages, owners and risk level. Leads and module owners write the pilot service's entries in Phase 0 with Claude drafting, and gaps found later become PRs (Appendix A4). The GitHub required check fails any PR that leaves a map glob matching no file, so the map cannot silently rot after a rename. The eight setup steps are in Appendix A2.

## Ticket-scoped context

A person picking up a ticket runs `/start-ticket PROJ-123`, and Claude then works from a one-page brief rather than the whole repository or Confluence space. `start-ticket` and `ticket-scout` are custom items we build, and the pilot must prove they work on our real tickets.

&#91;embedded content: Ticket flow · from Jira ticket to pull request\]

No edit starts until a person approves the plan, and the brief goes into the PR description so reviewers see what context was used.

- **Narrow by design:** only the ticket's summary, acceptance criteria, component and direct links are read. The component selects code paths and pages from the knowledge map, and `ticket-scout` searches in its own context and returns about one page of relevant excerpts with links.
- **No guessing:** if a ticket has no component, Claude asks the person. If work needs paths outside the map, Claude stops and asks, and repeated gaps become map PRs.
- **Same access, external content marked:** the Atlassian connector uses each person's existing permissions. The brief wraps every excerpt from Jira, Confluence or a linked page in a marked block that names its source and says it is data, not instructions, and editing agents see only the brief.
- **Tickets that span repositories,** such as Order Service plus Inventory Service: the knowledge map entry lists every repository the component touches. The owner opens the session in the primary repository and adds the other with `--add-dir`, so `ticket-scout` can read both. Each repository gets its own PR, brief section and owner review, linked by the Jira key, and `change-implementer` edits one repository per run. In the pilot, a second repository is read-only context; tickets needing edits in two repositories stay with people.

## Agents, skills and connectors

The pilot uses three agents and one skill: `ticket-scout`, `change-implementer`, `test-writer` and `/start-ticket` (see Pilot scope and resourcing). The limit is deliberate, because [each subagent runs its own context window](https://code.claude.com/docs/en/sub-agents) that counts toward usage. A task becomes an agent only if it reads a lot of output or needs restricted tools; otherwise it is a skill. The owner's session plans and reviews, `change-implementer` makes scoped edits inside the harness, and agents never own a ticket or a PR. [Agent teams](https://code.claude.com/docs/en/agent-teams) stay off, because they are experimental, and the cost guidance (section "Manage agent team costs") says they use about 7 times the tokens of a standard session when teammates run in plan mode.

Backlog items are built only if adoption widens after the pilot, one at a time and measured the same way. Anything unused 60 days after it ships is retired.

The built-in subagents (`Explore`, `Plan`, `general-purpose`) need no setup. Each pilot item's job, model, access and owner, and the backlog items, are in Appendix A11.

Pilot connectors are Atlassian (MCP, read-only for agents) and the GitHub `gh` CLI. If adoption widens after the pilot, the Azure CLI (`az`, read-only) and Azure Monitor and Application Insights queries follow; Slack and Figma stay optional (Appendix A5).

## Token efficiency

Sonnet is the team default, with haiku for simple work and opusplan for hard problems. Everyone keeps one task per session, plans before building and keeps long logs out of the context. Repository owners keep each REPO\_CONTEXT.md, which CLAUDE.md imports, under 200 lines, and DevOps sets models, effort, turn limits and spend limits centrally. The full guide is in Appendix A8.

## Control model and safety harness

Controls differ in strength, and only some are true guarantees. With CI otherwise out of scope, only three controls cannot be bypassed from a developer's machine: branch protection with CODEOWNERS, the GitHub required check, and server-managed permissions. Machine-enforced controls stop most mistakes but run where a person has admin rights. Conventions depend on people. The same model covers the main session and every agent.

&#91;embedded content: Control model · what each layer enforces, and where\]

| Control | What it stops | Enforced by | Known bypass |
| --- | --- | --- | --- |
| Branch protection with required CODEOWNERS review | Unreviewed merges | GitHub | Org admins only |
| `pr-guard` required check, a plain script with no AI | Files outside allowed paths, secrets, disallowed licences, broken map globs, oversized PRs | GitHub | Org admins only |
| Managed settings: deny rules, bypass mode disabled, approved MCP servers and hooks only, minimum version | Risky tools and commands, unapproved integrations | The developer's machine (server-managed on Enterprise, a file on Bedrock or Foundry) | Local admin can edit a managed file; deny rules match command text |
| Editing agents with no network tools and shell on approval | Fetching or sending data during edits | The developer's machine | An approved shell command can still reach domains on the sandbox allowlist |
| Claude Code sandbox in WSL2 | Shell commands, and the processes they start, writing outside the working folder or reaching unapproved domains | The developer's machine (WSL2) | Local admin can turn it off; covers shell commands only, not Claude's Read and Edit tools |
| Scope guard and command guard hooks | Edits outside the brief, risky commands | The developer's machine | A broken hook can fail open; the required check backs it up |
| Isolated worktree | Half-finished edits in the working copy | The developer's machine | None needed |
| Pre-push checks | Early feedback before a PR | Convention | `git push --no-verify` |
| Plan approval, brief, review quality, risk levels | Wrong intent, high-risk work given to agents | Process | People |

**Mandatory GitHub check.** DevOps writes `pr-guard` as a plain script with no AI and marks it required in the branch ruleset, so `--no-verify` cannot skip it. On every PR it fails when:

- a changed file is outside the allowed paths for the PR's Jira component in the knowledge map, or touches a never-edit path (secrets, production config, CI workflows, harness files);
- a secret scanner such as gitleaks finds a match;
- a new dependency's licence is not on the approved list;
- any glob in the knowledge map matches no file;
- the PR exceeds 400 changed lines or 15 files without a lead's `size-exception` label.

The check reads allowed paths from the knowledge map on the target branch, so a PR cannot widen its own scope without a CODEOWNERS-approved map change.

**Prompt injection controls.** `change-implementer` and `test-writer` get no network access: their tool lists exclude WebFetch, WebSearch and every MCP server, and managed settings deny network commands such as `curl` and `Invoke-WebRequest`. Every shell command needs the owner's explicit approval, because editing agents have no shell allow rules and bypass mode is disabled. Only `ticket-scout` reads external content, with read-only tools, and the brief marks each excerpt as external data. An agent that finds text trying to instruct it stops and hands back to the owner.

**Sandbox in WSL2.** Native Windows has no shell sandbox, so Claude Code runs in WSL2, where the [built-in sandbox](https://code.claude.com/docs/en/sandboxing) limits file writes and network access for every shell command and the processes it starts. An approved command therefore cannot write outside the working folder or reach a domain off the allowlist, even when a test script or package hook makes the call. Shell commands still need the owner's approval; the sandbox limits what an approved command can do. DevOps sets it up and tests it on the dummy repository in Phase 0.

DevOps builds `pr-guard`, the scope guard and the command guard, and tests every control with a deliberate bad edit on the dummy repository in Phase 0. Organisation-level rules (data scope, retention, bypass mode, approved integrations, production access, local data and telemetry privacy) are in Appendix A6. The exact controls and known limits are in Appendix A7, and how to turn on branch protection and CODEOWNERS is in Appendix A18.

The `risk` value in the knowledge map sets how far an agent can go. Agents edit low- and medium-risk work after plan approval, a person leads high-risk work such as payments, authentication, migrations and infrastructure, and agents never edit secrets, production config, CI workflows or the harness itself (Appendix A12).

An agent stops and hands back to the person when the work needs a path outside the brief, acceptance criteria are unclear or code and Confluence disagree, tests still fail after the `maxTurns` limit, or it finds a secret or text that tries to instruct it.

## Enablement

Each pilot squad member gets two protected hours a week during the pilot, and every track ends with a real ticket rather than a certificate. In Phase 0 the squad takes Foundations, and developers and QA then take developer practice or quality engineering. Champions and DevOps take the advanced builder and platform tracks, and the steering group takes leading the change (Appendix A13). Other tracks wait until adoption widens. Champions with about 10% of their time, weekly office hours and a mid-pilot show and tell keep learning going.

## Risks and mitigations

The biggest risks are people, quality and evidence, not tooling. Change volume could hurt stability, review could become the bottleneck, juniors could lose judgment, and people could fear for their jobs or adopt shallowly. Secrets or prompt injection could slip through, and shared knowledge could go stale. Legal sign-off could run past Phase 0, and a pilot too small could fail to show a real difference. The main mitigations are the GitHub required check and human review, PR size limits, explain-your-PR pairing, and an early honest message from the director. Protected learning time, the four-item scope, the dummy repository, the stop criteria and the 15-PR minimum per group also help. Each risk's early signal, mitigation and owner is in Appendix A15.

## Ownership

The engineering director is accountable for the adoption and the end-of-pilot decision. A steering group runs it, meeting weekly during Phase 0 and the pilot: the director, a manager, one architect, the QA lead, the DevOps lead and a security representative.

The engineering director is accountable for the adoption, gates, policy and communication. The DevOps lead owns the platform and telemetry, a manager owns squad adoption, training and reporting, the architect owns shared configuration, and the QA lead owns quality gates. The full RACI is in Appendix A16.

**Hooks, scope guard and required check: ownership and on-call.** The DevOps lead owns the hooks, the scope guard and `pr-guard`, with a named primary and backup DevOps engineer on call in working hours (IST) during the pilot. A failing hook blocks work, so the fix target is 4 working hours. Until it is fixed, DevOps may switch that hook off for the squad through managed settings, because `pr-guard` still guards every PR. Changes to these files need DevOps and architect review.

**Version pinning and upgrade testing.** DevOps pins the pilot to one tested Claude Code version. They [install that exact version](https://code.claude.com/docs/en/setup), set `DISABLE_AUTOUPDATER` to `1` in managed settings (`DISABLE_UPDATES` also blocks manual updates) and set a minimum version. A champion and a DevOps engineer test each candidate version on the dummy repository against the saved miss tests before rollout, at most every two weeks. The release channel (`autoUpdatesChannel`) stays on `stable` for anyone not pinned.

## Open decisions

| Decision | Owner | Why it matters |
| --- | --- | --- |
| Phase 0 start date | Engineering director | All timings count from the start of Phase 0 |
| Deployment route: Claude for Enterprise, Bedrock or Foundry, compared side by side | Engineering director, security | Sets features and controls. If traffic must stay in our cloud, we accept losing contribution metrics and use OpenTelemetry |
| Repositories in scope, and whether any code needs client consent | Security, legal | Dummy repository first, no client code or data until legal approves, then one microservice repository |
| Zero data retention required or not | Security, legal | Rules out contribution metrics |
| Scope of the Azure connector (az, read-only) if adoption widens | DevOps lead | Azure Monitor and Application Insights are confirmed for telemetry |
| Headcount by role and monthly budget | Manager | Sets licences and people-time (Pilot scope and resourcing) |
| Pilot squad, service and ticket scope (Inventory Service proposed, low and medium risk only) | Manager, architect | Determines how much the pilot proves |
| Whether Claude Code runs in CI/CD pipelines beyond pr-guard | DevOps lead | The non-AI required check is the minimum and is in scope; the rest is a DevOps decision |
| Whether earlier legal discussions already cover the pilot | Legal | Could shorten the legal review |
| Targets, operational thresholds and stop criteria | Steering group | The proposed values must be fixed before the baseline is final |
| Atlassian Cloud or Data Center | DevOps lead | Decides which Atlassian connector the pilot uses |

## Open items for Phase 0

These items need an owner's answer before the pilot reaches the service repository.

| Item | Owner | Current position |
| --- | --- | --- |
| Claude Code version pinning and upgrade testing | DevOps lead | One pinned version, auto-update off, each new version tested on the dummy repository first (see Ownership) |
| Ownership and on-call for hooks and the scope guard | DevOps lead | Named primary and backup in working hours, 4-hour fix target, `pr-guard` as the backstop (see Ownership) |
| Data residency and contracts for India-based teams | Security, legal | Check client contracts for limits on processing outside India. Confirm where each route processes data: Anthropic's regions, Bedrock in Mumbai (ap-south-1) or cross-region inference, and Foundry deployment regions |
| IP and licensing of AI-generated code, including indemnity | Legal | Anthropic's [Commercial Terms](https://www.anthropic.com/legal/commercial-terms) cover output ownership and an IP indemnity; legal confirms the conditions. On Bedrock or Foundry, AWS or Microsoft terms apply instead. `pr-guard` blocks disallowed licences, and the PR owner answers for copied code |
| Portability if we switch tools | Architect | `REPO_CONTEXT.md`, the knowledge map, the brief format, `pr-guard`, PR labels and the miss log are tool-neutral. Only the `CLAUDE.md` shim, agent and skill definitions (Markdown files) and managed settings are Claude-specific, so a switch means rewriting those files, not our knowledge |
| Exact source references for cost and token claims | Champion | Done; see Sources (cost guidance intro, "Manage agent team costs", "Keep cost and context down") |

## Extended plan: learning from our own mistakes

From the first pilot week, every mistake an agent or skill makes is logged, traced to its cause, fixed once in shared configuration, and tested so it does not come back.

1. **Capture, on every PR:** the change owner adds the `ai-miss` label and two lines on what went wrong. If adoption widens, `capture-learning` automates this.
2. **Triage, weekly:** champions and repository owners spend 30 minutes grouping misses by cause.
3. **Fix by PR** in the one place that matches the cause (Appendix A9), reviewed like code.
4. **Test:** each fixed mistake becomes a saved test case, rerun before every plugin release. DevOps checks whether `claude plugin eval` can run them.
5. **Release, every two weeks,** once tests pass, with notes listing the fixes, rolled out through managed settings. The person who reported the miss hears what changed.
6. **Review:** monthly, rework or retire weak skills and agents. Every three months, review the REPO\_CONTEXT.md files and the knowledge map.

Mistake signals, where each kind of fix goes, and the rules for fixing are in Appendix A9. Track misses per week and repeats of an already-fixed mistake, which should stay close to zero, since a repeat means the earlier fix went in the wrong place.

## Appendix: implementation reference

The detail behind the plan, for the people building and running the setup. The plan refers to each section by number.

### A1. Role details

| Role | Start with | Watch for |
| --- | --- | --- |
| Junior developer | Asking Claude to explain the code, small tickets through `/start-ticket` with a plan first, tests for own changes | Skipping the struggle that builds judgment: pair with a senior weekly and debug some issues by hand |
| Senior developer | Multi-file refactors and legacy code migration | Becoming the review bottleneck |
| Engineering lead | Tickets with a component and clear acceptance criteria, so `/start-ticket` builds a useful brief | Large AI-generated PRs nobody can review properly |
| Architect | Mapping legacy systems, drafting ADRs, comparing options against the real code | Design drift, where agent sessions solve the same problem differently |
| Manager | Sprint summaries from Jira and Confluence, and the weekly review of how each squad is adopting | Ranking people by usage (principle 6), and raising commitments before gains are proven |
| Engineering director | A clear message on why we are doing this and what it means for jobs | A mandate without time to learn, which produces shelfware |
| QA team | Manual testers: test cases from acceptance criteria, exploratory charters, bug reports, first automation. Automation engineers: converting manual suites, API and end-to-end tests, flaky-test triage | Tests that only mirror the code, since acceptance criteria define what correct means |
| DevOps team | Pipeline failures, infrastructure-as-code review, runbooks, read-only log triage | Agents with production write access; changes follow DevOps's own release process with human approval |

**Tools by role**

| Role | Plugins | Custom agents | Main skills | Connectors | Default model |
| --- | --- | --- | --- | --- | --- |
| Junior developer | core | `ticket-scout`, `change-implementer` | `start-ticket`, `change-workflow`, `pr-summary` | Atlassian, GitHub | sonnet |
| Senior developer | core, design | `ticket-scout`, `change-implementer`, `design-reviewer` | `start-ticket`, `capture-learning`, `threat-model`, `/code-review` | Atlassian, GitHub | sonnet |
| Engineering lead | core | `ticket-scout` | `spec-from-ticket`, `pr-summary` | Atlassian, GitHub | sonnet |
| Architect | core, design | `ticket-scout`, `design-reviewer` | `adr-draft`, `threat-model` | Atlassian, GitHub | opusplan |
| Manager, engineering director | leadership | None | `delivery-report` | Atlassian | haiku |
| QA, manual | core, quality | `ticket-scout`, `test-writer` | `test-cases-from-ac`, `bug-report` | Atlassian | sonnet |
| QA, automation | core, quality | `ticket-scout`, `test-writer`, `failure-triager` | `e2e-test-scaffold`, `bug-report` | Atlassian, GitHub, observability | sonnet |
| DevOps | core, platform | `ticket-scout`, `iac-reviewer`, `incident-triager` | `release-notes`, `runbook`, `incident-review` | Atlassian, GitHub, cloud, observability | sonnet |

### A2. Setup steps

1. Inventory repositories, modules and owners, and name one owner per repository.
2. Write or update `REPO_CONTEXT.md` in the root and in large module folders, with `/init` drafting, as the [large codebase guidance](https://claude.com/resources/articles/how-claude-code-works-in-large-codebases-best-practices-and-where-to-start) recommends. Keep each under 200 lines, and add the one-line `CLAUDE.md` shim that imports it.
3. Deny rules come from managed settings and the Atlassian connector from our plugin, so nothing Claude-specific beyond the shim is committed to the service repository. Each user approves the connector once.
4. Install [code intelligence plugins](https://code.claude.com/docs/en/plugins/code-intelligence) for our main languages, such as `csharp-lsp` or `clangd-lsp` for C# or C++, so Claude searches by symbol and sees type errors after edits.
5. Build the knowledge map (A4), starting with the 10 most-changed components.
6. Clean Confluence: give key pages an owner and review date, label them by Jira component, and archive stale pages.
7. Update Jira templates to require a component, acceptance criteria and links to design pages.
8. Publish the marketplace repository, pin it to a version tag, and roll plugins out through managed settings.

### A3. Visual Studio and local working copies

Developers edit local copies in Visual Studio, and Claude Code works on the same folder. We found no Claude Code extension for Visual Studio, so developers run Claude Code in a WSL2 terminal beside it.

- Claude's edits appear in Visual Studio's Git Changes window, where the owner reviews and commits them.
- `change-implementer` works in a separate folder under `.claude/worktrees/`, and the owner opens that folder or its branch in Visual Studio before merging.
- Uncommitted work and local branches are visible only to that person's Claude. Anything the team needs, including lessons, reaches GitHub through a PR.
- Claude Code runs in WSL2 rather than [natively on Windows](https://code.claude.com/docs/en/setup), because its sandbox works in WSL2 and not on native Windows. The harness still relies on permission rules, hooks, the pr-guard required check and human review; the sandbox adds an operating-system limit on shell commands.

### A4. The knowledge map

The knowledge map is our own convention, not a Claude Code feature. It routes a Jira component to the small set of code and pages that matter. This example is illustrative:

```yaml
payments:
  jira_components: [Payments, Billing]
  code_paths: [services/payments/**, libs/money/**]
  confluence_pages: [123456, 123789]
  protected_paths: [services/payments/migrations/**]
  owners: [team-payments]
  risk: high
```

The architect defines the format and component list and reviews the first version. Engineering leads own their squad's entries, module owners and senior developers fill in code paths, protected paths and risk, and Confluence page owners supply page IDs and keep those pages current. Anyone proposes a fix by PR when a brief misses something.

1. **Agree the components in Phase 0**, using existing Jira components as keys.
2. **Start with 10 components**, the most changed in the last 3 to 6 months.
3. **Draft with Claude, correct by hand.** Claude suggests paths and pages from the repository and recent tickets, and the module owner checks every path and sets the risk. Each entry takes about 30 to 60 minutes, so the first 10 take a few days of part-time work in Phase 0.
4. **Commit through a PR** reviewed by the architect and module owner.
5. **Test on the dummy repository, then in the pilot.** A brief that misses something points to a wrong or incomplete entry.
6. **Grow from gaps.** Repeated requests to widen scope become map PRs.

Three rules keep shared knowledge trustworthy. Each fact has one source: code facts in the repository, decisions in Confluence, work state in Jira. Code wins, so if a page disagrees with the code, the brief flags it and the page owner fixes it. And every change to shared configuration goes through a PR, reviewed like code.

### A5. Connectors

Where a good CLI exists we use it instead of an MCP server, as the [cost guidance](https://code.claude.com/docs/en/costs) recommends.

| Connector | Kind | Used for | Access | Users | When |
| --- | --- | --- | --- | --- | --- |
| Atlassian (Jira, Confluence) | MCP (Cloud; Data Center needs another connector) | Tickets, specs, ADRs, reports | Read-only for agents | Everyone | Pilot |
| GitHub: `gh` | CLI | PRs, issues, check status | Developer's own permissions | Developers, leads, QA automation, DevOps | Pilot |
| Azure Monitor and Application Insights | CLI or MCP | Logs, traces, alerts, errors | Read-only | DevOps, QA automation | If adoption widens |
| Azure CLI: `az` | CLI | Infrastructure checks | Read-only role | DevOps | If adoption widens |
| Slack (optional) | MCP | Incident and status updates | Read first | DevOps, leaders | Later |
| Figma (optional) | MCP | Design context for UI work | Read-only | Developers, QA | Later |

### A6. Governance and security

Guardrails go live in Phase 0, before the pilot, through managed settings, hooks and the GitHub required check. Security and legal sign them off, and any exception needs their approval.

| Area | Rule | Enforced by |
| --- | --- | --- |
| Code and data scope | Only repositories cleared by data classification are in scope | Approved repository list, security review |
| Training and retention | Commercial terms apply: [no training on our code or prompts](https://code.claude.com/docs/en/data-usage) unless we opt in, with 30-day standard retention | Contract and plan choice |
| Permissions | Nobody can switch to bypass mode or remove managed rules | `permissions.disableBypassPermissionsMode` in managed settings |
| Integrations | Only approved MCP servers and plugin marketplaces can be used | `allowManagedMcpServersOnly`, `strictKnownMarketplaces` |
| Production | Agents hold no production write credentials, and deployments follow the DevOps team's own process | IAM roles, DevOps release approvals |
| Local data | [Session transcripts](https://code.claude.com/docs/en/data-usage) are stored in plaintext on laptops for 30 days by default | Disk encryption, `cleanupPeriodDays` |
| Monitoring | Prompts stay redacted in telemetry, which is [the default](https://code.claude.com/docs/en/monitoring-usage) | `OTEL_LOG_USER_PROMPTS` left disabled |

[Zero data retention](https://code.claude.com/docs/en/data-usage) is available on Claude for Enterprise for qualified accounts, but it removes contribution metrics, so it is an open decision. Setting names change, so DevOps checks them against the [settings reference](https://code.claude.com/docs/en/settings-reference) during setup.

### A7. Safety harness controls and limits

DevOps builds the scope guard, the command guard and the `pr-guard` required check, a plain script with no AI that runs on GitHub for every PR. The other controls are configuration.

| Layer | Controls | Mechanism |
| --- | --- | --- |
| 1. Worktree | Editing agents work in their own copy on their own branch, and secrets are not copied in | [`isolation: worktree`](https://code.claude.com/docs/en/sub-agents) in the agent definition; secrets kept out of `.worktreeinclude` |
| 2. Permissions and shell limits | Each agent gets only the tools it needs. Secrets and sensitive paths are blocked, and Jira and Confluence are read-only for agents. Shell commands need approval unless allowlisted, and network commands are denied | `tools` lists; [`permissions.deny`](https://code.claude.com/docs/en/permissions) `Read` rules, which also block edits; denied `mcp__<server>__<tool>` write tools; `permissions.ask` for Bash and PowerShell; deny rules for commands such as `curl` and `Invoke-WebRequest` |
| 3. Hooks | The scope guard blocks edits outside the ticket's allowed paths, and the command guard blocks force pushes, pushes to main and destructive commands. Formatting and linting run after each edit, and tests for changed paths must pass before an agent finishes | [`PreToolUse`](https://code.claude.com/docs/en/hooks) hooks on edit tools and shell; `PostToolUse`; `Stop` and `SubagentStop` |
| 4. Local checks | Changed files must match the brief's allowed paths, including changes made through the shell. Tests, secret scans and licence scans must pass. The author runs `/code-review`, plus `/security-review` on high-risk changes | Git pre-push hook for early feedback (can be skipped); pr-guard required check on GitHub (cannot be skipped) |
| 5. People | A person approves every PR, code owners approve sensitive paths, and every change can be traced | Branch protection, CODEOWNERS; Jira key in branch and PR, brief in PR description, telemetry |

**Known limits.** These are why the controls overlap, and DevOps tests each control with a deliberate bad edit before the pilot uses `change-implementer`.

- **Windows:** we run Claude Code in WSL2 because the [sandbox](https://code.claude.com/docs/en/sandboxing) does not exist on native Windows. The sandbox covers shell commands only, not Claude's Read and Edit tools, which the permission rules still govern.
- **Command rules:** [they match command text](https://code.claude.com/docs/en/permissions), so they can be sidestepped. Approval prompts, the command guard and the pr-guard required check back them up, and pr-guard runs on GitHub, where neither a person nor an agent can skip it.
- **Rollback:** [`/rewind`](https://code.claude.com/docs/en/checkpointing) does not track shell changes and generally does not restore subagent edits. The real rollback is discarding the worktree branch.
- **Plugin agents:** [plugin subagents ignore](https://code.claude.com/docs/en/sub-agents) `hooks`, `mcpServers` and `permissionMode`. Harness hooks therefore live in managed settings, which also run inside subagents. The setup script installs the editing agents into each user's `~/.claude/agents/`, so they keep those fields without Claude files in service repositories.
- **Large repositories:** worktrees check out all tracked files, so the pilot measures the time and disk cost.

### A8. Token efficiency guide

The biggest savings are short sessions, the right model, and small always-loaded context. Most of this follows the [cost guidance](https://code.claude.com/docs/en/costs) and [best practices](https://code.claude.com/docs/en/best-practices). Check `/usage` for spend and `/context` for what is filling a session.

| Task | [Model](https://code.claude.com/docs/en/model-config) | Effort |
| --- | --- | --- |
| Reports, summaries, log triage, simple edits | haiku | low |
| Everyday coding, tests, reviews (the team default) | sonnet | medium, the default |
| Architecture, hard debugging, risky migrations | opusplan: opus to plan, sonnet to build | medium, high only if needed |

| Habit for everyone | How | Why it saves tokens |
| --- | --- | --- |
| One task per session | `/clear` between unrelated tasks | Old context is resent with every message |
| Compact with a focus | `/compact Focus on the API changes` | Keeps what matters and drops the rest |
| Plan before building | Plan mode with Shift+Tab | Avoids paying for the wrong build |
| Be specific | Name the files, function and expected result | Avoids broad searching of the repository |
| Keep logs out | Use `failure-triager` or a filtered command instead of pasting logs | Long logs fill the context |
| Undo instead of arguing | `/rewind` when a change goes wrong | Failed attempts leave the context |
| Ask side questions cheaply | `/btw` | Answers without adding to the history |
| Resume, don't re-explain | `/rename`, then `claude --resume <name>` | Avoids repeating background |
| Lower effort for simple work | `/effort low` | Less thinking on mechanical tasks |

The prompt cache lasts about an hour on a subscription but about five minutes on an API key, cloud provider or usage credits. On those routes, `/clear` or `/compact` before continuing a stale session.

**Repository rules for senior developers and leads**

- Keep each REPO\_CONTEXT.md [under 200 lines](https://code.claude.com/docs/en/memory), with only what Claude cannot read from the repository. `/doctor` suggests cuts.
- Put area-specific guidance in module-level `REPO_CONTEXT.md` files, each with its one-line shim so Claude loads it only when working in that folder, and procedures in [skills](https://code.claude.com/docs/en/skills), so each loads only when needed.
- Set `disable-model-invocation: true` on manually run skills, and keep skill and agent descriptions to one or two sentences.
- Use hooks to filter verbose output, such as showing only failing tests.

**Platform defaults for DevOps**

- Set `model` to sonnet in managed settings, and limit choices with `availableModels` where needed.
- Set `model`, `effort`, `tools` and `maxTurns` on every custom subagent, and use `omitClaudeMd` where project rules are not needed.
- Prefer CLIs over MCP, disable unused servers with `/mcp` because connected servers add context [even when unused](https://code.claude.com/docs/en/best-practices), and install only the plugins and connectors a role needs.
- Leave [`MAX_MCP_OUTPUT_TOKENS`](https://code.claude.com/docs/en/mcp) at its default unless a tool truly needs more.
- Cap unattended runs with `--max-turns` and `--max-budget-usd`, and give every scheduled task or `/loop` a named owner.
- Run `/security-review` only on changes that touch high-risk paths, and set spend limits in the admin console.

### A9. Learning loop details

| Mistake signal | How it is captured |
| --- | --- |
| A reviewer rejects or heavily rewrites agent-made code | `ai-miss` label on the PR, plus a two-line note |
| The scope guard or command guard blocks an action | Hook logs, automatically |
| A brief missed relevant code or pages, or scope had to widen | `capture-learning` at the `/start-ticket` stop |
| Tests from `test-writer` missed a defect found later | Jira bug linked to the PR, `ai-miss` label |
| An agent ignored a rule in REPO\_CONTEXT.md or a skill | `capture-learning` |
| A secret or an injection attempt was found | Security report |
| One task used far more tokens than similar ones | Weekly cost report from telemetry |

| Cause | Where the fix goes |
| --- | --- |
| Missing or wrong project knowledge | `REPO_CONTEXT.md`, root or module |
| The brief pulled the wrong or too little context | Knowledge map entry |
| Skill steps were unclear or missed a check | The skill's `SKILL.md` |
| An agent used the wrong tool or model, or went too far | Agent definition: `tools`, `model`, `maxTurns` |
| An unsafe action was not blocked | Hook or permission rule |
| The ticket lacked acceptance criteria | Jira template, not Claude configuration |

Misses are blameless (principle 6). Prefer the narrowest fix, such as a path-scoped rule or a skill step over a longer REPO\_CONTEXT.md, and don't overfit: a one-off miss may only need a better ticket. Rework a skill or agent with repeated misses, retire one unused for 60 days, and keep secrets out of notes and logs.

### A10. Where knowledge lives

| What | Where | Shared through | Loaded when | Owner | Week |
| --- | --- | --- | --- | --- | --- |
| Organization policy | [Managed settings](https://code.claude.com/docs/en/admin-setup) | Admin console or MDM | Always, and users cannot override it | DevOps, security | 0 |
| Repository map and critical gotchas | Root `REPO_CONTEXT.md`, imported by a one-line `CLAUDE.md` | Git | Every session | Architect | 0 |
| Module conventions | `REPO_CONTEXT.md` in each module folder | Git | [When Claude works in that folder](https://code.claude.com/docs/en/memory) | Module owner | 0 |
| File-type rules | Path-specific sections in module `REPO_CONTEXT.md` files | Git | When matching files are used | Senior developers | 0 |
| Knowledge map | `docs/knowledge-map.yaml` | Git | Only when a ticket starts | Engineering leads | 0 |
| Permissions, hooks, connector config | Managed settings and our plugin's connector configuration | Git | Every session | DevOps, engineering leads | 0 |
| Skills and agents | Private [plugin marketplace](https://code.claude.com/docs/en/plugin-marketplaces) in GitHub | Plugins by role | Skill body only when used | DevOps, champions | 1 to 3 |
| Decisions, designs, ADRs | Confluence | Atlassian connector | Only pages the ticket or map names | Architect | 0 |
| Work context | Jira | Atlassian connector | Only the current ticket and its links | Engineering leads | 0 |
| Personal notes | `CLAUDE.local.md`, auto memory | Not shared | That person's sessions | Each person | Ongoing |

### A11. Agents and skills

`ticket-scout` ships in our plugin. The setup script installs the two editing agents into each user's `~/.claude/agents/`, so they keep their hooks and permission settings without Claude files in service repositories. The pilot uses only `ticket-scout`, `change-implementer`, `test-writer` and `/start-ticket`; the other rows below are backlog, built only if adoption widens after the pilot.

| Agent | Job | Model | Access | Owner | Ships in | When |
| --- | --- | --- | --- | --- | --- | --- |
| `ticket-scout` | Reads the ticket, map, pages and code, and returns the one-page brief | haiku, or sonnet if briefs miss things | Read-only, Atlassian | Engineering leads | core plugin | Pilot |
| `change-implementer` | Makes scoped edits from an approved plan | sonnet | Edit in a worktree, allowed paths only; shell on approval; no network tools | Senior developers | User agent, via setup script | Pilot |
| `test-writer` | Writes tests from acceptance criteria | sonnet | Edit tests in a worktree; shell on approval; no network tools | QA lead | User agent, via setup script | Pilot |
| `failure-triager` | Reads long test and CI logs, returns only the likely cause | haiku | Read-only | QA automation | quality plugin | Backlog |
| `design-reviewer` | Checks a design against the real code and ADRs | opus | Read-only | Architect | design plugin | Backlog |
| `iac-reviewer` | Reviews infrastructure plans for risk and drift | sonnet | Read-only | DevOps lead | platform plugin | Backlog |
| `incident-triager` | Reads logs and alerts, returns likely cause and next checks | sonnet | Read-only, observability | DevOps lead | platform plugin | Backlog |

| Plugin | Skills | Main users | When |
| --- | --- | --- | --- |
| core | `start-ticket`, `capture-learning`, `spec-from-ticket`, `change-workflow`, `pr-summary` | Developers, leads, QA, DevOps | Pilot: start-ticket only; the rest is backlog |
| quality | `test-cases-from-ac` if adoption widens, then `e2e-test-scaffold` and `bug-report` | QA team | Backlog |
| design | `adr-draft`, `threat-model` | Architects, senior developers | Backlog |
| platform | `release-notes`, `runbook`, `incident-review` | DevOps team | Backlog |
| leadership | `delivery-report` | Managers to director | Backlog |

### A12. Autonomy by risk

The `risk` value in the knowledge map sets how far an agent can go.

| Risk | Examples | Agent role | Approval |
| --- | --- | --- | --- |
| Low | Tests, documentation, small fixes | Edits after plan approval | One reviewer |
| Medium | Features and refactors in one module | Edits after plan approval | Code owner |
| High | Payments, authentication, data migrations, infrastructure | Reads and proposes, while a person leads the edit | Code owner plus architect or security |
| Blocked | Secrets, production config, CI workflows, the harness itself | No agent edits | Normal human change process |

### A13. Enablement tracks

| Track | Audience | What it covers | Format |
| --- | --- | --- | --- |
| Foundations | Everyone | What Claude Code can do, our policy, safe data handling, permissions, token habits | 2-hour live session |
| Developer practice | Junior and senior developers | Plan-first work, giving context, tests first, reviewing AI output | Two 2-hour labs and a paired ticket |
| Advanced builder | Seniors, leads, architects, champions | Skills, subagents, hooks, MCP, parallel sessions, headless runs | 1-day workshop |
| Quality engineering | Whole QA team | Tests from acceptance criteria, test generation, flaky-test triage, first automation for manual testers | Two 2-hour labs and a paired test suite |
| Platform | DevOps team | Managed settings, telemetry, infrastructure review, incident triage | 1-day workshop |
| Leading the change | Managers to director | Hands-on demo, adoption measures, cost, supporting teams through role change | 90-minute session, then weekly review |

### A14. Target SDLC by stage

| Stage | How Claude Code helps the owner | The person stays accountable for | Lead |
| --- | --- | --- | --- |
| Plan | Drafts stories, acceptance criteria and impact analysis | Priority, scope and business intent | Engineering lead, manager |
| Design | Maps the codebase, compares options, drafts ADRs and threat models | Architecture decisions and trade-offs | Architect |
| Build | Plans, then implements on a branch, with the owner reviewing each step | Understanding and owning every change | Developers |
| Test | Generates unit, API and end-to-end tests from acceptance criteria, and triages failures | Test strategy, risk coverage and the quality call | QA |
| Review | Helps the author check the diff with `/code-review` and `/security-review` before the PR | Final approval, always by a person | Senior developers, leads |
| Release | Writes release notes and rollout checks | Go or no-go | DevOps, engineering lead |
| Operate | Triages logs and alerts read-only, and drafts incident reviews | Production actions and incident command | DevOps |
| Improve | Suggests dependency and documentation updates when asked | Which lessons go into shared configuration | Engineering leads |

### A15. Risk register

| Risk | Early signal | Mitigation | Owner |
| --- | --- | --- | --- |
| Stability falls as change volume rises | Change failure rate or rework climbs | pr-guard required check, smaller PRs, human review | Engineering director |
| Review becomes the bottleneck | PR wait time grows, seniors report overload | Author-run AI review before the PR, PR size limits, wider reviewer pool | Engineering leads |
| Junior developers stop building judgment | Juniors cannot explain their PRs | Explain-your-PR rule, weekly pairing, hands-on debugging | Senior developers |
| Staff fear for their jobs or quietly resist | Low usage in a team, worried questions in office hours | Early honest message from the director, new career paths, peer champions | Managers |
| Secrets exposed or prompt injection | Secret scan hits, denied access, unexpected tool calls or network access | Deny rules, repository scope list, secret scanning, MCP allowlist, shell approval rules, read-only credentials | Security, DevOps, architect |
| QA is left behind as code output grows | Test backlog grows, escaped defects rise | QA at the spec stage, QA-owned test strategy, automation training | QA lead |
| Spend exceeds budget | Cost per user trends above plan | Spend limits, weekly review, no managed Code Review | Manager |
| Adoption stays shallow | Licences unused, only simple tasks | Protected learning time, champions, leaders using it visibly | Managers |
| Shared knowledge goes stale or has gaps | Agents repeat old mistakes, briefs cite out-of-date pages, frequent scope widening | Named owners and review dates, code-wins rule, map fixes by PR, the learning loop | Architect, engineering leads |
| Licence or IP problems in generated code | Licence scan findings | Licence checks before push, legal guidance | Legal, architect |
| Agent edits drift outside ticket scope | Scope guard blocks, pr-guard failures | Control model, pr-guard required check, allowed paths, stop rules | DevOps lead |
| Thirty days is tight for the setup | Pilot items or knowledge map not ready by the end of Phase 0 | Build only the four pilot items, use the dummy repository, and keep the rest in the backlog | DevOps lead |

### A16. Ownership (RACI)

| Workstream | Accountable | Responsible | Consulted |
| --- | --- | --- | --- |
| Adoption, gate and end-of-pilot decisions | Engineering director | Manager | Steering group |
| Platform and telemetry | DevOps lead | DevOps team | Architect, security |
| Policy and guardrails | Engineering director | Security, DevOps lead | Legal, architect |
| Squad adoption | Manager | Engineering leads, champions | Senior developers |
| Shared configuration | Architect | Senior developers, champions | QA, DevOps |
| Quality gates | QA lead | QA team | Engineering leads |
| Training | Manager | Managers, champions | All role leads |
| Adoption reporting | Manager | Managers, DevOps | Steering group |
| Communication | Engineering director | Managers | HR |

### A17. Phase 0 and pilot checklist

- [ ] Phase 0, day 1: the director confirms sponsorship, the steering group and the message to staff
- [ ] Days 1 to 3: compare the deployment routes and choose one, confirm Atlassian Cloud or Data Center, name the hook on-call engineers, and send security and legal the review pack
- [ ] Days 3 to 5: choose the pilot squad, champions and service, pull the baseline, agree the thresholds, and approve the budget (Gate 1)
- [ ] Weeks 1 to 2: security review, at least one full week, ending with signed-off managed settings
- [ ] Weeks 1 to 2: legal review, at least one full week, ending with approval for the service repository
- [ ] Weeks 1 to 3: on the dummy repository, build `pr-guard`, the four pilot items and the shim, and set up WSL2 with the sandbox on
- [ ] Week 3: deploy the signed-off managed settings and the pinned version, turn on `pr-guard`, branch protection and CODEOWNERS on the pilot repository, and send OpenTelemetry to Azure Monitor
- [ ] Week 3: write `REPO_CONTEXT.md` and the knowledge map entries for the pilot service
- [ ] End of week 3: test every control with a deliberate bad edit, run Foundations, and review Gate 2
- [ ] Pilot weeks 1 to 2 (weeks 4 to 5): start on the dummy repository, move to low-risk real tickets once legal approves, and hold the weekly steering review
- [ ] Pilot week 3 (week 6): mid-pilot review (Gate 3) decides whether to continue, adjust the scope or stop
- [ ] Pilot weeks 4 to 6 (weeks 7 to 9): add medium-risk tickets, with weekly reviews and the learning loop
- [ ] Week 10: one-page results, the end-of-pilot review and a retro

### A18. Enabling branch protection and CODEOWNERS

These GitHub settings make the human gate and the pr-guard required check enforceable: no change reaches the default branch without a PR approved by a senior reviewer, and sensitive paths need their owners' approval. A repository admin sets them up in Phase 0 on the dummy and pilot repositories. GitHub's current mechanism is [rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets), which work alongside older branch protection rules, with the most restrictive rule applying. On private repositories these features need a paid GitHub plan, so confirm ours supports them.

**1. Create a branch ruleset (repository admin)**

1. In the repository, open **Settings**, then **Rules** > **Rulesets** in the sidebar, then **New ruleset** > **New branch ruleset**.
2. Name it, for example `protect-main`, and set the enforcement status to **Active**.
3. Under **Target branches**, add the default branch.
4. Under **Bypass list**, add nobody, or only a named admin team for emergencies. Never add the accounts or GitHub Apps that Claude Code uses.
5. Under **Branch protections**, turn on:
   - **Restrict deletions** and **Block force pushes**.
   - **Require a pull request before merging**, with at least 1 required approval, **Dismiss stale pull request approvals when new commits are pushed**, **Require approval of the most recent reviewable push**, review from Code Owners, and resolved conversations.
   - **Require status checks to pass before merging**, with `pr-guard` as the required check. Pre-push checks stay as early local feedback only.
6. Click **Create**. A direct push to the default branch should now be rejected.

An organization owner can instead create one ruleset that targets all pilot repositories. GitHub's pages differ on whether this needs the Team or the Enterprise plan, so check with our GitHub admin.

**2. Add a CODEOWNERS file (repository owner)**

Create `.github/CODEOWNERS` and merge it to the default branch through a PR, since GitHub reads [the file on the PR's base branch](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners). This example is illustrative:

```text
# Owners are reviewer teams: senior developers, leads and architects only, never juniors
# Broad rule first: the last matching pattern wins
*                     @our-org/platform-reviewers

# Sensitive areas need their own reviewers
/services/payments/   @our-org/payments-reviewers
/services/auth/       @our-org/security-reviewers
/infrastructure/      @our-org/devops-leads

# Shared context, the knowledge map and CI (including pr-guard), which agents never edit
/CLAUDE.md                @our-org/devops-leads @our-org/architects
/REPO_CONTEXT.md          @our-org/architects
/docs/knowledge-map.yaml  @our-org/architects
/.github/             @our-org/devops-leads
```

- Put broad rules first and specific paths after, because the last matching pattern takes precedence.
- Owners must be people or visible teams with write access to the repository.
- Keep owners in step with the knowledge map's `owners`, `protected_paths` and `risk` values (A4, A12).

**Keep approval and merge with senior reviewers**

GitHub lets anyone with write access approve and merge, so without these steps a junior could approve a teammate's PR in the same team.

- Create reviewer teams (Organization > Teams) that hold only senior developers, leads and architects, for example `payments-reviewers`. Juniors stay in the delivery team (for example `team-payments`) for write access but never join a reviewer team.
- Name only reviewer teams in CODEOWNERS, as above. Because `*` gives every file an owner and the ruleset requires Code Owner review, every PR needs an approval from a reviewer team. A junior's approval shows on the PR but does not unblock it, and authors can never approve their own PR.
- Keep **Require approval of the most recent reviewable push** on, so no one can add commits after a senior approves and merge them unreviewed.
- If only seniors should click **Merge** as well, add a classic branch protection rule on the default branch with **Restrict who can push to matching branches**, listing the reviewer teams. It applies on top of the ruleset, and needs an organization repository on the Team plan or above. GitHub's pages do not say outright that it blocks the Merge button, so confirm that in the test below. Do not use the ruleset bypass list for this, because bypass actors can merge without the required approvals.
- Repository admins can push past classic rules, so keep the Admin role to a few named people and never give it to juniors.

**3. Test it (Phase 0, with the harness test)**

- A direct push to the default branch is rejected.
- A PR cannot merge without an approval, and a new push after approval needs approval again.
- A PR touching `/services/payments/` automatically requests the payments owners and stays blocked until they approve.
- A PR changing the knowledge map, `CLAUDE.md` or `.github/` needs architect or DevOps approval.
- A junior developer's approval leaves the PR blocked, and an approval from the reviewer team unblocks it.
- If merge is restricted to reviewers, a junior cannot merge a PR even after a senior approves it.

### Sources

Documentation read on 6 November 2026. Features, prices and setting names change, so recheck them at setup.

- [Set up Claude Code for your organization](https://code.claude.com/docs/en/admin-setup)
- [Claude Code settings reference](https://code.claude.com/docs/en/settings-reference)
- [Claude Code advanced setup](https://code.claude.com/docs/en/setup)
- [Claude Code monitoring](https://code.claude.com/docs/en/monitoring-usage)
- [Claude Code analytics](https://code.claude.com/docs/en/analytics)
- [Manage costs effectively](https://code.claude.com/docs/en/costs)
- [Claude Code data usage](https://code.claude.com/docs/en/data-usage)
- [Claude Code best practices](https://code.claude.com/docs/en/best-practices)
- [How Claude Code works in large codebases](https://claude.com/resources/articles/how-claude-code-works-in-large-codebases-best-practices-and-where-to-start)
- [Claude Code memory](https://code.claude.com/docs/en/memory)
- [Create custom subagents](https://code.claude.com/docs/en/sub-agents)
- [Agent teams](https://code.claude.com/docs/en/agent-teams)
- [Extend Claude with skills](https://code.claude.com/docs/en/skills)
- [Plugin marketplaces](https://code.claude.com/docs/en/plugin-marketplaces)
- [Code intelligence plugins](https://code.claude.com/docs/en/plugins/code-intelligence)
- [Connect Claude Code to tools via MCP](https://code.claude.com/docs/en/mcp)
- [Model configuration](https://code.claude.com/docs/en/model-config)
- [Claude Code commands](https://code.claude.com/docs/en/commands)
- [Hooks reference](https://code.claude.com/docs/en/hooks)
- [Permissions](https://code.claude.com/docs/en/permissions)
- [Sandboxing](https://code.claude.com/docs/en/sandboxing)
- [Checkpointing](https://code.claude.com/docs/en/checkpointing)
- [Automated security reviews](https://support.claude.com/en/articles/11932705-automated-security-reviews-in-claude-code)

* [GitHub: About rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets)
* [GitHub: Creating rulesets for a repository](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/creating-rulesets-for-a-repository)
* [GitHub: Available rules for rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets)
* [GitHub: About code owners](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-code-owners)

**Exact references for figures in this plan** (checked 13 November 2026):

- About $13 per developer per active day and $150-250 per developer per month: [Manage costs effectively](https://code.claude.com/docs/en/costs), opening paragraph before "Track your costs".
- Agent teams use about 7x more tokens when teammates run in plan mode: same page, "Reduce token usage" > "Manage agent team costs".
- The prompt cache lasts about an hour on a subscription and five minutes on an API key, cloud provider or usage credits: [Best practices](https://code.claude.com/docs/en/best-practices), "Change a default" > "Keep cost and context down".
- Installing a specific version, `autoUpdatesChannel` and `DISABLE_AUTOUPDATER`: [Set up Claude Code](https://code.claude.com/docs/en/setup), "Install a specific version", "Configure release channel" and "Disable auto-updates".
