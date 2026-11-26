# Changelog

All notable changes to Compze.Sql.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.8.0-alpha

- **For Compze's SQL packages: a command's rows read asynchronously.**
- **Requires newer minor versions of other Compze packages:** Compze.Sql.Common 0.7.0-alpha (was 0.6.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.7.1-alpha

- **Documentation: on Linux, a synchronous call to the database waits for a free thread-pool thread to receive its answer,** since .NET completes a synchronous read of a socket that anything has used asynchronously on a pool thread. So a process whose pool threads block, such as on synchronous units of work waiting for one another's locks, can stall its own database calls for seconds, on Linux only. `MySqlDomainDatabase`'s remarks now say so.
- **A domain database's process locks: what Compze takes where data in the database must be used by one process at a time,** such as an exactly-once endpoint's tables. Each is a session-scoped lock of the server's own on a connection of its own, released when its holder releases it, or the server ends its holder's session. Their holders record themselves in the domain database's new `DatabaseProcessLockHolder` table, created at the database's first use when a feature that takes such a lock is registered: what each holds its lock for, which process, on which machine, and since when, for the message that refuses another.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.5.0-alpha (was 0.4.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.7.0-alpha

- **Internal: what Compze.Teventive.MemoryImageStateStore.MySql needs to keep a memory image state store in MySQL tables.**
- **A transaction that holds a durable resource already, such as another database's connection or a resource manager of your own, refuses Compze's MySQL connection with a message that says so,** and names the connection, the rule and what to do: keep each database, and each other durable resource, in transactions of its own. Before, it was .NET's "Implicit distributed transactions have not been enabled", which names neither. When your resource takes part second instead, the refusal is .NET's own, from your call: the `MySqlDomainDatabase` documentation now says so.
- **Creating the schema at a database's first use no longer waits for a free thread-pool thread.** A synchronous SQL layer, such as the tevent store's, blocked its thread on the asynchronous creation, whose continuations each needed a free thread-pool thread, while many units of work at once could hold every one, each blocked waiting for the creation. The creation is now synchronous for every caller, the asynchronous SQL layers' too, which block their thread for it once, at the database's first use.
- **A transaction holding Compze's MySQL connection commits the database as its decision:** the database commits once every other participant of the transaction has prepared, and before any is told the outcome. Compze begins the connection's transaction itself, at the ambient transaction's isolation level, and every connection opens with MySqlConnector's `AutoEnlist=false`, whatever the configured connection string says. Before, MySqlConnector's enlistment committed only when told the outcome, after participants told before it, such as Compze's units of work, which then acted on a commit that hadn't happened. And a commit that failed, as when the connection dropped, threw out of the transaction's notifications: the participants after it were never told, the transaction never completed, and the connection the transaction held, and the units of work's locks, were never released. Now a commit that fails aborts the transaction, or, when the connection broke before the server answered, leaves it in doubt, and every participant is told so.
  - **What a consumer could notice:** the connection takes part as its transaction's durable participant, as SQL Server's does, so a transaction that also holds another durable resource, such as a second database's connection, becomes distributed, which .NET refuses unless distributed transactions are allowed; with them allowed, the connection refuses to prepare a two-phase commit, and the transaction rolls back. Before, such a transaction committed the two one after the other. A volatile participant of the consumer's own keeps working, and is told the outcome once the database has committed.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.Common 0.6.0-alpha (was 0.5.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.6.0-alpha

- **Compze's MySQL transactions are plain local transactions, no longer XA transactions:** every connection opens with MySqlConnector's `UseXaTransactions=false`, whatever the configured connection string says. Before, MySqlConnector's default made each ambient transaction an XA transaction, with an `XA PREPARE` before each commit, and a process that died between the prepare and the commit left a prepared transaction on the server, holding its locks until it was resolved by hand. Compze never uses distributed transactions, so XA bought nothing.
- **Transactions on MySQL run at the isolation level they ask for:** READ COMMITTED for Compze's own, as on the other databases. MySqlConnector's XA path ignored it, so they ran at the server's default, REPEATABLE READ.
- **Internal: its internals are visible to Compze.Sql.InternalSpecifications,** Compze's own white-box specifications of the SQL layers, and each assembly they are visible to is listed once. Nothing public changes, and nothing a consumer sees.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.Common 0.5.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.Sql.Common 0.4.0-alpha (was 0.3.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **The package sheds the `Internals` label: `Compze.Internals.Sql.MySql` is `Compze.Sql.MySql`.** The label told consumers not to depend on the package, which was never true of something they compose with deliberately. Previously published as [Compze.Internals.Sql.MySql](https://www.nuget.org/packages/Compze.Internals.Sql.MySql/) 0.3.0-alpha.
- Schema creation serializes under the engine's named lock (`GET_LOCK`, acquired, run, and released on one connection — the lock is session-scoped): several endpoints joining one domain database create their schemas concurrently, from one process or many, and IF-NOT-EXISTS guards are not concurrency-safe DDL.
- `MySqlDomainDatabase(connectionStringName)`: declares the domain database this endpoint joins — registers the connection pool every sql layer the endpoint registers stores its data through; the sql layers wire their shared infrastructure (the type-id interner) themselves.
- The endpoint process lock is held here: a session-scoped lock (`GET_LOCK`) held for as long as the process that claimed the endpoint lives. It replaces the heartbeat lease, which could never tell a paused-but-alive holder from a dead one.
- The package shows only its doors: the domain-database declaration and its registrar are the public face; every plumbing type is internal behind `InternalsVisibleTo`.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-alpha

- `MySqlEndpointDatabase`: the declaration that an endpoint's database is MySQL, carried by `EndpointFoundation<MySqlEndpointDatabase>` so the features added on the foundation bind their MySQL sql layers through the compiler. The declaration itself lives here too — `MySqlEndpointDatabase(connectionStringName)` and its `ComposeEndpoint` composition form register the endpoint's connection pool; the sql-layer features wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
