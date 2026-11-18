---
id: TASK-012
type: task
title: Lint the OpenAPI contracts
status: ready
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
