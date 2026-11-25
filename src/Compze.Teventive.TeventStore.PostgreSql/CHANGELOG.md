# Changelog

All notable changes to Compze.Teventive.TeventStore.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.1-alpha

- **Saving a taggregate's tevents no longer waits for a free thread-pool thread.** Each tevent was inserted by one synchronous command of two statements, an INSERT and an UPDATE, and Npgsql 9 sends the second and later statements of a synchronous command, and flushes the command, from the thread pool, while the calling thread waits for the answer. With every pool thread busy, as under many units of work at once, the database received nothing until the pool added a thread, and each save waited for it. Each statement is now a command of its own, and a tevent migration sets the tevents' effective versions in one statement, rather than one per tevent in one command.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.PostgreSql 0.6.0-alpha (was 0.5.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **Internal support for Compze.Teventive.TeventStore's deleted taggregates; nothing public changes:** the store reads the type of each taggregate's last tevent, to tell the histories that end with an `ITaggregateDeletedTevent`. An application that upgrades Compze.Teventive.TeventStore to 0.7.0-alpha must upgrade this package too: the earlier version fails against it at runtime.
- **Requires newer minor versions of other Compze packages:** Compze.Teventive.TeventStore.Abstractions 0.7.0-alpha (was 0.6.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.PostgreSql 0.5.0-alpha (was 0.4.0-alpha), Compze.Teventive.TeventStore.Abstractions 0.6.0-alpha (was 0.5.0-alpha), Compze.TypeIdentifiers.Interning 0.2.0-alpha (was 0.1.1-alpha), Compze.TypeIdentifiers.Interning.PostgreSql 0.3.0-alpha (was 0.2.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.1-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.*` is now `Compze.Teventive.TeventStore.PostgreSql.*` — the namespaces this package's name has promised all along.
- `PgSqlTeventStoreSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`PgSqlSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.
- `PgSqlTeventStoreSqlLayer()` demands the type-id interner itself (`PgSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
