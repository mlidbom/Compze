# Changelog

All notable changes to Compze.DocumentDb.MySql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `MySqlDocumentDbSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`MySqlSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.
- `MySqlDocumentDbSqlLayer()` demands the type-id interner itself (`MySqlTypeIdInterner()`) — interner wiring vanishes from composing layers.

## 0.1.0-alpha

- Initial pre-release
