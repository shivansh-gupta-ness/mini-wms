# Frontend: repository context

Angular app for the Mini WMS (TASK-007). Structure decisions are in `docs/adr/ADR-006-angular-structure.md`.

## Versions (as scaffolded, 2026-11-19)
Angular, Angular CLI / `@angular/build`, Material and CDK: 22.3.0. TypeScript ~6.0.2, RxJS ~7.8.0. Node used 24.22.3, npm 11.19.1.

## Test runner
The CLI default: Vitest 5 through the `@angular/build:unit-test` builder, with jsdom as the DOM.
It runs headless, so no browser is needed. jsdom and `@vitest/coverage-v8` (same version as Vitest) are test libraries.
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
  Only `locations` is built; the other modules hold `pages/<module>-placeholder.page.ts` (an h1 with the title).
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
- Tests, once and headless, exits with pass/fail status: `npm test`
- Only the tests of one AC (same quoting in bash, PowerShell and cmd): `npm test -- --filter "\bTASK-010\.1\b"`
- Tests with coverage, then the gate: `npm run test:coverage` (text table, report in git-ignored `coverage/mini-wms/`)
- Gate alone: `node scripts/coverage-gate.mjs` (reads `coverage-summary.json`; `--summary <file>`, `--threshold <n>`).
  Line coverage of `src/app` (specs and `.d.ts` excluded) must be at least 90%, else it exits 1 and names the shortfall;
  a missing summary also exits 1. Fail example: `node scripts/coverage-gate.mjs --threshold 101`
- Gate script tests (`node --test`, fixture summaries, not part of `npm test`): `npm run test:gate`
- Watch build: `npm run watch`

## Run the API and the frontend together (TASK-011)
- API: `dotnet run --project src/backend/MiniWms.Api` (http://localhost:5080)
- Frontend: `npm start` (http://localhost:4200) proxies `/api/` through `proxy.conf.json` (dev only; `ng build` does not use it).
- The top bar shows `API: ok` or `API: unavailable` (health check every 30 s, `core/layout/api-status.ts`).

## Locations screens (FEAT-001, `features/locations/`)
- `pages/locations.page.ts` (header, Rename, Tree and List tabs, `reload()`); `components/` (`location-tree`, `bin-list`, the rename, zone and bin dialogs); `data/` (`LocationsApi` over `ApiClient`, models). Shared parts: `confirm-dialog`, `status-chip`, `empty-state`, `error-banner`.
- Forms are Material dialogs; field errors use `applyServerErrors`, a stale-version 409 shows a banner with Reload. The real-browser check is not run.

## Lockfile
`package-lock.json` is committed and is the source of truth for versions; use `npm ci`, not `npm install`.
It is large by nature (every transitive package). pr-guard reads its `license` fields and rejects
GPL, AGPL, SSPL, BUSL and Commons-Clause. When you change dependencies, run `npm install` once and commit
the lockfile in the same PR.

## Gotchas
- Tools may not be on PATH in agent shells: add `C:\Program Files\nodejs` first.
- npm 11 warns about install scripts not covered by `allowScripts` (esbuild, lmdb and others); not needed to build or test.
- `node_modules`, `dist`, `.angular` and `coverage` are git-ignored.
