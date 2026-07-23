# Changelog

All notable changes to Compze.DocumentDb.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.1-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `PgSqlDocumentDbSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`PgSqlSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.
- `PgSqlDocumentDbSqlLayer()` demands the type-id interner itself (`PgSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
