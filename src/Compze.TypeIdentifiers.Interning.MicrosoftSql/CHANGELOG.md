# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `MsSqlEndpointPersistence` is gone. The endpoint-database declaration (`MsSqlEndpointDatabase`) lives in `Compze.Internals.Sql.MicrosoftSql`, and this package is purely the interner again — the sql-layer features demand `MsSqlTypeIdInterner()` themselves, so interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
