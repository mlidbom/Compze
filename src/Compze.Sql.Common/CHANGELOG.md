# Changelog

All notable changes to Compze.Sql.Common will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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
