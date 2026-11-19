# Delivery reports

`docs/reports/<TICKET-ID>/` holds the `ac-results.md` of each ticket (so far the only file produced). When the full pipeline runs, Prompt 5 §7-8 also files the story summary, QA mapping table and helper scripts there
for the ticket (replaces the Jira attachment step). They are committed in the ticket's PR, so the ticket's `allowed_paths`
must include `docs/reports/<TICKET-ID>/**`. Raw coverage output stays in the git-ignored `TestResults/`.

Every ticket also gets `docs/reports/<TICKET-ID>/ac-results.md` (one row per acceptance criterion: PASS, FAIL, N/A or NOT RUN, with the proving test and evidence) and one row in the roll-up `ac-log.md`.
