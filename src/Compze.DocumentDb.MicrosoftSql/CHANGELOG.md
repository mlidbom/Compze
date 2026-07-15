# Changelog

All notable changes to Compze.DocumentDb.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `MsSqlDocumentDbSqlLayer()` contributes its schema-creation SQL through the engine's schema-contribution seam (`MsSqlSchemaContribution`) — schema wiring vanishes from composing layers, and the public `SchemaCreationSql` property is removed.

## 0.1.0-alpha

- Initial pre-release
