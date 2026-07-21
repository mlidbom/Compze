# Changelog

All notable changes to Compze.Internals.Sql.Common will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.1-alpha

- `ICompzeDbConnection.ExecuteScalarAsync(commandText)` — the async twin of the existing `ExecuteScalar` convenience.
- Async command plumbing rounded out: `IDbConnectionPool.UseCommandAsync` (the convenience the sync side already had) and `ExecuteReaderAndSelectAsync` (the async reader-loop twin).
- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
