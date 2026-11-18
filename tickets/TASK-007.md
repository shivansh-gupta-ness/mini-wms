---
id: TASK-007
type: task
title: Angular app creation, strict configuration and Material
status: ready
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-007.md, docs/reports/TASK-007/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-006-angular-structure.md]
parent: 
depends_on: []
---

## Description
Create the Angular app in `src/frontend` as ADR-006 describes, with strict settings, Angular Material and the folder layout. Record the Angular and CLI versions and the test runner the CLI chose in `src/frontend/REPO_CONTEXT.md`.

## Acceptance criteria
1. Given `src/frontend`, when `npm ci` and `npm run build` run, then the build succeeds with no errors, with strict TypeScript and strict templates on.
2. Given `src/frontend/package.json`, when read, then Angular Material is a dependency and no other UI or state library is.
3. Given the folder layout in ADR-006, when the app is opened, then `core/`, `shared/` and `features/` exist under `src/app/` and standalone components are used (no NgModules).
4. Given `npm test`, when it runs, then a first smoke test passes and its name starts with `TASK-007.<n>`.
5. Given the app is started with `npm start`, when it loads in a browser, then the default page renders without console errors.
6. Given `src/frontend/REPO_CONTEXT.md`, when read, then it lists the Angular and CLI versions, the test runner, the folder layout and the build, test and run commands (under 80 lines).

## Out of scope
- The shell and navigation (TASK-008).
- HTTP layer (TASK-009).
- Coverage gate (TASK-010).

## Notes and links
- Record AC results in `docs/reports/TASK-007/ac-results.md` and one row in `docs/reports/ac-log.md`.
