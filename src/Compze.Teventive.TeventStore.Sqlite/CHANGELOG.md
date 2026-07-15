# Changelog

All notable changes to Compze.Tessaging.Teventive.TeventStore.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.Sqlite` is now `Compze.Teventive.TeventStore.Sqlite` — the namespaces this package's name has promised all along.
- `SqliteTeventStoreSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`SqliteSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.

## 0.1.0-alpha

- Initial pre-release
