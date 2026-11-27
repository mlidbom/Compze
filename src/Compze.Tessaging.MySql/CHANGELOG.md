# Changelog

All notable changes to Compze.Tessaging.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **The inbox admits a tommand an endpoint sends a deferred handler of its own (Compze.Tessaging 0.14.0-alpha), within the sender's transaction:** the inbox's admissions table, `<endpoint name>_InboxDeliveryStreamAdmissions`, gets a row whose sender is the endpoint itself, created when the endpoint starts, whose lock makes the sends to the endpoint itself commit one after another.
- **Requires exact versions of the Compze packages whose internals it uses:** Compze.Sql.Common 0.9.1-alpha (not required directly before), Compze.Sql.MySql 0.9.1-alpha (was 0.9.0-alpha or newer), Compze.Tessaging 0.15.0-alpha (was 0.14.0-alpha or newer). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- **The inbox table has a `FailingTimeMilliseconds` column:** how long a tessage's handling has been failing, recorded with each failed try, so that an exactly-once endpoint's retries carry across restarts. An existing database's inbox table, `<endpoint name>_InboxTessages`, lacks it: add it as `bigint NOT NULL DEFAULT 0`, or recreate the database.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.MySql 0.9.0-alpha (was 0.7.1-alpha), Compze.Tessaging 0.14.0-alpha (was 0.12.1-alpha), Compze.TypeIdentifiers.Interning.MySql 0.4.0-alpha (was 0.3.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.2-alpha

- **The endpoint catalog no longer has a `LockHolderDescription` column:** the holder of an endpoint's process lock records itself in the domain database's `DatabaseProcessLockHolder` table, as every holder of the domain database's process locks does. An existing database keeps the column, which nothing writes any more.
- **Requires newer minor versions of other Compze packages:** Compze.Tessaging 0.12.1-alpha (was 0.11.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.1-alpha

- **Starting an endpoint creates the database's tables synchronously, blocking the starting thread once, at the database's first use,** as Compze.Sql.MySql's schema creation is now synchronous for every caller: see its changelog for why. Nothing public changes.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.MySql 0.7.0-alpha (was 0.5.0-alpha), Compze.Tessaging 0.11.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.MySql 0.5.0-alpha (was 0.4.0-alpha), Compze.Tessaging 0.9.0-alpha (was 0.5.0-alpha), Compze.TypeIdentifiers.Interning 0.2.0-alpha (was 0.1.1-alpha), Compze.TypeIdentifiers.Interning.MySql 0.3.0-alpha (was 0.2.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- The tessaging sql layers store in the endpoint's prefixed table-set (`EndpointTableSet`, resolved from the container): the inbox, outbox, outbox-dispatching, and peer-memory tables are per-endpoint prefixed (`«EndpointName»_InboxTessages`), so any number of endpoints store side by side in one domain database, and `SchemaCreationSql` became a function of the table-set. New: `MySqlEndpointCatalogSqlLayer` — the domain database's one shared, deliberately unprefixed `EndpointCatalog` table recording each endpoint and the process holding its lock (insert-if-absent race-safe via `INSERT IGNORE`).
- The tessaging SQL layer implementations are async end to end (`SaveTessageAsync`, `MarkAsReceivedAsync`, `GetUndeliveredTessagesForEndpointAsync`, the inbox marks, the peer-registry members — matching the now-async `ITessagingSqlLayer`): exactly-once tessaging is database I/O, and synchrony-follows-the-type reaches the storage first. Same SQL, async command and reader plumbing.
- `MySqlDomainDatabase(connectionStringName)` on `ExactlyOnceEndpointBuilder`: declares that the domain database this endpoint joins is MySQL, filling the exactly-once endpoint's one domain-database parameter with the whole engine pairing — the connection pool, the type-id interner Tessaging's sql layers share, and Tessaging's MySQL inbox/outbox sql layers.
- **The storage follows the reliability work.** The endpoint catalog's sql layer carries a process **lock** rather than a lease — the heartbeat claim/renew/release statements are gone, replaced by a session-scoped lock held for the holder's lifetime. Two new per-endpoint tables serve exactly-once in-order delivery: the outbox's per-receiver delivery-stream counters and the inbox's delivery-stream admissions. The handling execution takes a row-level claim on its inbox row inside the handling transaction, and the inbox recovery scan re-enqueues admitted-but-unhandled tessages at start.
- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `MySqlTessagingSqlLayer()` demands the MySQL type-id interner itself (`MySqlTypeIdInterner()`) — interner wiring vanishes from composing layers.
- `AddExactlyOnceTessaging(compose)` on `EndpointFoundation<MySqlEndpointDatabase>`: adds exactly-once Tessaging to an endpoint whose database is MySQL, registering Tessaging's MySQL inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
