# Changelog

All notable changes to Compze.Sql.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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
