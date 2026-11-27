# Changelog

All notable changes to Compze.DbPool.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- **The in-memory layer, `SqliteMemoryDbPoolSqlLayer`, lets the pools on a machine create up to 1000 databases,** where every layer allowed 100: an in-memory database costs memory only while a pool holds it, and is private to that pool. Compze's own test suite held 152 at once on a machine with 32 logical processors, and with 100 its tests timed out waiting for a free database.
- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DbPool 0.3.0-alpha (was 0.2.2-alpha), Compze.Sql.Sqlite 0.5.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.2-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.1-alpha

- Refactoring.
- Version aligned with Compze.Internals.Sql.* 0.2.1-alpha.

## 0.1.0-alpha.1

### Changed

Extracted from monolithic package.
