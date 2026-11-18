# ADR-006: Angular frontend structure

Status: Proposed (accepted when merged)
Date: 2026-11-18
Owner: shivansh-gupta-ness

## Context
The frontend is an Angular app in `src/frontend` that talks only to the API in `docs/specs/api/`. Tickets for it are separate from backend tickets and depend on the published contract. The app runs locally only.

## Decision
- **Angular current stable at scaffold time**, created with the Angular CLI (`npx @angular/cli@<version> new`), standalone components, no NgModules, strict TypeScript and strict templates. TASK-003 records the exact version.
- **Folder layout** (module-first, to match ADR-001 and the knowledge map):
  ```
  src/frontend/src/app/
    core/        api client, auth session, error handling, layout shell
    shared/      reusable components (table, form field, confirm dialog), pipes, directives
    features/
      locations/ products/ inventory/ inbound/ outbound/ auth/
        pages/ components/ data/ (typed API services and models)  <module>.routes.ts
  ```
- **Routing:** lazy-loaded route per feature (`loadChildren`), a route guard per policy once Auth exists.
- **State:** component-level signals and small injectable services; no global state library in version 1.
- **API access:** one typed service per module in `data/`, models hand-written to match the OpenAPI YAML (a later ticket can generate them). All calls go through one `HttpClient` interceptor that adds the token, and one that maps `application/problem+json` to a typed `ApiError`.
- **Forms:** reactive forms. A field-level `errors` object from the API (ADR-003) is applied to the matching form controls, so server and client messages appear in the same place.
- **UI library:** Angular Material, for accessible controls (NFR-6). Every control has a visible label or `aria-label`; all workflows are keyboard-usable.
- **Role-aware UI:** controls the user may not use are hidden or disabled (AUT-3.2). This is a convenience only; the server enforces permissions.
- **Config:** the API base URL is `/api/v1`, reached through the Angular dev-server proxy to the local API, so no CORS or environment URLs are hard-coded.
- **Tests:** per ADR-005; AC IDs at the start of test names.
- **Style:** the CLI's default linter and formatter configuration; `npm run build` must pass with no errors.

## Consequences
- A frontend ticket only needs `src/frontend/**` plus the contract in `docs/specs/api/`.
- Hand-written models can drift from the contract; each module's frontend ticket re-checks them against the YAML (and a generated-client ticket may replace this later).
- Angular Material adds a dependency but removes the need to build accessible widgets.
