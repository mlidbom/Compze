# Changelog

All notable changes to Compze.Tessaging.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- Adding exactly-once Tessaging on a sqlite foundation also wires the type-id interner, derived from the foundation's declaration (`SqliteTypeIdInterner(SqliteEndpointDatabase)`) — on sqlite the interner has its own database, so the registrar-level `SqliteTessagingSqlLayer()` cannot demand it namelessly; registrar-level compositions register it explicitly.
- `AddExactlyOnceTessaging(compose)` on `EndpointFoundation<SqliteEndpointDatabase>`: adds exactly-once Tessaging to an endpoint whose database is sqlite, registering Tessaging's sqlite inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
