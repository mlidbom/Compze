# Changelog

All notable changes to Compze.Sql.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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
