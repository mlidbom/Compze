# Changelog

All notable changes to Compze.Sql.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **Documentation: on Linux, a synchronous call to the database waits for a free thread-pool thread to receive its answer,** since .NET completes a synchronous read of a socket that anything has used asynchronously on a pool thread. So a process whose pool threads block, such as on synchronous units of work waiting for one another's locks, can stall its own database calls for seconds, on Linux only. `PgSqlDomainDatabase`'s remarks now say so.
- **Internal: what Compze.Teventive.MemoryImageStateStore.PostgreSql needs to keep a memory image state store in PostgreSQL tables.**
- **A transaction holding Compze's PostgreSQL connection commits the database as its decision, with any other participants:** the database commits once every other participant of the transaction has prepared, and before any is told the outcome. Before, Npgsql's own enlistment committed as the decision only while the transaction had no other participant that prepares last, as a consumer's own enlistment with no options does: with one, Npgsql prepared a two-phase commit, which a PostgreSQL server refuses by default (max_prepared_transactions is 0), so every such transaction rolled back. Compze's connection now begins the database transaction itself, at the ambient transaction's isolation level, with Npgsql's enlistment off (`Enlist=false`).
  - **What a consumer could notice:** the connection takes part as its transaction's durable participant, as SQL Server's, MySQL's and SQLite's do, so a transaction that also holds another durable resource, such as a second database's connection, becomes distributed, which .NET refuses unless distributed transactions are allowed; with them allowed, the connection refuses to prepare a two-phase commit, and the transaction rolls back. A volatile participant of the consumer's own keeps working, and is told the outcome once the database has committed.
- **A transaction that holds a durable resource already, such as another database's connection or a resource manager of your own, refuses Compze's PostgreSQL connection with a message that says so,** and names the connection, the rule and what to do: keep each database, and each other durable resource, in transactions of its own. Before, it was .NET's "Implicit distributed transactions have not been enabled", which names neither. When your resource takes part second instead, the refusal is .NET's own, from your call: the `PgSqlDomainDatabase` documentation now says so.
- **A synchronous open no longer waits in Npgsql's pool for a free thread-pool thread.** When as many connections as the pool holds (its Max Pool Size) were in use, a synchronous open waited for one to come back on an event that Npgsql sets from the thread pool: with every pool thread busy, as under many units of work at once, each waiting for a connection, the connections that came back lay unclaimed until the pool added a thread. A hundred and twenty-eight units of work through a pool of ten took 12 s, against under 1 s on SQL Server and MySQL. Compze's connections now take a place in the pool before they open, and wait for one, if they must, where a returning connection wakes them itself: 0.7 s. A wait longer than the connection string's Timeout throws a TimeoutException that names the pool's Max Pool Size and Timeout, where Npgsql threw an NpgsqlException saying the pool was exhausted.
- **Creating the schema at a database's first use no longer waits for a free thread-pool thread.** A synchronous SQL layer, such as the tevent store's, blocked its thread on the asynchronous creation, whose continuations each needed a free thread-pool thread, while many units of work at once could hold every one, each blocked waiting for the creation. The creation is now synchronous for every caller, the asynchronous SQL layers' too, which block their thread for it once, at the database's first use. Each statement of the schema runs as a command of its own, since Npgsql 9 sends the second and later statements of a synchronous command from the thread pool: so each commits by itself, and a creation that fails part way is completed by the next.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.Common 0.6.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.Sql.Common 0.4.0-alpha (was 0.3.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **The package sheds the `Internals` label: `Compze.Internals.Sql.PostgreSql` is `Compze.Sql.PostgreSql`.** The label told consumers not to depend on the package, which was never true of something they compose with deliberately. Previously published as [Compze.Internals.Sql.PostgreSql](https://www.nuget.org/packages/Compze.Internals.Sql.PostgreSql/) 0.3.0-alpha.
- Schema creation serializes under the engine's advisory lock (`pg_advisory_lock`, acquired, run, and released on one connection — the lock is session-scoped): several endpoints joining one domain database create their schemas concurrently, from one process or many, and IF-NOT-EXISTS guards are not concurrency-safe DDL.
- `PgSqlDomainDatabase(connectionStringName)`: declares the domain database this endpoint joins — registers the connection pool every sql layer the endpoint registers stores its data through; the sql layers wire their shared infrastructure (the type-id interner) themselves.
- The endpoint process lock is held here: a session-scoped lock (`pg_try_advisory_lock`) held for as long as the process that claimed the endpoint lives. It replaces the heartbeat lease, which could never tell a paused-but-alive holder from a dead one.
- The package shows only its doors: the domain-database declaration and its registrar are the public face; every plumbing type is internal behind `InternalsVisibleTo`.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-alpha

- `PgSqlEndpointDatabase`: the declaration that an endpoint's database is PostgreSQL, carried by `EndpointFoundation<PgSqlEndpointDatabase>` so the features added on the foundation bind their PostgreSQL sql layers through the compiler. The declaration itself lives here too — `PgSqlEndpointDatabase(connectionStringName)` and its `ComposeEndpoint` composition form register the endpoint's connection pool; the sql-layer features wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
