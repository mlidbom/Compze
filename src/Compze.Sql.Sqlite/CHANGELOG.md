# Changelog

All notable changes to Compze.Sql.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

- **Internal: what Compze.Teventive.MemoryImageStateStore.Sqlite needs to keep a memory image state store in SQLite tables.**
- **A transaction that holds a durable resource already, such as another database's connection or a resource manager of your own, refuses Compze's SQLite connection with a message that says so,** and names the connection, the rule and what to do: keep each database, and each other durable resource, in transactions of its own. Before, it was .NET's "Implicit distributed transactions have not been enabled", which names neither. When your resource takes part second instead, the refusal is .NET's own, from your call: the `SqliteDomainDatabase` and `SqliteMemoryDomainDatabase` documentation now says so.
- **Creating the schema at a database's first use no longer waits for a free thread-pool thread.** A synchronous SQL layer, such as the tevent store's, blocked its thread on the asynchronous creation, whose continuations each needed a free thread-pool thread, while many units of work at once could hold every one, each blocked waiting for the creation. The creation is now synchronous for every caller, the asynchronous SQL layers' too, which block their thread for it once, at the database's first use.
- **A transaction holding Compze's SQLite connection commits the database as its decision:** the database commits once every other participant of the transaction has prepared, and before any is told the outcome. Before, the connection committed only when told the outcome, after participants told before it, such as Compze's units of work, which then acted on a commit that hadn't happened. And a commit that failed, as on a full disk, threw out of the transaction's notifications: the participants after it were never told, the transaction never completed, and the connection the transaction held, and the units of work's locks, were never released. Now a commit that fails aborts the transaction, and every participant is told so.
  - **What a consumer could notice:** the connection takes part as its transaction's durable participant, as SQL Server's does, so a transaction that also holds another durable resource, such as a second database's connection, becomes distributed, which .NET refuses unless distributed transactions are allowed; with them allowed, the connection refuses to prepare a two-phase commit, and the transaction rolls back. Before, such a transaction committed the two one after the other. A volatile participant of the consumer's own keeps working, and is told the outcome once the database has committed.
- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.Sql.Common 0.4.0-alpha (was 0.3.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **The package sheds the `Internals` label: `Compze.Internals.Sql.Sqlite` is `Compze.Sql.Sqlite`.** The label told consumers not to depend on the package, which was never true of something they compose with deliberately. Previously published as [Compze.Internals.Sql.Sqlite](https://www.nuget.org/packages/Compze.Internals.Sql.Sqlite/) 0.3.0-alpha.
- `SqliteDomainDatabase`: the declaration that the domain database an endpoint joins is sqlite. `SqliteDomainDatabase(connectionStringName)` registers the connection pool, and the declaration type carries the connection-string name the sqlite pairings derive their wiring from — e.g. the type-id interner's own database name, which on sqlite lives in a separate database file.
- `SqliteMemoryDomainDatabase(databaseName)`: the public door to an in-memory sqlite domain database — the composition tests and samples reach for it directly instead of through test-only wiring.
- Auxiliary sqlite database pools: a consumer that needs a database of its own beside the domain database — its own file, and so its own per-database write gate — declares a pool type of its own and registers it through the auxiliary-database registrar, which mirrors the domain pool's configuration-vs-test-pool split. First consumer: the Tessaging peer registry on sqlite. The type-id interner's bespoke wiring predates the mechanism and remains.
- The endpoint process lock is held here: a session-scoped lock (an OS-level machine-wide mutex, sqlite having no server sessions) held for as long as the process that claimed the endpoint lives. It replaces the heartbeat lease, which could never tell a paused-but-alive holder from a dead one.
- The package shows only its doors: the domain-database declaration and its registrar are the public face; every plumbing type is internal behind `InternalsVisibleTo`.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-alpha

- `SqliteEndpointDatabase`: the declaration that an endpoint's database is sqlite, carried by `EndpointFoundation<SqliteEndpointDatabase>` so the features added on the foundation bind their sqlite sql layers through the compiler. The declaration itself lives here too — `SqliteEndpointDatabase(connectionStringName)` and its `ComposeEndpoint` composition form register the endpoint's connection pool; the sql-layer features wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
