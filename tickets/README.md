# Tickets (local replacement for Jira)

One markdown file per ticket, named by ID: `FEAT-001.md`, `BUG-001.md`, `TASK-001.md`.
IDs count up per type. Copy the matching file from `_templates/` and fill every field.

| Field | Rule |
|---|---|
| `owner` | A named person. Required. pr-guard fails without it |
| `component` | A key from `docs/knowledge-map.yaml` |
| `risk` | low, medium, high or blocked (matches the component) |
| `allowed_paths` | Flow-style list of globs the change may touch, e.g. `[src/backend/**/Inventory/**]`. Feature tickets run through `/run-feature-*` must also list `tickets/<ID>.md`, `docs/reports/<ID>/**` and `docs/reports/ac-log.md` (the pipeline appends a `## Delivery` section, files reports and adds an AC-log row) |
| `docs` | Spec or ADR files under `docs/` the ticket relies on |
| `status` | draft, ready, in-progress, in-review, done |

A ticket without acceptance criteria is not `ready`. Write them as Given / When / Then so tests can be derived.
The ticket file is the only source of work state; the branch name carries the ID (`feat/FEAT-001-add-stock`).

## Features that span repositories or layers
Create one ticket per repository (or per layer, e.g. backend and frontend), each with its own owner, component, risk and `allowed_paths`. Link them with `parent:` (the feature ticket) and `depends_on: [FEAT-001]` (for example the frontend ticket depends on the backend ticket that publishes the API contract in `docs/specs/`). An agent works in one repository at a time, so each repository's scope guard stays valid. Use `allowed_paths` that include `docs/reports/<ID>/**` when the feature pipeline will file reports.
