# API contracts

OpenAPI 3.0.3 files, one per module, are the source of truth for the HTTP API (contract first, see `docs/adr/ADR-003-api-conventions.md`).

| File | Module |
|---|---|
| `locations.yaml` | LOC: warehouse, zones, bins |
| `products.yaml` | PRD: products |

Each operation lists the acceptance criteria it serves in `x-acceptance-criteria` (ids from `docs/specs/product.md`).

## Lint

Spectral is not installed in the repository; run it through `npx` with the pinned version (Node 18 or later). From the repository root:

```
npx @stoplight/spectral-cli@6.17.1 lint "docs/specs/api/*.yaml"
```

The expected result is no errors and no warnings. The ruleset is `.spectral.yaml` at the repository root: it extends `spectral:oas` and adds three rules that enforce ADR-003 (all severity error):

| Rule | Enforces | Why |
|---|---|---|
| `operation-has-acceptance-criteria` | every operation (get, put, post, delete, patch) has a non-empty `x-acceptance-criteria` array | keeps each operation traceable to the spec |
| `error-response-is-problem-json` | every 4xx and 5xx response has `application/problem+json` content | one error shape for the whole API (RFC 9457) |
| `error-response-uses-problem-schema` | the `application/problem+json` schema requires `type`, `title`, `status` and `traceId` | every error body carries the correlation id (ADR-003) |
| `list-operation-has-paging-params` | a GET whose 200 JSON schema has an `items` property declares `page`, `pageSize` and `sort` parameters | every collection pages and sorts the same way |

Default rule `info-contact` is switched off in `.spectral.yaml` with the reason written next to it.

## Rule tests

`tests/` holds small fixture contracts. `valid.yaml` and `not-a-list.yaml` must report nothing; each other fixture must trigger exactly one custom rule. They are not matched by the lint glob above.

```
node docs/specs/api/tests/run-rule-tests.mjs
```

It prints one line per fixture and exits 1 on any mismatch.

## Acceptance-criteria coverage

`tools/ac-coverage.mjs` compares the LOC and PRD acceptance criteria in `docs/specs/product.md` with the `x-acceptance-criteria` lists of the contracts, prints a table, and exits 1 if any criterion is uncovered.

```
node docs/specs/api/tools/ac-coverage.mjs
```

## Known limits of the rules
- The list rule recognises a list as a GET whose 200 JSON schema has `items` directly under `properties`; a page schema wrapped in `allOf`, or paging parameters declared at path level, are not recognised yet. Add a fixture and extend the rule when a contract needs it.
- `tools/ac-coverage.mjs` reads `product.md` list lines (`- LOC-1.1 ...`) and `x-acceptance-criteria` as an inline `[A, B]` list or a block list. It exits 1 when it parses nothing, so a format change cannot pass silently.
- The `Problem` schema is repeated in each contract; the rules check its shape, so the copies cannot drift apart silently.

## CI

Not wired into CI yet (`.github/` is protected and out of scope for TASK-012). Run the three commands above by hand before changing a contract. Update the pinned version in `.spectral.yaml`, `tests/run-rule-tests.mjs` and this file together.
