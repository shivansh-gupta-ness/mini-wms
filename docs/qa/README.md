# QA cases

One file per ticket: `docs/qa/<TICKET-ID>.md` (replaces AgileTest). Prompt 3 of the feature pipeline maps unit tests to these cases.

```
| ID     | Title                         | Steps                         | Expected result              |
|--------|-------------------------------|-------------------------------|------------------------------|
| QA-001 | Low-stock flag set on receive | Receive stock below threshold | Item shows LowStock = true   |
```

Cases are written by the QA lead or the ticket owner. Pure-UI cases (layout, tab order) are marked N/A in the mapping with a one-line reason.

The QA case files are optional; where none exists, the ticket acceptance criteria are the fallback.
