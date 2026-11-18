---
id: TASK-005
type: task
title: Locations and Products schema, first migration and warehouse seed
status: draft
owner: shivansh-gupta-ness
component: backend-platform
risk: low
allowed_paths: [src/backend/**, tickets/TASK-005.md, docs/reports/TASK-005/**, docs/reports/ac-log.md]
docs: [docs/specs/data-model.md, docs/adr/ADR-002-ef-core-sqlite.md]
parent: 
depends_on: [TASK-004]
---

## Description
Add the entities, EF configurations and the first migration for the first two modules: Warehouse, Zone, Bin and Product, plus the single seeded warehouse. Set `status: ready` after TASK-004 is merged.

## Acceptance criteria
1. Given an empty database, when the migrations are applied, then tables for Warehouse, Zone, Bin and Product exist with the columns, lengths, keys and constraints in `docs/specs/data-model.md`.
2. Given a fresh database, when the app starts, then exactly one warehouse `MAIN` named `Main warehouse` exists, and starting again does not add a second.
3. Given two zone codes, bin codes or product SKUs that differ only by case, when the second is saved, then the database rejects it (case-insensitive unique index).
4. Given a bin that points at a missing zone, when it is saved, then the database rejects it (foreign key).
5. Given a zone whose type is not Receiving, Storage or Shipping, when it is saved, then it is rejected.
6. Given the entities, when business rules are tested, then the invariants for zone type, active flags and reorder level in the data model are enforced in Domain, with unit tests.
7. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-005.<n>")]` on those proving the ACs above.

## Out of scope
- Endpoints or use cases for Locations and Products (module tickets).
- Tables for later modules.

## Notes and links
- Record AC results in `docs/reports/TASK-005/ac-results.md` and one row in `docs/reports/ac-log.md`.
