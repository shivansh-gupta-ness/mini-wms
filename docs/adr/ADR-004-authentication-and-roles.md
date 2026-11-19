# ADR-004: Authentication and roles

Status: Accepted; not implemented (Auth deferred, scope decision 2026-11-19)
Date: 2026-11-18
Owner: shivansh-gupta-ness

## Context
`docs/specs/product.md` module AUT requires sign-in, three roles (Admin, Clerk, Viewer), lockout and role checks on every endpoint. Auth is high risk and is built last, so every earlier module must work without it and must not need rework when it arrives.

## Decision
- **Own user table, no external identity provider.** `User` stores `userName` (unique, case-insensitive), `passwordHash`, `role`, `isActive`, failed-attempt counters and audit fields.
- **Password hashing:** ASP.NET Core `PasswordHasher<TUser>` (PBKDF2, per-user salt). Minimum length 12 (AUT-2.1). Passwords never appear in logs, responses or source control (NFR-8).
- **Session:** JWT bearer access token, 30 minutes, signed with HS256. The signing key comes from user-secrets or an environment variable, never from a committed file. The Angular app keeps the token in memory only (no local storage), so a page reload signs the user out.
- **Roles as policies:** three authorization policies built on the permission matrix in the spec: `CanView`, `CanOperate` (Admin or Clerk), `CanAdminister` (Admin). Endpoints declare a policy; they never check role names inline.
- **Lockout:** five failed sign-ins for one user within ten minutes locks sign-in for fifteen minutes (AUT-1.3). The failure message is generic (AUT-1.2).
- **Last Admin protection** is a domain rule enforced when changing a role or deactivating a user (AUT-2.3).
- **Before Auth exists:** modules are built against the policies from day one. A development handler authenticates every request as a fixed Admin while `Auth:DevAdmin` is `true`. The setting defaults to `false` and is rejected when the environment is not Development.
- **Direct checks:** an integration test per module asserts that each endpoint returns 401 without a session and 403 for a role without permission (AUT-3.1, AUT-3.3), once Auth lands.

## Consequences
- No endpoint is written without a policy, so adding real auth later changes configuration, not endpoints.
- JWT in memory is safer against theft than storage, at the cost of signing in again after a reload; acceptable for a local app.
- The Locations endpoints were built open (no policy and no development handler). They gain their policies in the Auth ticket.
- Auth is the highest-risk module: agents propose, the owner makes the edit (`docs/autonomy-by-risk.md`), and `/security-review` is run on its PR.
