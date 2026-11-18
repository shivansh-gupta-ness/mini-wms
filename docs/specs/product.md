# Mini WMS: product specification

Status: **DRAFT, owner decisions applied; awaiting final review** (Phase 1 of `docs/sdd-roadmap.md`). Nothing here is approved until the owner merges it.
Owner: shivansh-gupta-ness. The owner's decisions are recorded in section 9.

## 1. Purpose and scope

Mini WMS is a small warehouse management system. It tracks what products exist, where they are stored, how many are on hand, and how stock moves in (receiving), within the warehouse (put-away, transfers, adjustments) and out (orders, picking, shipping).

It exists to evaluate Claude Code on a spec-driven build in .NET and Angular. It is built to production quality (validation, audit trail, tests, clear errors) and is **never deployed**: it runs only on a developer machine, backed by a local SQLite file.

## 2. Users and roles

| Role | Who | Can do |
|---|---|---|
| Admin | Warehouse manager | Everything, including managing users, locations and products |
| Clerk | Warehouse operator | Receive, put away, adjust, transfer, pick and ship stock; view everything |
| Viewer | Read-only user | View everything; change nothing |

The permission matrix is in section 7, module AUT.

## 3. Conventions used by this spec

- **IDs.** Modules have a 3-letter code (LOC, PRD, INV, INB, OUT, AUT). Stories are `LOC-1`, acceptance criteria are `LOC-1.1`. Tickets copy these IDs, and `docs/reports/<ID>/ac-results.md` reports a result per AC ID.
- **Acceptance criteria (AC)** are Given / When / Then and are testable. Every AC ends with a recorded PASS, FAIL, N/A or NOT RUN.
- **Status** words (for example `Draft`, `Received`) are values stored in the database and shown in the UI exactly as written.
- **Audit fields.** Every record keeps `createdAt`, `createdBy`, `updatedAt`, `updatedBy`.
- **Soft delete.** Locations and products are deactivated, never physically deleted, so history stays valid.
- **Quantities** are whole numbers of the product's unit of measure (no fractions in version 1).

## 4. Domain glossary

| Term | Meaning |
|---|---|
| Warehouse | The single physical site the system manages. Version 1 has exactly one, created automatically |
| Zone | An area inside the warehouse with a type: Receiving, Storage or Shipping |
| Bin | The smallest storage location, inside one zone, with a code unique across the warehouse. Stock is always held in a bin |
| Product | A catalogue item identified by a unique SKU |
| Stock level | Quantity of one product in one bin |
| Receipt | A document recording goods arriving from a supplier |
| Put-away | Moving received goods from the receiving bin to a storage bin |
| Order | A customer order for products to be shipped |
| Allocation | Reserving stock in specific bins for an order |
| Pick | Taking allocated stock from its bin for an order |
| Movement | An immutable record of any change in stock (receive, put-away, transfer, adjust, pick, ship) |

## 5. Module summary

| Code | Module | Knowledge-map component | Risk | Depends on |
|---|---|---|---|---|
| LOC | Locations | `locations` | low | none |
| PRD | Products | `products` | low | none |
| INV | Inventory | `inventory` | medium | LOC, PRD |
| INB | Inbound | `inbound` | medium | LOC, PRD, INV |
| OUT | Outbound | `outbound` | medium | LOC, PRD, INV |
| AUT | Auth | `auth` | high | all (cross-cutting) |

Build order follows the roadmap: LOC, PRD, INV, INB, OUT, then AUT. Until AUT is built the app runs with a fixed local Admin user.

## 6. Non-functional requirements

| ID | Requirement |
|---|---|
| NFR-1 | Runs locally only. No deployment, hosting, cloud service or network dependency. Data is a local SQLite file excluded from git |
| NFR-2 | Every list endpoint is paged (default 25, maximum 100) and sortable; list pages respond within 500 ms for 10,000 rows on a developer laptop |
| NFR-3 | Invalid input is rejected with a field-level error message; the API returns a consistent error body (ProblemDetails) and never a stack trace |
| NFR-4 | Stock changes are atomic: a failed operation leaves no partial change |
| NFR-5 | Every stock change writes a movement record with who, when, why and the before and after quantity |
| NFR-6 | The UI is keyboard-usable and every form control has a label |
| NFR-7 | Every acceptance criterion in this document is covered by at least one automated test (gates: mapping 100, pass 100, coverage 90) |
| NFR-8 | No secrets in source control; passwords are stored only as salted hashes |

## 7. Modules

### LOC: Locations (risk low)

The single warehouse, its zones and its bins.

**LOC-1. As an Admin, I set up the warehouse's zones and bins.**
- LOC-1.1 Given a fresh database, when the app starts, then exactly one warehouse exists (code and name from configuration, default `MAIN` and `Main warehouse`), and no screen or endpoint can create a second one.
- LOC-1.2 Given I am an Admin, when I add a zone with a code unique in the warehouse, a name and a type (Receiving, Storage or Shipping), then it is saved as Active.
- LOC-1.3 Given a zone exists, when I add a bin with a code unique across the warehouse, then it is saved under that zone and shown with its path (zone / bin).
- LOC-1.4 Given I submit a zone or bin with a missing code or name, a missing zone type, or a code already used in its scope, then it is rejected with a field-level message and nothing is saved.

**LOC-2. As an Admin, I edit and deactivate locations.**
- LOC-2.1 Given a zone or bin exists (or the warehouse), when I change its name, then the change is saved and its code stays unchanged; a zone's type cannot be changed once it has any bin.
- LOC-2.2 Given a bin holds stock, when I try to deactivate it, then it is rejected with a message naming the stock on hand.
- LOC-2.3 Given a bin holds no stock, when I deactivate it, then it is marked Inactive, stays in history, and cannot be chosen for new stock.
- LOC-2.4 Given a zone has any Active bin, when I try to deactivate the zone, then it is rejected; a zone with no Active bin can be deactivated.

**LOC-3. As any user, I browse locations.**
- LOC-3.1 Given locations exist, when I open the list, then I see zones and their bins as a tree or filtered list, paged and sortable.
- LOC-3.2 Given I type part of a code or name, when I search, then only matching locations are shown.

### PRD: Products (risk low)

The product catalogue.

**PRD-1. As an Admin, I create a product.**
- PRD-1.1 Given I am an Admin, when I create a product with a unique SKU, a name, a unit of measure and a reorder level of zero or more, then it is saved as Active.
- PRD-1.2 Given a SKU already exists (compared case-insensitively), when I create another product with it, then it is rejected with a field-level message.
- PRD-1.3 Given a required field is missing or the reorder level is negative, then the product is rejected and nothing is saved.

**PRD-2. As an Admin, I edit and deactivate products.**
- PRD-2.1 Given a product exists, when I change its name, unit of measure or reorder level, then the change is saved; the SKU cannot be changed.
- PRD-2.2 Given a product has stock on hand, when I try to deactivate it, then it is rejected with a message giving the total on hand.
- PRD-2.3 Given a product has no stock, when I deactivate it, then it is marked Inactive, keeps its history, and cannot be used on new receipts or orders.

**PRD-3. As any user, I find products.**
- PRD-3.1 Given products exist, when I open the list, then it is paged and sortable by SKU, name and status.
- PRD-3.2 Given I search by part of a SKU or name, then only matching products are shown.
- PRD-3.3 Given I open a product, then I see its details and total stock on hand across all bins.

### INV: Inventory (risk medium)

Stock levels and every change to them.

**INV-1. As any user, I see what is in stock and where.**
- INV-1.1 Given stock exists, when I open the stock list, then I see product, bin and quantity on hand, paged and filterable by product, zone and bin.
- INV-1.2 Given a product's total on hand is at or below its reorder level, then it is flagged Low stock in the list and on the product page.
- INV-1.3 Given I open a product, then I see its stock per bin and its total.

**INV-2. As a Clerk, I adjust stock.**
- INV-2.1 Given I choose a product, an Active bin and a quantity change, when I enter a reason, then the stock level changes by that amount and a movement of type Adjust is recorded.
- INV-2.2 Given an adjustment would make a stock level negative, then it is rejected and nothing changes.
- INV-2.3 Given I submit an adjustment without a reason, then it is rejected.
- INV-2.4 Given the stock level did not exist, when I adjust upward, then it is created.

**INV-3. As a Clerk, I transfer stock between bins.**
- INV-3.1 Given a source bin holds enough of a product, when I transfer a quantity to another Active bin, then the source decreases and the destination increases by the same quantity, and one movement of type Transfer is recorded.
- INV-3.2 Given the quantity exceeds what the source bin holds, or source and destination are the same bin, then the transfer is rejected and nothing changes.
- INV-3.3 Given the transfer fails part-way, then neither bin changes (NFR-4).

**INV-4. As any user, I read the movement history.**
- INV-4.1 Given movements exist, when I open the history, then I see date, type, product, bin(s), quantity, before and after quantity, user and reason, newest first, paged.
- INV-4.2 Given I filter by product, bin, type or date range, then only matching movements are shown.
- INV-4.3 Given any record in the history, then it cannot be edited or deleted.

### INB: Inbound (risk medium)

Receiving goods and putting them away.

**INB-1. As a Clerk, I record a receipt.**
- INB-1.1 Given I am a Clerk, when I create a receipt with a supplier name and at least one line (Active product, expected quantity greater than zero), then it is saved with status Draft and a generated receipt number.
- INB-1.2 Given a receipt has no lines, or a line has a quantity of zero or less, or an inactive product, then it is rejected.
- INB-1.3 Given a Draft receipt, when I edit or remove lines, then the changes are saved; once it is no longer Draft they are not allowed.

**INB-2. As a Clerk, I receive the goods.**
- INB-2.1 Given a Draft receipt, when I confirm receipt with a received quantity per line and a bin in a Receiving zone, then the stock in that bin increases, one movement of type Receive is recorded per line, and the status becomes Received.
- INB-2.2 Given a received quantity differs from the expected one, then the difference is stored on the line and shown on the receipt.
- INB-2.3 Given a receipt is already Received or Cancelled, when I try to receive it again, then it is rejected.
- INB-2.4 Given a Draft receipt, when I cancel it, then the status becomes Cancelled and no stock changes.

**INB-3. As a Clerk, I put received goods away.**
- INB-3.1 Given stock sits in a Receiving bin, when I put away a quantity to an Active bin in a Storage zone, then it is moved as a transfer and a movement of type PutAway is recorded.
- INB-3.2 Given the quantity exceeds what the Receiving bin holds, then it is rejected.
- INB-3.3 Given every received line is fully put away, then the receipt status becomes PutAway.

**INB-4. As any user, I find receipts.**
- INB-4.1 Given receipts exist, when I open the list, then I can filter by status, supplier and date, paged and sortable.
- INB-4.2 Given I open a receipt, then I see its lines, expected and received quantities, status history and the movements it caused.

### OUT: Outbound (risk medium)

Customer orders: allocate, pick, ship.

**OUT-1. As a Clerk, I create an order.**
- OUT-1.1 Given I create an order with a customer name and at least one line (Active product, quantity greater than zero), then it is saved as Draft with a generated order number.
- OUT-1.2 Given an order has no lines, or a line has a quantity of zero or less, then it is rejected.
- OUT-1.3 Given a Draft order, when I edit or remove lines or cancel it, then the change is saved; a cancelled order keeps its history.

**OUT-2. As a Clerk, I allocate stock to an order.**
- OUT-2.1 Given a Draft order and enough total stock for every line, when I allocate, then the system chooses the bins (oldest stock first, measured by the date the stock entered the bin, and only bins in Storage zones; stock in Receiving and Shipping zones is never allocated), the stock is reserved, the status becomes Allocated, and reserved stock cannot be used by another order or transfer.
- OUT-2.2 Given there is not enough unreserved stock for any line, then the allocation is rejected as a whole, nothing is reserved, and the message names the short products and quantities.
- OUT-2.3 Given an Allocated order is cancelled, then its reservations are released.

**OUT-3. As a Clerk, I pick an order.**
- OUT-3.1 Given an Allocated order, when I confirm a pick quantity per allocation, then those quantities are marked picked and a movement of type Pick is recorded per allocation.
- OUT-3.2 Given a pick quantity exceeds the allocated quantity, then it is rejected.
- OUT-3.3 Given every line is fully picked, then the status becomes Picked.

**OUT-4. As a Clerk, I ship an order.**
- OUT-4.1 Given a Picked order, when I ship it, then the picked stock leaves the warehouse, a movement of type Ship is recorded per line, and the status becomes Shipped.
- OUT-4.2 Given an order is not Picked, when I try to ship it, then it is rejected.
- OUT-4.3 Given a Shipped order, then it can no longer be edited, cancelled or shipped again.

**OUT-5. As any user, I find orders.**
- OUT-5.1 Given orders exist, when I open the list, then I can filter by status, customer and date, paged and sortable.
- OUT-5.2 Given I open an order, then I see its lines, allocations, picks and status history.

### AUT: Auth (risk high, built last, a person leads the edit)

**AUT-1. As a user, I sign in and out.**
- AUT-1.1 Given valid credentials, when I sign in, then I receive a session and see only what my role allows.
- AUT-1.2 Given invalid credentials, then sign-in fails with a generic message that does not say whether the user name or the password was wrong.
- AUT-1.3 Given five failed sign-ins for one user within ten minutes, then further attempts are refused for fifteen minutes.
- AUT-1.4 Given I sign out or my session expires, then protected pages and endpoints reject me.

**AUT-2. As an Admin, I manage users.**
- AUT-2.1 Given I am an Admin, when I create a user with a unique user name, a role and a password that meets the password rule (at least 12 characters), then the user is saved with a salted password hash and no password is ever shown again.
- AUT-2.2 Given I change a user's role or deactivate a user, then it takes effect on their next request.
- AUT-2.3 Given I am the last active Admin, when I try to deactivate or demote myself, then it is rejected.

**AUT-3. Roles are enforced on every endpoint and screen.**
- AUT-3.1 Given the matrix below, when a user without the permission calls an endpoint, then it returns a forbidden error and changes nothing.
- AUT-3.2 Given a user without a permission opens the app, then the controls for it are hidden or disabled (the server check in AUT-3.1 is the real control).
- AUT-3.3 Given any request without a valid session, then it returns an unauthenticated error.

Permission matrix:

| Action | Admin | Clerk | Viewer |
|---|---|---|---|
| View locations, products, stock, movements, receipts, orders | yes | yes | yes |
| Manage locations and products | yes | no | no |
| Adjust, transfer, receive, put away, create and edit orders, allocate, pick, ship | yes | yes | no |
| Manage users | yes | no | no |

## 8. Out of scope (version 1)

Deployment, hosting, CI/CD release steps, multi-tenant or multi-company use, barcode or RFID hardware, lot, serial and expiry tracking, fractional quantities, pricing, invoices and payments, purchase orders and supplier management beyond a name, returns, cycle-count workflows, reports beyond filtered lists, email or other notifications, integrations with other systems, multi-machine concurrency and any cloud database.

## 9. Decisions recorded by the owner

| # | Question | Decision |
|---|---|---|
| 1 | Multi-warehouse or one warehouse? | **One warehouse**, created automatically; zones and bins are managed under it |
| 2 | How is the receiving bin defined? | A bin in a zone of type Receiving (zone types: Receiving, Storage, Shipping) |
| 3 | Password rule and lockout | At least 12 characters; five failed sign-ins in ten minutes lock the user for fifteen minutes |
| 4 | Who chooses bins when allocating an order? | The system, oldest stock first, from Storage zones only |
| 5 | Roles | Three: Admin, Clerk, Viewer (no Supervisor) |
| 6 | Quantity type | **Whole numbers only** in version 1 |

## 10. Traceability

| Module | Stories | ACs |
|---|---|---|
| LOC | 3 | 10 |
| PRD | 3 | 9 |
| INV | 4 | 13 |
| INB | 4 | 12 |
| OUT | 5 | 14 |
| AUT | 3 | 10 |
| **Total** | **22** | **68** |
