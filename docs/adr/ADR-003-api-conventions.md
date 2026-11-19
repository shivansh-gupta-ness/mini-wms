# ADR-003: API conventions

Status: Proposed (accepted when merged)
Date: 2026-11-18
Owner: shivansh-gupta-ness

## Context
The frontend depends on the API contract in `docs/specs/api/`. Consistent conventions keep the contract small and let tests and the UI treat every module the same way.

## Decision
**Contract first.** The OpenAPI files in `docs/specs/api/<module>.yaml` are the source of truth. Each operation lists the acceptance criteria it serves in an `x-acceptance-criteria` extension. If the code must differ, the contract changes in the same PR.

**Style**
- JSON over HTTP, base path `/api/v1`, plural kebab-case nouns (`/products`, `/stock-levels`).
- Identifiers are integers (`id`). Business codes (SKU, zone code, bin code) are separate unique fields.
- Dates and times are ISO 8601 in UTC. Quantities are integers.
- Property names are camelCase. Enum values are the exact words in the spec (`Draft`, `Received`, `Storage`).
- State changes that are not simple edits are actions: `POST /products/{id}/deactivate`, `POST /receipts/{id}/receive`.

**Lists:** `GET` collections accept `page` (default 1), `pageSize` (default 25, maximum 100), `sort` (`field` or `-field`) and `q` (text search where the spec asks). Response:
```json
{ "items": [], "page": 1, "pageSize": 25, "totalItems": 0 }
```

**Errors:** every error is `application/problem+json` (RFC 9457) with `type`, `title`, `status`, `traceId` and, for validation, an `errors` object keyed by field name. `detail` is present only on 4xx responses whose text our own code wrote (validation and business-rule messages, for example the 409 `detail` the UI shows in a banner). It is never sent on a 5xx and never carries raw exception text, so an unexpected error cannot leak internals (decided 2026-11-19, resolving the TASK-006 / BUG-001 conflict):
```json
{ "type": "validation", "title": "Validation failed", "status": 400, "errors": { "sku": ["SKU is required."] } }
```

| Status | Used for |
|---|---|
| 200 / 201 / 204 | Success; 201 returns the new resource and a `Location` header |
| 400 | Validation of the request (missing or malformed fields) |
| 401 | No valid session |
| 403 | Signed in, but the role lacks the permission |
| 404 | Resource does not exist |
| 409 | Business-rule conflict (duplicate code, deactivate with stock, stale `version`) |
| 500 | Unexpected error: generic body, details only in the log, never a stack trace (NFR-3) |

**Validation:** request shape and field rules are checked in the Application layer with FluentValidation validators, one per command; rules that need data (uniqueness, stock) are checked in the use case and raise a domain exception mapped to 409. Messages are the text the UI shows.

**Concurrency:** updates send the resource `version`; a mismatch returns 409.

**CORS:** only `http://localhost:4200` (the Angular dev server) is allowed.

## Consequences
- The frontend can generate or hand-write typed clients straight from the YAML.
- One error shape means one error-handling path in Angular (ADR-006).
- 409 for business rules (not 422) keeps the status list short; the `type` field distinguishes causes.
