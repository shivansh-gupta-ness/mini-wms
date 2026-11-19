---
id: TASK-009
type: task
title: Frontend HTTP layer: interceptors, ApiError and form error helper
status: ready
owner: shivansh-gupta-ness
component: frontend-platform
risk: low
allowed_paths: [src/frontend/**, tickets/TASK-009.md, docs/reports/TASK-009/**, docs/reports/ac-log.md]
docs: [docs/adr/ADR-006-angular-structure.md, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: [TASK-007]
---

## Description
Build the single HTTP layer ADR-006 describes: base path, token interceptor, error interceptor and a helper that applies server field errors to a reactive form. Set `status: ready` after TASK-007 is merged.

## Acceptance criteria
1. Given any request, when it is sent through the API client, then the base path `/api/v1` is prefixed and no API host is hard-coded.
2. Given a session token exists, when a request is sent, then the token interceptor adds the bearer token; with no token, no header is added.
3. Given an `application/problem+json` response, when the error interceptor handles it, then it produces a typed `ApiError` with `status`, `title`, `detail` and the field `errors`.
4. Given a 5xx response, when it is handled, then the user sees a generic message with no technical detail (NFR-3).
5. Given an `ApiError` with field errors, when the form helper is applied to a reactive form, then each message is set on the matching control and shown under it.
6. Given the tests, when they run, then all pass and each AC-proving test name starts with `TASK-009.<n>`.

## Out of scope
- Real module services.
- Sign-in screens.

## Notes and links
- Record AC results in `docs/reports/TASK-009/ac-results.md` and one row in `docs/reports/ac-log.md`.
- Analysis before readying (2026-11-19): TASK-007 is merged. The error shape to parse is the backend's (ADR-003, as built in TASK-006): `application/problem+json` with `type`, `title`, `status`, `traceId` and, for validation, `errors` keyed by camelCase field name; `detail` is present only on 4xx text written by our own code (business-rule 409, the concurrency 409 text) and never on 5xx, so the 5xx message must be generic and must not show `detail` or `traceId` beyond an optional reference. The dev-server proxy is TASK-011, so this ticket only prefixes `/api/v1`. Tests use Vitest (`HttpTestingController` or a fake backend), names start with `TASK-009.<n>`.
- Owner decisions (2026-11-19, on the scout's brief, recommended options taken): an `ErrorNotifier` in `core/errors/` shows the generic message through `MatSnackBar` for 5xx and network errors, and the error interceptor still rethrows the `ApiError`; the token lives in an in-memory signal `AuthSession` in `core/auth/` (nothing persisted, ADR-004); an `ApiClient` wrapper in `core/api/` adds `API_BASE_PATH = '/api/v1'` (interceptors never rewrite URLs); `traceId` stays on `ApiError` for logs and is not shown; `detail` is exposed on `ApiError` and the 409 banner is built with the first module form, not here; `applyServerErrors` returns the unmatched messages instead of dropping them; the interceptor does not redirect on 401 (session expiry belongs to the auth module). The scout's note that the root `REPO_CONTEXT.md` still says 1000 lines and 30 files is stale: it was corrected in PR #23.
