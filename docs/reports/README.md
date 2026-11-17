# Delivery reports

`docs/reports/<TICKET-ID>/` holds the story summary, QA mapping table, AC verification report and helper scripts that Prompt 5 §7-8
files for each ticket (replaces the Jira attachment step). They are committed in the ticket's PR, so the ticket's `allowed_paths`
must include `docs/reports/<TICKET-ID>/**`. Raw coverage output stays in the git-ignored `TestResults/`.