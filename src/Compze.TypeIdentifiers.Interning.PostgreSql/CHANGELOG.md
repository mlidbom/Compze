# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `PgSqlEndpointPersistence` is gone. The domain-database declaration (`PgSqlDomainDatabase`) lives in `Compze.Internals.Sql.PostgreSql`, and this package is purely the interner again — the sql-layer features demand `PgSqlTypeIdInterner()` themselves, so interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
