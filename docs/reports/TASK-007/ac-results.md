# TASK-007 acceptance-criteria results

Ticket: `tickets/TASK-007.md` (Angular app creation, strict configuration and Material). Branch: `task/TASK-007-angular-app-strict-material`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (decisions recorded in the ticket, including the npm-registry exception for this ticket), `change-implementer` and `test-writer`, each in an isolated worktree, work carried back with `worktree-apply.mjs`. Numbers come from my own runs in the main working tree, except AC 5 (see its row).

| Command (in `src/frontend`) | Result |
|---|---|
| `npm ci` | Succeeded (4 packages with install scripts not covered by `allowScripts` are warned about; build and tests work) |
| `npm run build` | Succeeded, no errors or warnings; initial bundle 226 kB |
| `npm test` (run twice) | Both runs: Test Files 2 passed (2), Tests 16 passed (16) |

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | `npm ci` and `npm run build` succeed with strict TypeScript and strict templates | PASS | 6 tests tagged `TASK-007.1` (`strict`, `strictTemplates`, `strictInjectionParameters`, `strictInputAccessModifiers`, scripts present, `test` is non-watch) | Real `npm ci` and `npm run build` succeeded; tests passed | 2026-11-19 |
| 2 | Angular Material is a dependency and no other UI or state library is | PASS | 2 tests (`TASK-007.2`) | Tests passed; `@angular/material` and `@angular/cdk` 22.3.0 only | 2026-11-19 |
| 3 | `core/`, `shared/`, `features/` exist under `src/app/`, standalone components, no NgModules | PASS | 3 tests (`TASK-007.3`) | Tests passed | 2026-11-19 |
| 4 | `npm test` runs a first smoke test whose name starts with `TASK-007.<n>` | PASS | `TASK-007.1 app component is created` plus 1 check (`TASK-007.4`) | Tests passed (Vitest 5.1.1 with jsdom) | 2026-11-19 |
| 5 | `npm start` loads in a browser without console errors | PASS (one-off check by the implementer; not re-run by me and not automated) | none by design | The implementer served the app with `ng serve` (HTTP 200) and loaded it with headless Edge: `<app-root ng-version="22.3.0">` rendered, console showed only INFO lines. Re-check by hand once with `npm start` | 2026-11-19 |
| 6 | `src/frontend/REPO_CONTEXT.md` lists versions, runner, layout, commands, under 80 lines | PASS | 5 tests (`TASK-007.6`) | Tests passed; file is 58 lines | 2026-11-19 |

Totals: PASS 6, FAIL 0, N/A 0, NOT RUN 0

## Gates
- Mapping: 5 of 6 ACs have dedicated tests (83.3%). AC 5 is a browser check.
- Pass: 16 of 16 frontend tests pass (100%).
- Coverage: not measured; the frontend coverage gate is TASK-010.

## Notes
- Versions: Angular CLI and Material 22.3.0 (npm registry on 2026-11-19), Node 24.22.3, npm 11.19.1, Vitest 5.1.1 with jsdom.
- The CLI did not write `strict` or `strictTemplates` into `tsconfig.json`; both were added explicitly.
- The test spec reads files through untyped dynamic `node:fs` imports because `@types/node` is not installed; adding it is a separate change.
- `app.html` is still the CLI placeholder page; TASK-008 replaces it.
- PR size: 29 files and about 8,130 changed lines, of which 7,092 are `package-lock.json`. This exceeds the 5000-line `pr-guard` limit; the owner decides how to handle it (see the hand-over message).
- Licence check: all 426 packages in `package-lock.json` have a `license` field and none is GPL, AGPL, SSPL, BUSL or Commons-Clause.
