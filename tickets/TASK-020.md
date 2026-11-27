---
id: TASK-020
type: task
title: Check the search text length after trimming
status: ready
owner: shivansh-gupta-ness
component: locations
risk: low
allowed_paths: [src/backend/MiniWms.Application/Locations/LocationRules.cs, src/backend/MiniWms.Application.Tests/Locations/LocationListAndVersionTests.cs, src/backend/MiniWms.Api.Tests/Locations/LocationListApiTests.cs, src/backend/REPO_CONTEXT.md, docs/specs/api/locations.yaml, tickets/TASK-020.md, docs/reports/TASK-020/**, docs/reports/ac-log.md]
docs: [docs/specs/api/locations.yaml, docs/adr/ADR-003-api-conventions.md]
parent: 
depends_on: []
---

## Description
`GET /api/v1/zones` and `GET /api/v1/bins` take a search text `q`. Its rules disagree in `src/backend/MiniWms.Application/Locations/LocationRules.cs`:
- `ValidSearch` checks the raw `q`, so it must be at most 100 characters including any spaces around it.
- `NormalizeSearch` then trims `q`, and the search uses only the trimmed text.

So `q` = 100 letters plus a trailing space is refused with a 400, although the search would use exactly 100 characters. The name and code rules already check the length after trimming (`RequiredText`). Make the search rule do the same: trim first, then check the length.

## Acceptance criteria
1. Given `q` with spaces around it whose trimmed length is 100 or less (for example 100 letters plus a leading and a trailing space), when `GET /api/v1/zones` or `GET /api/v1/bins` is called, then the response is 200 and matches the same rows as the trimmed text.
2. Given `q` whose trimmed length is over 100, when either list is called, then the response is still a 400 validation problem with an error under `q`, and the message is unchanged ("Search text must be at most 100 characters.").
3. Given `q` that is empty or only spaces, when either list is called, then it is still treated as no search (200, unfiltered), as today.
4. Given the existing `q` tests (`A_q_of_exactly_100_characters_is_accepted`, `A_q_over_100_characters_is_400_under_q`, `List_zones_and_bins_with_q_over_100_characters_are_rejected_under_q`, and the search tests), when they run, then they pass with their expected values unchanged.
5. Given `docs/specs/api/locations.yaml`, when the `Q` parameter is read, then its description says the 100-character limit counts the text after leading and trailing spaces are removed. Spectral and `node docs/specs/api/tools/ac-coverage.mjs` still pass.
6. Given the tests, when they run, then all pass with `[Trait("AC", "TASK-020.<n>")]` on the new tests proving criteria 1 to 3. `dotnet test src/backend` and the 90% coverage gate pass. `src/backend/REPO_CONTEXT.md` mentions the rule if it describes search validation. `docs/reports/TASK-020/ac-results.md` and one row in `docs/reports/ac-log.md` record the real output.

## Out of scope
- The frontend (the bins search box already trims before sending).
- The search behaviour itself (LIKE escaping, case handling, the fields searched).
- The code and name rules (they already trim before checking).
- Products and the other design-only modules.

## Notes and links
- `ValidSearch` and `NormalizeSearch` are both in `LocationRules.cs`. `ListZones.cs` and `ListBins.cs` call them and need no change.
- The contract's `Q` parameter (`components.parameters.Q`) keeps `maxLength: 100` in its schema. A raw `q` longer than 100 that trims to 100 or fewer would now pass, so the description must say the limit is counted after trimming. If Spectral or `ac-coverage` objects to the schema, stop and ask the owner.
- Do not edit protected paths (`.github/`, `.claude/`, `scripts/`, the root `REPO_CONTEXT.md`, `docs/knowledge-map.yaml`).
