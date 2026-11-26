# Changelog

All notable changes to Compze.Sql.Common will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **Internal: a database's process locks,** each held by one process at a time on whichever machine, which Compze.Tessaging's exactly-once endpoints and Compze.Teventive.MemoryImageStateStore's tables take: see Compze.Sql.MicrosoftSql and its siblings for what a consumer sees in the database.
- **Internal: what the memory image state store kept in database tables needs from Compze's SQL layers,** for the packages Compze.Teventive.MemoryImageStateStore.Sqlite, .MicrosoftSql, .PostgreSql and .MySql.
- **A transaction that aborts while one of its operations uses its connection, as when the connection's own transaction can't take part in it, no longer hides why behind an `ObjectDisposedException`.** The transaction's completion disposed the connection's lock under the operation, which then failed releasing it, throwing that instead of what aborted the transaction.
- **Internal: what Compze's SQL layers need to say that a transaction holds a durable resource already,** with a message naming the connection and the rule. See their changelogs for what a consumer sees.
- **More units of work at once than the database provider's pool holds connections no longer stall every transaction on the database.** A transaction's connection opened, on its first use, under the lock on every transaction's connection, and opening waited for the provider's pool to hand one out, while the transactions holding them could give them back only through that same lock. Every transaction on the database waited, until the open timed out, or the lock after two minutes: on the SQL Server, PostgreSQL and MySQL layers, from more than a hundred units of work at once, with the providers' default pool sizes. A transaction's connection now opens outside that lock, and a connection that fails to open is no longer kept as the transaction's connection, failing each later use in the transaction, and its completion.
- **Internal: what Compze's SQLite and MySQL connections need to commit as their transaction's decision,** and a connection that fails to open in a transaction is disposed, not left open. See Compze.Sql.Sqlite's and Compze.Sql.MySql's changelogs for what a consumer sees.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.SystemCE 0.10.0-internal (was 0.9.0-internal), Compze.Threading 0.13.0-alpha (was 0.12.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- **A database connection that Compze opens in a transaction takes part in it as a durable resource,** so a durable resource that can't share its transaction with one refuses it, with a message naming both: by default, a Compze.Teventive.MemoryImageStateStore kept in files, in a unit of work that writes its files, since a crash between their commits would lose one of them. Nothing public changes. With an earlier version, such a unit of work on PostgreSQL, MySQL or SQLite isn't refused: use this one with Compze.Teventive.MemoryImageStateStore.
- **Internal: its internals are visible to Compze.Sql.InternalSpecifications,** Compze's own white-box specifications of the SQL layers. Nothing public changes, and nothing a consumer sees.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.10.0-alpha (was 0.9.0-alpha), Compze.Internals.SystemCE 0.9.0-internal (was 0.8.0-internal). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.Internals.Logging 0.7.0-alpha (was 0.5.0-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.6.0-internal), Compze.Threading 0.12.0-alpha (was 0.10.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.1-alpha

- **Async sql layers prepare statements without blocking a thread.** `ExecutePreparedNonQueryAsync`, `ExecutePreparedScalarAsync` and `ExecutePreparedReaderAsync` prepare with `PrepareAsync` before executing, for the sql layers' async code, which called the synchronous `PrepareStatement`: a round trip to the server that held a thread-pool thread for its whole duration. `PrepareStatement` is for synchronous code only.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.Logging 0.5.0-alpha (was 0.4.1-alpha), Compze.Internals.SystemCE 0.6.0-internal (was 0.4.0-internal), Compze.Threading 0.10.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **The package sheds the `Internals` label: `Compze.Internals.Sql.Common` is `Compze.Sql.Common`.** The label told consumers not to depend on the package, which was never true of something they compose with deliberately. Previously published as [Compze.Internals.Sql.Common](https://www.nuget.org/packages/Compze.Internals.Sql.Common/) 0.2.1-alpha.
- `ICompzeDbConnection.ExecuteScalarAsync(commandText)` — the async twin of the existing `ExecuteScalar` convenience.
- Async command plumbing rounded out: `IDbConnectionPool.UseCommandAsync` (the convenience the sync side already had) and `ExecuteReaderAndSelectAsync` (the async reader-loop twin).
- **The package goes fully dark**: every type in it is internal, reachable only by the Compze packages granted `InternalsVisibleTo`. It ships because they depend on it, not because anything in it is meant to be used directly.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
