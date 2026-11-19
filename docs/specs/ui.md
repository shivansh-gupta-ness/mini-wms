# Mini WMS: UI specification

Status: Accepted. Source: `docs/specs/product.md`. Structure: `docs/adr/ADR-006-angular-structure.md`. Locations is built; Products is specified in full but not built in this evaluation (`product.md` section 9, decision 7); the other modules are outlined and get their detail when their module is frozen.

## 1. Shell and navigation
- Top bar: app name, signed-in user and role, sign-out (once AUT exists; until then a fixed "Dev Admin" label).
- Left navigation, in this order: **Stock**, **Receipts**, **Orders**, **Products**, **Locations**. All five items show; a module that is not built opens a placeholder page. **Users** (Admin only) is not shown: it has no route until the Auth module exists.
- Every page has a title, a primary action button top-right (for example "New product"), and a body. Breadcrumbs only on detail pages.
- Layout works from 1024 px wide upwards; phones and tablets are out of version 1.

## 2. Common patterns (all modules)
| Pattern | Behaviour |
|---|---|
| Lists | Table with sortable column headers, a search box (`q`), filters above the table, and a paginator (25, 50, 100 per page). Empty state explains how to add the first item |
| Forms | Reactive forms, labels on every control, required marked with an asterisk, submit disabled while invalid or saving |
| Server errors | A 400 response puts each message under its field; a 409 shows the `detail` in a banner at the top of the form; 500 shows a generic message with no technical detail (NFR-3) |
| Confirm | Deactivate actions open a confirm dialog naming the item; the dialog's default focus is Cancel |
| Status words | Shown exactly as in the spec (`Active`, `Inactive`, `Draft`) as a coloured chip with text, never colour alone |
| Roles | Buttons the role may not use are hidden; the server is the real check (AUT-3.2). Until the Auth module, buttons show to everyone |
| Loading | A progress bar on first load, a disabled button with a spinner on save |
| Accessibility | Keyboard-reachable, visible focus, labels or `aria-label` on all controls, dialogs trap focus (NFR-6) |

## 3. Locations (module LOC)

### 3.1 Locations page (`/locations`)
- Two tabs: **Tree** and **List**.
- **Tree** (LOC-3.1): zones as parents, bins as children, each showing code, name, an Active/Inactive chip, and for zones the type (`Receiving`, `Storage`, `Shipping`). A toggle shows or hides Inactive items. Admin sees (until the Auth module: shown to everyone) buttons: "New zone", per zone "New bin", and per item "Edit" and "Deactivate".
- **List** (LOC-3.1, LOC-3.2): a bins table with columns Code, Name, Zone, Status; sortable; search box filters by part of code or name; a zone filter.
- The warehouse name is shown in the page header with an Admin-only "Rename" action (until the Auth module: shown to everyone) (LOC-2.1). There is no "New warehouse" control (LOC-1.1).

### 3.2 Zone form (dialog)
Fields: Code (required, max 20, unique), Name (required, max 100), Type (select; disabled when the zone has bins, with a hint saying why). Errors per LOC-1.4. Code is read-only when editing (LOC-2.1).

### 3.3 Bin form (dialog)
Fields: Zone (select of Active zones), Code (required, max 30, unique across the warehouse), Name (required, max 100). Code is read-only when editing.

### 3.4 Deactivate
- Bin: confirm dialog. If the API answers 409 (stock on hand, LOC-2.2) the dialog stays open and shows the message naming the stock.
- Zone: same; 409 when it still has an Active bin (LOC-2.4).

## 4. Products (module PRD, not built in this evaluation)

### 4.1 Products page (`/products`)
- Table columns: SKU, Name, Unit, Reorder level, On hand, Status. Sortable by SKU, Name and Status (PRD-3.1). Search box matches part of SKU or name (PRD-3.2). Filter: Active, Inactive, All (default Active).
- A **Low stock** chip appears next to On hand when `lowStock` is true (INV-1.2; the chip shows once Inventory exists, before that On hand is 0).
- "New product" button (Admin). Row click opens the detail page.

### 4.2 Product form (page or dialog)
Fields: SKU (required, max 40, case-insensitive unique, read-only after creation), Name (required, max 150), Unit of measure (required, max 20), Reorder level (whole number, 0 or more). Client messages match the API messages (PRD-1.2, PRD-1.3).

### 4.3 Product detail (`/products/:id`)
Shows the fields, the total on hand, and a "Stock by bin" table (empty until Inventory exists). Admin actions: Edit, Deactivate. Deactivate follows the confirm pattern; a 409 shows the total on hand (PRD-2.2). An Inactive product shows a notice that it cannot be used on new receipts or orders (PRD-2.3).

## 5. Outlines for later modules (detail added when each module is frozen)
| Module | Screens |
|---|---|
| INV | Stock list with filters and Low stock flag; product-by-bin view; Adjust and Transfer dialogs (reason required); Movement history table with filters |
| INB | Receipts list; receipt form with lines; Receive dialog (received quantity per line, receiving bin); Put-away dialog; receipt detail with status history |
| OUT | Orders list; order form with lines; Allocate action showing chosen bins or the shortage message; Pick dialog; Ship action; order detail |
| AUT | Sign-in page; Users list and form (Admin); sign-out; session-expired redirect |

## 6. Out of scope
Mobile layouts, dark mode, printing, import and export, localisation (English only), and any screen not listed above.
