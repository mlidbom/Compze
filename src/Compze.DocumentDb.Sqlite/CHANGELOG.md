# Changelog

All notable changes to Compze.DocumentDb.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `SqliteDocumentDbSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`SqliteSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.

## 0.1.0-alpha

- Initial pre-release
