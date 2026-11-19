---
id: TASK-012
type: task
title: Lint the OpenAPI contracts
status: done
owner: shivansh-gupta-ness
component: api-contracts
risk: low
allowed_paths: [docs/specs/api/**, .spectral.yaml, tickets/TASK-012.md, docs/reports/TASK-012/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: []
---

## Description
The contracts `docs/specs/api/locations.yaml` and `products.yaml` have not been validated by a tool. Lint them with Spectral, fix what it finds, and add rules that keep every later contract in line with ADR-003.

## Acceptance criteria
1. Given `.spectral.yaml` at the repository root, when `npx @stoplight/spectral-cli@<pinned version> lint "docs/specs/api/*.yaml"` runs, then it reports zero errors and zero warnings on both contracts.
2. Given an operation without `x-acceptance-criteria`, when linted, then a custom rule reports it.
3. Given an error response, when linted, then a rule requires the `application/problem+json` content type.
4. Given a list operation, when linted, then a rule requires the `page`, `pageSize` and `sort` parameters.
5. Given each LOC and PRD acceptance criterion in `docs/specs/product.md`, when compared with the contracts, then each appears in an `x-acceptance-criteria` list (record the check in the report).
6. Given the lint command, when read in `docs/specs/api/README.md`, then it is documented with the pinned version.

## Out of scope
- Generating clients from the contracts.
- Contracts for later modules.

## Notes and links
- Record AC results in `docs/reports/TASK-012/ac-results.md` and one row in `docs/reports/ac-log.md`.
- Owner decisions (2026-11-19, on the scout's brief, recommended options taken): the npm registry may be used for this ticket only (`npm view @stoplight/spectral-cli version` to pin, and `npx @stoplight/spectral-cli@<pinned>` to lint); default Spectral warnings are fixed in the YAML (tags, descriptions) and only rules that mean nothing for this project (for example contact information) are disabled, each with a written reason in `.spectral.yaml`; list operations are detected with a filter on the 200 response having `items` first, with a small function file under `docs/specs/api/tools/` as the fallback; the `Problem` schema gains `traceId` (required) and a tighter `detail` description to match the backend (a contract change, noted in the PR); the health endpoint, a shared 500 response and 401/403 stay out of the contracts for now; the AC 5 check is a small Node script that compares `docs/specs/product.md` with the `x-acceptance-criteria` lists and prints the table for the report; rule tests are fixture contracts that must fail, with a script that checks the expected rule codes, under `docs/specs/api/tests/`. CI wiring of Spectral is out of scope (`.github/` is protected).

## Delivery
Delivered in PR 32. All 6 criteria PASS; results in `docs/reports/TASK-012/ac-results.md`, the row in `docs/reports/ac-log.md`.
