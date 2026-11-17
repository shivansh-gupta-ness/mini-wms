# Tickets (local replacement for Jira)

One markdown file per ticket, named by ID: `FEAT-001.md`, `BUG-001.md`, `TASK-001.md`.
IDs count up per type. Copy the matching file from `_templates/` and fill every field.

| Field | Rule |
|---|---|
| `owner` | A named person. Required. pr-guard fails without it |
| `component` | A key from `docs/knowledge-map.yaml` |
| `risk` | low, medium, high or blocked (matches the component) |
| `allowed_paths` | Flow-style list of globs the change may touch, e.g. `[src/backend/**/Inventory/**]` |
| `docs` | Spec or ADR files under `docs/` the ticket relies on |
| `status` | draft, ready, in-progress, in-review, done |

A ticket without acceptance criteria is not `ready`. Write them as Given / When / Then so tests can be derived.
The ticket file is the only source of work state; the branch name carries the ID (`feat/FEAT-001-add-stock`).
