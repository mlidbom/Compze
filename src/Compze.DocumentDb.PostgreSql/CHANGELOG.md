# Changelog

All notable changes to Compze.DocumentDb.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.1-alpha

- **Requires exact versions of the Compze packages whose internals it uses:** Compze.DocumentDb 0.3.0-alpha (was 0.3.0-alpha or newer), Compze.Sql.Common 0.9.1-alpha (not required directly before), Compze.Sql.PostgreSql 0.8.1-alpha (was 0.8.0-alpha or newer). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **Storing a document type new to the database no longer waits for a connection only other units of work can give back:** the type-id interner takes its connections from a pool of its own on the database, at most two, in Compze.TypeIdentifiers.Interning.PostgreSql, which this version requires. It took them from the pool units of work hold their transactions' connections from, so with every connection held by units of work, the interning waited until the pool timed out.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.PostgreSql 0.8.0-alpha (was 0.6.0-alpha), Compze.TypeIdentifiers.Interning.PostgreSql 0.4.0-alpha (was 0.3.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.1-alpha

- **Internal: its tables are created by one statement per command,** as Compze.Sql.PostgreSql's schema creation now runs them: see its changelog for why. Nothing public changes.
- **Requires newer minor versions of other Compze packages:** Compze.Sql.PostgreSql 0.6.0-alpha (was 0.5.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DocumentDb 0.3.0-alpha (was 0.2.0-alpha), Compze.Sql.PostgreSql 0.5.0-alpha (was 0.4.0-alpha), Compze.TypeIdentifiers.Interning 0.2.0-alpha (was 0.1.1-alpha), Compze.TypeIdentifiers.Interning.PostgreSql 0.3.0-alpha (was 0.2.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.1-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `PgSqlDocumentDbSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`PgSqlSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.
- `PgSqlDocumentDbSqlLayer()` demands the type-id interner itself (`PgSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
