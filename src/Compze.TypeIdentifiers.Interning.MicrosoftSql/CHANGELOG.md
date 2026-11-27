# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.1-alpha

- **Requires exact versions of the Compze packages whose internals it uses:** Compze.Sql.Common 0.9.1-alpha (not required directly before), Compze.Sql.MicrosoftSql 0.8.1-alpha (was 0.8.0-alpha or newer). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **Storing a type the database hasn't stored before no longer deadlocks the connection pool.** The interner reads and mints type ids outside the unit of work's transaction, and took its connections from the pool units of work hold their transactions' connections from. A unit of work that held a taggregate's lock and stored a tevent of a new type, while units of work waiting for that lock held every connection of the pool, got no connection, and all of them waited until the pool timed out, 15 s by default: the SQL Server tevent store's deadlock. The interner now takes its connections from a pool of its own on the database, of at most two connections, which the server shows as `Compze type id interner`'s: a database may have two connections more than before.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.MicrosoftSql 0.8.0-alpha (was 0.5.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.Sql.MicrosoftSql 0.5.0-alpha (was 0.4.0-alpha), Compze.TypeIdentifiers.Interning 0.2.0-alpha (was 0.1.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.1-alpha

- `MsSqlEndpointPersistence` is gone. The domain-database declaration (`MsSqlDomainDatabase`) lives in `Compze.Sql.MicrosoftSql`, and this package is purely the interner again — the sql-layer features demand `MsSqlTypeIdInterner()` themselves, so interner wiring vanishes from composing layers.
- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `MsSqlEndpointPersistence` is gone. The endpoint-database declaration (`MsSqlEndpointDatabase`) lives in `Compze.Internals.Sql.MicrosoftSql`, and this package is purely the interner again — the sql-layer features demand `MsSqlTypeIdInterner()` themselves, so interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
