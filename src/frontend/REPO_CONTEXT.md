# Frontend: repository context

Angular app for the Mini WMS. Structure decisions are in `docs/adr/ADR-006-angular-structure.md`.
Created by TASK-007.

## Versions (as scaffolded, 2026-11-19)
| Item | Version |
|---|---|
| Angular (`@angular/*`) | 22.3.0 |
| Angular CLI / `@angular/build` | 22.3.0 |
| Angular Material / CDK | 22.3.0 |
| TypeScript | ~6.0.2 |
| RxJS | ~7.8.0 |
| Node used | 24.22.3 (CLI 22.3 needs ^22.22.3, ^24.15.0 or >=26) |
| npm used | 11.19.1 |

## Test runner
The CLI default: Vitest 5 through the `@angular/build:unit-test` builder, with jsdom as the DOM.
It runs headless, so no browser is needed. jsdom is a test library, not a UI library.
Test names start with the AC ID (ADR-005), for example `TASK-007.1 app component is created`.

## Setup
- Standalone components only, no NgModules. SCSS styles, routing on, no SSR, zone-based defaults of the CLI.
- Strict: `strict: true` in `tsconfig.json`; `strictTemplates`, `strictInjectionParameters` and
  `strictInputAccessModifiers` in `angularCompilerOptions`.
- Angular Material with the default Material 3 theme (`src/styles.scss`). No other UI or state library
  (no NgRx, Bootstrap and so on).

## Folder layout (`src/app/`)
- `core/`: the HTTP layer (TASK-009). `api/` has `ApiClient` (prefixes `API_BASE_PATH` `/api/v1`, relative paths only)
  and `ApiError`; `auth/` has the in-memory `AuthSession` signal and `authInterceptor`; `errors/` has `errorInterceptor`
  (problem+json to `ApiError`, always rethrown) and `ErrorNotifier` (snackbar for 5xx and network only).
  `core/layout/` holds the shell (`shell.ts`: top bar "Mini WMS" and fixed "Dev Admin", left nav,
  `<main id="main-content">`, skip link) and `not-found.page.ts`
- `shared/`: reusable components, pipes, directives; `shared/forms/apply-server-errors.ts` sets `server` errors on controls
- `features/`: one folder per module with `pages/`, `components/`, `data/` and `<module>.routes.ts`.
  For now each holds only `pages/<module>-placeholder.page.ts` (an h1 with the title).
- `app.ts`, `app.config.ts`, `app.routes.ts`, `app.html`: root component, providers and routes.
  `app.html` is only `<router-outlet />`; the shell is the component of the parent route `''`.

## Routes (all children of the shell, each lazy via `loadChildren`, default export)
| URL | Feature folder | Title / nav |
|---|---|---|
| `/` | redirect to `/stock` | |
| `/stock` | `inventory` | Stock (nav 1) |
| `/receipts` | `inbound` | Receipts (nav 2) |
| `/orders` | `outbound` | Orders (nav 3) |
| `/products` | `products` | Products (nav 4) |
| `/locations` | `locations` | Locations (nav 5) |
| `/sign-in` | `auth` | Sign in (no nav item) |
| `**` | `core/layout/not-found.page.ts` | Page not found, inside the shell |

Global `:focus-visible` outline and the skip-link style live in `src/styles.scss` (NFR-6).

## Commands (run in `src/frontend`)
- Install exactly from the lockfile: `npm ci`
- Dev server: `npm start` (http://localhost:4200)
- Production build: `npm run build` (output in `dist/`)
- Tests, once and headless, exits when done: `npm test`
- Watch build: `npm run watch`

## Not done yet
- Dev-server proxy for `/api` is TASK-011 (until then `/api/v1` is not served by `npm start`).
- Coverage gate is TASK-010. Real module screens come with their own tickets.

## Lockfile
`package-lock.json` is committed and is the source of truth for versions; use `npm ci`, not `npm install`.
It is large by nature (every transitive package). pr-guard reads its `license` fields and rejects
GPL, AGPL, SSPL, BUSL and Commons-Clause. When you change dependencies, run `npm install` once and commit
the lockfile in the same PR.

## Gotchas
- Tools may not be on PATH in agent shells: add `C:\Program Files\nodejs` first.
- npm 11 warns that 4 packages have install scripts not covered by `allowScripts`
  (esbuild, lmdb, @parcel/watcher, msgpackr-extract). Build and tests work without running them.
- `node_modules`, `dist` and `.angular` are git-ignored.
