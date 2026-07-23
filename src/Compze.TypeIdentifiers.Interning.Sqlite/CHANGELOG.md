# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- `SqliteEndpointPersistence` is gone. The domain-database declaration (`SqliteDomainDatabase`) lives in `Compze.Sql.Sqlite`, and this package is purely the interner again. New: `SqliteTypeIdInterner(SqliteDomainDatabase)` derives the interner's own database name ("«domain-database-name».TypeIdInterner") from the declaration — the one home of that naming convention; the sqlite feature pairings call it with the foundation's declaration.
- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `SqliteEndpointPersistence` is gone. The endpoint-database declaration (`SqliteEndpointDatabase`) lives in `Compze.Internals.Sql.Sqlite`, and this package is purely the interner again. New: `SqliteTypeIdInterner(SqliteEndpointDatabase)` derives the interner's own database name ("«endpoint-database-name».TypeIdInterner") from the endpoint's declaration — the one home of that naming convention; the sqlite feature pairings call it with the foundation's declaration.

## 0.1.0-alpha

- Initial pre-release
