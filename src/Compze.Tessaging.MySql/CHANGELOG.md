# Changelog

All notable changes to Compze.Tessaging.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `MySqlTessagingSqlLayer()` demands the MySQL type-id interner itself (`MySqlTypeIdInterner()`) — interner wiring vanishes from composing layers.
- `AddExactlyOnceTessaging(compose)` on `EndpointFoundation<MySqlEndpointDatabase>`: adds exactly-once Tessaging to an endpoint whose database is MySQL, registering Tessaging's MySQL inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
