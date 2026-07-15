# Changelog

All notable changes to Compze.Tessaging.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `PgSqlTessagingSqlLayer()` demands the PostgreSQL type-id interner itself (`PgSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.
- `AddExactlyOnceTessaging(compose)` on `EndpointFoundation<PgSqlEndpointDatabase>`: adds exactly-once Tessaging to an endpoint whose database is PostgreSQL, registering Tessaging's PostgreSQL inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
