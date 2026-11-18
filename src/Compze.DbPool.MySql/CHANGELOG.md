# Changelog

All notable changes to Compze.DbPool.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **Clears the pool of Compze's own connections before it drops a database; nothing public changes:** MySqlConnector keys its pools by connection string, and Compze.Sql.MySql's new version connects with its own settings added to the configured string, so the pool is now named by that string. An application that upgrades Compze.Sql.MySql to 0.6.0-alpha must upgrade this package too: the earlier version names another pool, and clears nothing.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.MySql 0.6.0-alpha (was 0.5.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DbPool 0.3.0-alpha (was 0.2.2-alpha), Compze.Sql.MySql 0.5.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.2-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.1-alpha

- Refactoring.
- Version aligned with Compze.Internals.Sql.* 0.2.1-alpha.

## 0.1.0-alpha.1

### Changed

Extracted from monolithic package.
