---
id: TASK-006
type: task
title: API building blocks: paging, sorting, validation and error mapping
status: draft
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
5. Given a domain exception for a business-rule conflict, when it is thrown, then the response is 409 `application/problem+json` with `type` `conflict` and the exception message as `detail`.
6. Given a stale `version` in an update, when the concurrency exception occurs, then the response is 409 with `type` `conflict`.
7. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-006.<n>")]` on those proving the ACs above.

## Out of scope
- Any module endpoint.
- Authorization.

## Notes and links
- Record AC results in `docs/reports/TASK-006/ac-results.md` and one row in `docs/reports/ac-log.md`.
