# Changelog

All notable changes to Compze.DbPool.MicrosoftSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.2-alpha

- The package shows only its registrar: every plumbing type is internal, reachable by the Compze packages granted `InternalsVisibleTo` and by nothing else.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.1-alpha

- Refactoring.
- Version aligned with Compze.Internals.Sql.* 0.2.1-alpha.

## 0.1.0-alpha.1

### Changed

Extracted from monolithic package.
