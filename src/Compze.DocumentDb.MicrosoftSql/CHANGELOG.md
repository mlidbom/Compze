# Changelog

All notable changes to Compze.DocumentDb.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.1-alpha

- **Requires exact versions of the Compze packages whose internals it uses:** Compze.DocumentDb 0.3.0-alpha (was 0.3.0-alpha or newer), Compze.Sql.Common 0.9.1-alpha (not required directly before), Compze.Sql.MicrosoftSql 0.8.1-alpha (was 0.5.0-alpha or newer). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DocumentDb 0.3.0-alpha (was 0.2.0-alpha), Compze.Sql.MicrosoftSql 0.5.0-alpha (was 0.4.0-alpha), Compze.TypeIdentifiers.Interning 0.2.0-alpha (was 0.1.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.1-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `MsSqlDocumentDbSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`MsSqlSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.

## 0.1.0-alpha

- Initial pre-release
