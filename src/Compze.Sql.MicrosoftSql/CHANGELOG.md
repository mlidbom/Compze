# Changelog

All notable changes to Compze.Sql.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **Internal: what Compze.Teventive.MemoryImageStateStore.MicrosoftSql needs to keep a memory image state store in SQL Server tables.**
- **A transaction that holds a durable resource already, such as another database's connection or a resource manager of your own, refuses Compze's SQL Server connection with a message that says so,** and names the connection, the rule and what to do: keep each database, and each other durable resource, in transactions of its own. Before, it was .NET's "Implicit distributed transactions have not been enabled", which names neither. When your resource takes part second instead, the refusal is .NET's own, from your call: the `MsSqlDomainDatabase` documentation now says so.
- **Creating the schema at a database's first use no longer waits for a free thread-pool thread.** A synchronous SQL layer, such as the tevent store's, blocked its thread on the asynchronous creation, whose continuations each needed a free thread-pool thread, while many units of work at once could hold every one, each blocked waiting for the creation. The creation is now synchronous for every caller, the asynchronous SQL layers' too, which block their thread for it once, at the database's first use.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.Common 0.6.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.Sql.Common 0.4.0-alpha (was 0.3.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **The package sheds the `Internals` label: `Compze.Internals.Sql.MicrosoftSql` is `Compze.Sql.MicrosoftSql`.** The label told consumers not to depend on the package, which was never true of something they compose with deliberately. Previously published as [Compze.Internals.Sql.MicrosoftSql](https://www.nuget.org/packages/Compze.Internals.Sql.MicrosoftSql/) 0.3.0-alpha.
- Schema creation serializes under the engine's application lock (`sp_getapplock`, acquired, run, and released on one connection — the lock is session-scoped): several endpoints joining one domain database create their schemas concurrently, from one process or many, and IF-NOT-EXISTS guards are not concurrency-safe DDL.
- `MsSqlDomainDatabase(connectionStringName)`: declares the domain database this endpoint joins — registers the connection pool every sql layer the endpoint registers stores its data through; the sql layers wire their shared infrastructure (the type-id interner) themselves.
- The endpoint process lock is held here: a session-scoped lock (`sp_getapplock`) held for as long as the process that claimed the endpoint lives. It replaces the heartbeat lease, which could never tell a paused-but-alive holder from a dead one.
- The package shows only its doors: the domain-database declaration and its registrar are the public face; every plumbing type is internal behind `InternalsVisibleTo`.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-alpha

- `MsSqlEndpointDatabase`: the declaration that an endpoint's database is SQL Server, carried by `EndpointFoundation<MsSqlEndpointDatabase>` so the features added on the foundation bind their SQL Server sql layers through the compiler. The declaration itself lives here too — `MsSqlEndpointDatabase(connectionStringName)` and its `ComposeEndpoint` composition form register the endpoint's connection pool; the sql-layer features wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
