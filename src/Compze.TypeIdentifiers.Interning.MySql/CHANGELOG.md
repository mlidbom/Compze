# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `MySqlEndpointPersistence` is gone. The endpoint-database declaration (`MySqlEndpointDatabase`) lives in `Compze.Internals.Sql.MySql`, and this package is purely the interner again — the sql-layer features demand `MySqlTypeIdInterner()` themselves, so interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
