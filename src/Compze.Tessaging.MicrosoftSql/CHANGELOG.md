# Changelog

All notable changes to Compze.Tessaging.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- The tessaging sql layers store in the endpoint's prefixed table-set (`EndpointTableSet`, resolved from the container): the inbox, outbox, outbox-dispatching, and peer-memory tables are per-endpoint prefixed (`«EndpointName»_InboxTessages`), so any number of endpoints store side by side in one domain database, and `SchemaCreationSql` became a function of the table-set. New: `MsSqlEndpointCatalogSqlLayer` — the domain database's one shared, deliberately unprefixed `EndpointCatalog` table with the process lease's conditional claim/heartbeat/release statements (insert-if-absent race-safe via `UPDLOCK, HOLDLOCK`).
- The tessaging SQL layer implementations are async end to end (`SaveTessageAsync`, `MarkAsReceivedAsync`, `GetUndeliveredTessagesForEndpointAsync`, the inbox marks, the peer-registry members — matching the now-async `ITessagingSqlLayer`): exactly-once tessaging is database I/O, and synchrony-follows-the-type reaches the storage first. Same SQL, async command and reader plumbing.
- `MsSqlTessagingSqlLayer()` demands the SQL Server type-id interner itself (`MsSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.
- `MsSqlDomainDatabase(connectionStringName)` on `ExactlyOnceEndpointBuilder`: declares that the domain database this endpoint joins is SQL Server, filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing — the connection pool, the type-id interner Tessaging's sql layers share, and Tessaging's SQL Server inbox/outbox sql layers.

## 0.1.0-alpha

- Initial pre-release
