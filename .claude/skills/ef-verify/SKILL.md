---
name: ef-verify
description: Verify EF Core queries and writes with the read-only ef-query-verifier agent (SQL shape, indexes, paging, concurrency, SQLite traps). Changes nothing.
disable-model-invocation: true
argument-hint: "[TICKET-ID | path under src/backend/MiniWms.Infrastructure]"
---

Verify the EF Core queries for `$ARGUMENTS` (no argument: the Locations module). Change nothing in this skill.

Preconditions (check first; STOP and tell the owner if any fails)
- A ticket ID must have a `tickets/<ID>.md` whose `allowed_paths` include persistence code. A path must exist under `src/backend/MiniWms.Infrastructure/`.

Method
1. Run the `ef-query-verifier` agent with the argument.
2. Show its report unchanged, including "Not verified". The agent reasons about SQL from the LINQ; it does not capture the real SQL, so do not present its SQL shapes as measured.
3. STOP. Ask the owner which findings become tickets. Do not fix anything here.
