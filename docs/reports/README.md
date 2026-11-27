# Delivery reports

`docs/reports/<TICKET-ID>/` holds each ticket's reports. When the full pipeline runs, Prompt 5 §7-8 also files the story summary, QA mapping table and helper scripts there
for the ticket (replaces the Jira attachment step). They are committed in the ticket's PR, so the ticket's `allowed_paths`
must include `docs/reports/<TICKET-ID>/**`. Raw coverage output stays in the git-ignored `TestResults/`.

Every ticket also gets `docs/reports/<TICKET-ID>/ac-results.md` (one row per acceptance criterion: PASS, FAIL, N/A or NOT RUN, with the proving test and evidence) and one row in the roll-up `ac-log.md`.

From 2026-11-27 `/start-ticket` and `/review-ticket` also save what they show at the owner stops (copies of the chat, nothing edited):

| File | Written at | Holds |
|---|---|---|
| `ticket_plan.md` | Stop 1 (`/start-ticket` step 6), updated after the owner answers | The proposed plan, then its status: proposed, approved or rejected, with the date |
| `ac-results.md` | After the build | One row per acceptance criterion |
| `code_summary.md` | Stop 2 (end of the review chain) | Changed files, the implementer and test-writer reports, build and test results, AC totals |
| `review_findings.md` | Stop 2; a `/review-ticket` rerun overwrites it | Each review agent's report unchanged, the summary table, the stop notices |

Tickets before TASK-019 have only `ac-results.md`.
