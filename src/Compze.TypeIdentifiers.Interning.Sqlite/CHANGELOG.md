# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `SqliteEndpointPersistence` is gone. The endpoint-database declaration (`SqliteEndpointDatabase`) lives in `Compze.Internals.Sql.Sqlite`, and this package is purely the interner again. New: `SqliteTypeIdInterner(SqliteEndpointDatabase)` derives the interner's own database name ("«endpoint-database-name».TypeIdInterner") from the endpoint's declaration — the one home of that naming convention; the sqlite feature pairings call it with the foundation's declaration.

## 0.1.0-alpha

- Initial pre-release
