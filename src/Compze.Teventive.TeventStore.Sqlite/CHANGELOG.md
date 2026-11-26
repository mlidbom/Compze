# Changelog

All notable changes to Compze.Teventive.TeventStore.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **Reads a taggregate's history for update asynchronously,** for Compze.Teventive.TeventStore's part of `ITaggregateStoreUpdater.GetForUpdateAsync`: the database's write gate is waited for holding no thread. Requires the Compze.Teventive.TeventStore.Abstractions released with it, and the Compze.Sql.Sqlite.
- **Internal support for Compze.Teventive.TeventStore's deleted taggregates; nothing public changes:** the store reads the type of each taggregate's last tevent, to tell the histories that end with an `ITaggregateDeletedTevent`. An application that upgrades Compze.Teventive.TeventStore to 0.7.0-alpha must upgrade this package too: the earlier version fails against it at runtime.
- **Requires newer minor versions of other Compze packages:** Compze.Teventive.TeventStore.Abstractions 0.7.0-alpha (was 0.6.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.Sqlite 0.5.0-alpha (was 0.4.0-alpha), Compze.Teventive.TeventStore.Abstractions 0.6.0-alpha (was 0.5.0-alpha), Compze.TypeIdentifiers.Interning 0.2.0-alpha (was 0.1.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.1-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.Sqlite` is now `Compze.Teventive.TeventStore.Sqlite` — the namespaces this package's name has promised all along.
- `SqliteTeventStoreSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`SqliteSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.

## 0.1.0-alpha

- Initial pre-release
