# Changelog

All notable changes to Compze.TypeIdentifiers.Interning.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.1-alpha

- `MySqlEndpointPersistence` is gone. The domain-database declaration (`MySqlDomainDatabase`) lives in `Compze.Sql.MySql`, and this package is purely the interner again — the sql-layer features demand `MySqlTypeIdInterner()` themselves, so interner wiring vanishes from composing layers.
- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `MySqlEndpointPersistence` is gone. The endpoint-database declaration (`MySqlEndpointDatabase`) lives in `Compze.Internals.Sql.MySql`, and this package is purely the interner again — the sql-layer features demand `MySqlTypeIdInterner()` themselves, so interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
