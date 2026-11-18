---
id: TASK-003
type: task
title: Angular app skeleton
status: ready
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-003.md, docs/reports/TASK-003/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-006-angular-structure.md, docs/adr/ADR-005-testing-coverage-and-ac-log.md, docs/specs/ui.md]
parent: 
depends_on: []
---

## Description
Create the Angular app in `src/frontend` as ADR-006 describes: standalone components, strict TypeScript, the folder layout, Angular Material, the application shell with navigation, the HTTP interceptors and the test runner. No module screens yet. Record the Angular and CLI versions used and which test runner the CLI chose, in `src/frontend/REPO_CONTEXT.md`.

## Acceptance criteria
1. Given `src/frontend`, when `npm ci` and `npm run build` run, then the build succeeds with no errors and strict TypeScript and strict templates are on.
2. Given the folder layout in ADR-006, when the app is opened, then `core/`, `shared/` and `features/` exist under `src/app/`, with an empty lazy-loaded route for each of locations, products, inventory, inbound, outbound and auth.
3. Given the app is running, when it loads, then the shell shows the top bar, the left navigation in the order from `docs/specs/ui.md` section 1 (only for modules that have a route) and a content area.
4. Given an HTTP response with `application/problem+json`, when the error interceptor handles it, then it produces a typed `ApiError` carrying `status`, `title`, `detail` and the field `errors`, and a unit test proves it.
5. Given a request, when it is sent, then the base path `/api/v1` is used and, once a session exists, the token interceptor adds the bearer token (a test proves both).
6. Given the API returns field errors, when a form helper applies them, then each message appears on its matching form control, and a unit test proves it.
7. Given `npm test`, when it runs, then all tests pass, coverage is reported, and each test that proves an AC above starts with its ID, for example `it('TASK-003.4 maps problem+json to ApiError', ...)`.
8. Given the shell, when it is navigated with the keyboard only, then every navigation item and button is reachable and shows a visible focus, and every control has a label (NFR-6).
9. Given `src/frontend/REPO_CONTEXT.md`, when read, then it lists the Angular and CLI versions, the test runner, the folder layout, and the build, test and run commands (under 80 lines).

## Out of scope
- Any module screen (Locations, Products and so on).
- Talking to the real API (TASK-004 does the proxy and the health indicator).
- End-to-end browser tests.

## Notes and links
- `docs/adr/ADR-006-angular-structure.md`, `docs/specs/ui.md`.
- Create the app with `npx @angular/cli@<current stable> new`, then add Angular Material. Do not add other UI or state libraries.
- Record AC results in `docs/reports/TASK-003/ac-results.md` and one row in `docs/reports/ac-log.md`.
