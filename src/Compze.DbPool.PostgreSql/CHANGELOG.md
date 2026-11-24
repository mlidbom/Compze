# Changelog

All notable changes to Compze.DbPool.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- **Internal: clears the pool of a test database by the connection string Compze's PostgreSQL connections now open with,** which turns Npgsql's enlistment off. Nothing public changes.
- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DbPool 0.3.0-alpha (was 0.2.2-alpha), Compze.Sql.PostgreSql 0.5.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.2-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.1-alpha

- Refactoring.
- Version aligned with Compze.Internals.Sql.* 0.2.1-alpha.

## 0.1.0-alpha.1

### Changed

Extracted from monolithic package.
