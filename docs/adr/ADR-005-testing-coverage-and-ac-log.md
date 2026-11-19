# ADR-005: Testing, coverage and the acceptance-criteria log

Status: Proposed (accepted when merged)
Date: 2026-11-18
Owner: shivansh-gupta-ness

## Context
The evaluation needs proof that every acceptance criterion (AC) in `docs/specs/product.md` works, recorded as PASS, FAIL, N/A or NOT RUN (`docs/sdd-roadmap.md`). The feature pipeline already enforces gates: mapping 100%, pass 100%, coverage 90%.

## Decision
**Backend**
- Framework: xUnit. Assertions: xUnit's own `Assert` (no assertion library with licence changes). Test doubles: hand-written fakes for Application tests; NSubstitute only where a fake is impractical.
- Layers: Domain tests (rules), Application tests (use cases with fakes), Api tests (real HTTP against a temporary SQLite database, ADR-002). Prefer the real database over mocking EF Core.
- Coverage: `coverlet.collector`; reports built with ReportGenerator. Scope: Domain, Application and Api. Excluded: migrations, `Program.cs`, generated code. Gate: line coverage of at least 90% of the scope; branch coverage is reported, not gated.
- Static analysis: `dotnet build /warnaserror` with the default analyzers; warnings are errors.

**Frontend**
- The runner is Angular's default at scaffold time (Vitest or Jasmine with Karma, depending on the CLI version); TASK-003 records which one. Component tests use Angular's testing utilities and a fake `HttpClient` backend. Gate: 90% line coverage of `src/frontend/src/app`.
- End-to-end browser tests are out of version 1 unless the owner adds a ticket.

**AC traceability (the rule that makes the log possible)**
- Every test that proves an AC carries the AC ID: backend `[Trait("AC", "LOC-1.1")]` (repeat the trait for several ACs); frontend the AC ID at the start of the test name, `it('LOC-1.1 creates a zone ...')`.
- A test with no AC ID is allowed (extra tests), but an AC with no test is reported as NOT RUN.
- The pipeline's Prompt 3 mapping table and Prompt B step 5 read these IDs and write `docs/reports/<ID>/ac-results.md` and the `docs/reports/ac-log.md` row (PASS, FAIL, N/A, NOT RUN) from real test output, never from memory.
- A ticket is done only when every AC in its scope is PASS, or a written owner decision explains the exception.

**Pipeline gates:** mapping 100% (every AC in scope has a test), pass 100% (all tests pass), coverage 90%.

**Running locally:** `dotnet test src/backend --collect:"XPlat Code Coverage"` and, in `src/frontend`, `npm test` and `npm run build`. There is no CI test workflow; running the tests is the owner's check before opening a PR, and the `run-tests` hook runs them when an editing agent finishes.

## Consequences
- Test names carry the spec, so a failing AC is found by its ID.
- The 5000-line and 60-file PR limits count tests; slice tickets so code and tests fit.
- Without CI, a passing local run is the only evidence; the reports under `docs/reports/<ID>/` quote real output so a reviewer can check it.
