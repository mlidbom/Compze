# Changelog

All notable changes to Compze.Tessaging.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `MsSqlTessagingSqlLayer()` demands the SQL Server type-id interner itself (`MsSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.
- `AddDistributedTessaging(compose)` on `EndpointFoundation<MsSqlEndpointDatabase>`: adds distributed Tessaging to an endpoint whose database is SQL Server, registering Tessaging's SQL Server inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
