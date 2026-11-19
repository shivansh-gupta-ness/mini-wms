---
id: TASK-006
type: task
title: API building blocks: paging, sorting, validation and error mapping
status: ready
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, tickets/TASK-006.md, docs/reports/TASK-006/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [TASK-002, TASK-004]
---

## Description
Build the shared pieces every module endpoint will use, once: paged and sorted list handling, the validation pipeline, and mapping of domain exceptions and stale versions to the standard error format. Set `status: ready` after TASK-002 and TASK-004 are merged.

## Acceptance criteria
1. Given a list query, when `page` and `pageSize` are omitted, then page 1 with 25 items is used; a `pageSize` above 100 is rejected with a field-level 400.
2. Given `sort=name` or `sort=-name`, when a list is built, then it is ordered ascending or descending by an allowed field; an unknown field is rejected with a field-level 400.
3. Given a list result, when it is serialised, then it has `items`, `page`, `pageSize` and `totalItems` exactly as in ADR-003.
4. Given a command with FluentValidation rules, when it is invalid, then the response is 400 `application/problem+json` with an `errors` object keyed by camelCase field name.
5. Given a domain exception for a business-rule conflict, when it is thrown, then the response is 409 `application/problem+json` with `type` `conflict` and the domain exception's message as `detail`. Any other exception still gets no `detail` (BUG-001 behaviour kept for 5xx and for unexpected exceptions).
6. Given a stale `version` in an update, when the concurrency exception occurs, then the response is 409 with `type` `conflict`.
7. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-006.<n>")]` on those proving the ACs above.

## Out of scope
- Any module endpoint.
- Authorization.

## Notes and links
- **Decision (owner, 2026-11-19): resolved with the suggested option below.** Part of this ticket: change the BUG-001 customizer in `Program.cs` so `detail` is kept only when it comes from a domain exception on a 4xx, adjust the `BUG-001.6`/`BUG-001.7` allow-list tests to match (add `detail` for that case only), and keep the rest of BUG-001 unchanged. ADR-003 is already updated. Original finding: AC 5 expects a business-rule 409 to carry the exception message as `detail` (the UI spec shows the 409 `detail` in a banner, `docs/specs/ui.md` section 2). BUG-001 (merged) clears `detail` on every error body in `Program.cs` and its tests (`BUG-001.6`, `BUG-001.7`) require no `detail` and an allow-list of `type`, `title`, `status`, `traceId` and `errors`. Both cannot hold. Suggested resolution: keep `detail` only for 4xx responses whose text is written by our own code (validation and business rules), never for 5xx and never for exception text; then adjust the BUG-001 customizer and its allow-list test accordingly and update ADR-003, which still says every error has `detail`.
- BUG-001 already delivers the problem `type` by status and the `traceId`; this ticket adds the shared pieces on top (paging, sorting, validation, domain exceptions), so AC 4 to 6 should reuse that customizer, not replace it.
- Record AC results in `docs/reports/TASK-006/ac-results.md` and one row in `docs/reports/ac-log.md`.
