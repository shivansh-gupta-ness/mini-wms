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
- `core/`: API client, auth session, error handling, layout shell (empty for now)
- `shared/`: reusable components, pipes, directives (empty for now)
- `features/`: one folder per module with `pages/`, `components/`, `data/` and `<module>.routes.ts` (empty for now)
- `app.ts`, `app.config.ts`, `app.routes.ts`, `app.html`: root component, providers and routes.
  `app.html` is still the CLI placeholder page; the shell replaces it in TASK-008.

## Commands (run in `src/frontend`)
- Install exactly from the lockfile: `npm ci`
- Dev server: `npm start` (http://localhost:4200)
- Production build: `npm run build` (output in `dist/`)
- Tests, once and headless, exits when done: `npm test`
- Watch build: `npm run watch`

## Not done yet
- Dev-server proxy for `/api` is TASK-011; the HTTP layer is TASK-009.
- Coverage gate is TASK-010. Shell and navigation are TASK-008.

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
