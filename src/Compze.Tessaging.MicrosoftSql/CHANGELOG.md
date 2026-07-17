# Changelog

All notable changes to Compze.Tessaging.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `MsSqlTessagingSqlLayer()` demands the SQL Server type-id interner itself (`MsSqlTypeIdInterner()`) — interner wiring vanishes from composing layers.
- `MsSqlEndpointDatabase(connectionStringName)` on `ExactlyOnceEndpointBuilder`: declares that the endpoint's database is SQL Server, filling the exactly-once endpoint's one database parameter with the whole engine pairing — the connection pool, the type-id interner Tessaging's sql layers share, and Tessaging's SQL Server inbox/outbox sql layers.

## 0.1.0-alpha

- Initial pre-release
