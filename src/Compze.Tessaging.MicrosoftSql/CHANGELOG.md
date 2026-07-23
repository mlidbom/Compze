# Changelog

All notable changes to Compze.Tessaging.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- The tessaging sql layers store in the endpoint's prefixed table-set (`EndpointTableSet`, resolved from the container): the inbox, outbox, outbox-dispatching, and peer-memory tables are per-endpoint prefixed (`«EndpointName»_InboxTessages`), so any number of endpoints store side by side in one domain database, and `SchemaCreationSql` became a function of the table-set. New: `MsSqlEndpointCatalogSqlLayer` — the domain database's one shared, deliberately unprefixed `EndpointCatalog` table recording each endpoint and the process holding its lock (insert-if-absent race-safe via `UPDLOCK, HOLDLOCK`).
- The tessaging SQL layer implementations are async end to end (`SaveTessageAsync`, `MarkAsReceivedAsync`, `GetUndeliveredTessagesForEndpointAsync`, the inbox marks, the peer-registry members — matching the now-async `ITessagingSqlLayer`): exactly-once tessaging is database I/O, and synchrony-follows-the-type reaches the storage first. Same SQL, async command and reader plumbing.
- `MsSqlDomainDatabase(connectionStringName)` on `ExactlyOnceEndpointBuilder`: declares that the domain database this endpoint joins is SQL Server, filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing — the connection pool, the type-id interner Tessaging's sql layers share, and Tessaging's SQL Server inbox/outbox sql layers.
- **The storage follows the reliability work.** The endpoint catalog's sql layer carries a process **lock** rather than a lease — the heartbeat claim/renew/release statements are gone, replaced by a session-scoped lock held for the holder's lifetime. Two new per-endpoint tables serve exactly-once in-order delivery: the outbox's per-receiver delivery-stream counters and the inbox's delivery-stream admissions. The handling execution takes a row-level claim on its inbox row inside the handling transaction, and the inbox recovery scan re-enqueues admitted-but-unhandled tessages at start.
- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `MsSqlTessagingSqlLayer()` demands the SQL Server type-id interner itself (`MsSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.
- `AddExactlyOnceTessaging(compose)` on `EndpointFoundation<MsSqlEndpointDatabase>`: adds exactly-once Tessaging to an endpoint whose database is SQL Server, registering Tessaging's SQL Server inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
