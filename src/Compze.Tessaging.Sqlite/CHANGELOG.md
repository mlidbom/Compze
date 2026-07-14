# Changelog

All notable changes to Compze.Tessaging.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `AddDistributedTessaging(compose)` on `EndpointFoundation<SqliteEndpointDatabase>`: adds distributed Tessaging to an endpoint whose database is sqlite, registering Tessaging's sqlite inbox/outbox sql layers — the engine pairing is routed by the compiler through the foundation's type.

## 0.1.0-alpha

- Initial pre-release
