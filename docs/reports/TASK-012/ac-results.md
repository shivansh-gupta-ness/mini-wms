# TASK-012 acceptance-criteria results

Ticket: `tickets/TASK-012.md` (Lint the OpenAPI contracts). Branch: `task/TASK-012-lint-openapi-contracts`. Date: 2026-11-19.
How it was run: `/start-ticket` flow with `ticket-scout`, owner approval (decisions recorded in the ticket, including the npm-registry exception for this ticket), and `change-implementer` in an isolated worktree, work carried back with `worktree-apply.mjs`. No separate test-writer ran: the rule tests and the coverage script are part of the deliverable and were written with the rules. Numbers below come from my own runs in the main working tree after the hand-off.

Pinned version: `@stoplight/spectral-cli@6.17.1` (the registry's version on 2026-11-19, runs on Node 24.22.3). Nothing was installed into the repository; the CLI runs through `npx`.

| Command (from the repository root) | Result |
|---|---|
| `npx @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml" --fail-severity=warn` | `No results with a severity of 'warn' or higher found!` exit 0 (zero errors and zero warnings on both contracts) |
| `node docs/specs/api/tests/run-rule-tests.mjs` | 6 of 6 fixtures PASS (exit 0); each fixture asserts the exact set of rule codes |
| `node docs/specs/api/tools/ac-coverage.mjs` | `Ids in product.md: 19; covered: 19`; none missing in either direction (exit 0) |

Before the contract fixes the lint reported `36 problems (0 errors, 36 warnings)`, all from default rules (`operation-description`, `operation-tags`, then `operation-tag-defined`); the three custom rules reported nothing on the real contracts. The agent fixed them by adding tags and descriptions in the YAML and turned off only `info-contact`, with the reason written in `.spectral.yaml`.

| AC # | AC text (short) | Result | Test(s) that prove it | Evidence | Date |
|---|---|---|---|---|---|
| 1 | The pinned Spectral lint reports zero errors and zero warnings on both contracts | PASS (by a real run, no unit test) | none by design | The lint run above with `--fail-severity=warn`, exit 0 | 2026-11-19 |
| 2 | An operation without `x-acceptance-criteria` is reported by a custom rule | PASS | `tests/missing-ac.yaml` (expects exactly `operation-has-acceptance-criteria`), `tests/valid.yaml` (expects none) | `run-rule-tests.mjs` PASS | 2026-11-19 |
| 3 | An error response without `application/problem+json` is reported | PASS | `tests/error-no-problem-json.yaml` (expects exactly `error-response-is-problem-json`) and `tests/problem-wrong-schema.yaml` (expects exactly `error-response-uses-problem-schema`) | `run-rule-tests.mjs` PASS | 2026-11-19 |
| 4 | A list operation without `page`, `pageSize` and `sort` is reported | PASS | `tests/list-missing-sort.yaml` (expects exactly `list-operation-has-paging-params`), `tests/not-a-list.yaml` (single-object GETs are not flagged) | `run-rule-tests.mjs` PASS | 2026-11-19 |
| 5 | Each LOC and PRD acceptance criterion appears in an `x-acceptance-criteria` list | PASS | `tools/ac-coverage.mjs` | 19 of 19 ids covered, table below | 2026-11-19 |
| 6 | The lint command is documented with the pinned version in `docs/specs/api/README.md` | PASS | none by design | The README lists `npx @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml"` | 2026-11-19 |

Totals: PASS 6, FAIL 0, N/A 0, NOT RUN 0

## AC 5 check (output of `node docs/specs/api/tools/ac-coverage.mjs`)
| Id | Covered | Operations |
|---|---|---|
| LOC-1.1 | yes | getWarehouse |
| LOC-1.2 | yes | createZone |
| LOC-1.3 | yes | createBin |
| LOC-1.4 | yes | createZone, createBin |
| LOC-2.1 | yes | renameWarehouse, updateZone, updateBin |
| LOC-2.2 | yes | deactivateBin |
| LOC-2.3 | yes | deactivateBin |
| LOC-2.4 | yes | deactivateZone |
| LOC-3.1 | yes | getWarehouse, listZones, getZone, listBins, getBin, getLocationTree |
| LOC-3.2 | yes | listZones, listBins |
| PRD-1.1 | yes | createProduct |
| PRD-1.2 | yes | createProduct |
| PRD-1.3 | yes | createProduct, updateProduct |
| PRD-2.1 | yes | updateProduct |
| PRD-2.2 | yes | deactivateProduct |
| PRD-2.3 | yes | deactivateProduct |
| PRD-3.1 | yes | listProducts |
| PRD-3.2 | yes | listProducts |
| PRD-3.3 | yes | getProduct |

## Gates
- Mapping: 6 of 6 ACs are proven (AC 2 to 5 by fixtures or the script, AC 1 and 6 by a real run and by reading the README).
- Pass: 6 of 6 rule fixtures and 19 of 19 criteria covered.
- Coverage: not applicable (no application code).

## Notes
- Contract changes (called out for the reviewer): both contracts gain `tags` on every operation, an operation `description`, and global `tags` definitions; the shared `Problem` schema now requires `traceId` (the backend adds it to every error) and its `detail` description says it appears only on 4xx text written by our own code. Paths, operations and the `x-acceptance-criteria` lists are unchanged.
- The list rule detects a list as a GET whose 200 JSON schema has an `items` property, so `/warehouse` and `/locations/tree` are not flagged.
- The fixtures live under `docs/specs/api/tests/`, which the `docs/specs/api/*.yaml` glob does not match, so the deliberately bad files never affect the main lint.
- Not verified: the `Problem` schema was not re-compared against the backend source in this run; the `traceId` and `detail` text follows ADR-003 and the backend behaviour from BUG-001 and TASK-006.
- Out of scope: wiring Spectral into CI (`.github/` is protected), the health endpoint, a shared 500 response and 401/403 in the contracts.

## Review round (`/code-review`, 8 findings)
Fixed:
- #1 and #2 `ac-coverage.mjs` exits 1 when it parses nothing from `product.md` or from the contracts, and also reads block-style `x-acceptance-criteria` lists;
- #3 `run-rule-tests.mjs` writes the Spectral report to a file (log lines on stdout cannot break the parse) and says "Spectral did not produce a report ... is the npm registry reachable?" when it did not run;
- #4 and #8 a new rule `error-response-uses-problem-schema` requires the `application/problem+json` schema to require `type`, `title`, `status` and `traceId`; a new fixture `problem-wrong-schema.yaml` proves it, and `valid.yaml` now carries the full `Problem`;
- #6 the rules cover every operation kind (`head`, `options` and `trace` as well) and no longer walk non-operation keys of a path item.
After the fixes: lint with `--fail-severity=warn` exit 0, 6 of 6 rule tests PASS, 19 of 19 criteria covered.
Not changed: #5 the list rule does not see a page schema wrapped in `allOf` or paging parameters declared at path level (recorded under "Known limits" in the README; no contract needs it today); #7 requiring `traceId` is a deliberate contract change that matches the backend (called out in the PR), and the shared `Problem` schema stays duplicated in the two contracts for now (the new rule keeps the copies honest).
Not exercised: the new "parses nothing" and block-list paths of `ac-coverage.mjs` were not run against a changed `product.md`.
