# Autonomy by risk

The `risk` value on a knowledge-map component (and on the ticket) sets how far an agent may go.

| Risk | Examples in Mini WMS | Agent role | Approval |
|---|---|---|---|
| low | Tests, documentation, locations CRUD, small fixes | Edits after plan approval | Code owner review |
| medium | Inventory, inbound, outbound features and refactors in one module | Edits after plan approval | Code owner review |
| high | Authentication and roles, data migrations, stock-adjustment rules | Reads and proposes; a person leads the edit | Code owner plus a second look (`/security-review`) |
| blocked | Secrets, CI workflows, the harness itself | No agent edits | Normal human change through a `setup/` PR |

Rules of thumb: when a ticket touches two components, use the higher risk. When unsure, treat it as one level higher.
