# A Compze API spends nothing on what its callers can do as well themselves

Whatever an API spends on each call, processor time or memory, every caller pays, those who don't need it too. So a
Compze API does only the work that nearly every caller needs, and that callers couldn't do as cheaply themselves.
Magnus, 2026-11-17: "we should not do anything that costs CPU or memory that is not pretty certain to be what the great
majority of clients want and need. Even if it is the great majority, if they can do it themselves with the same
performance with a single call to a sort.... Then they should..."

- **A library can't know its callers' sizes:** a store, an index's group or a batch may hold millions.
- **Ordering a result is the usual case.** A caller that needs an order sorts what it got, at the same cost, and a search
  sorts after it has filtered, when far less is left. A result with no order is a collection, such as
  `IReadOnlyCollection`, not a list: a list's positions read as an order whatever its documentation says.
