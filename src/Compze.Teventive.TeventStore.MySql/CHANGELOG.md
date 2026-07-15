# Changelog

All notable changes to Compze.Tessaging.Teventive.TeventStore.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.*` is now `Compze.Teventive.TeventStore.MySql.*` — the namespaces this package's name has promised all along.
- `MySqlTeventStoreSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`MySqlSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.
- `MySqlTeventStoreSqlLayer()` demands the type-id interner itself (`MySqlTypeIdInterner()`) — interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
