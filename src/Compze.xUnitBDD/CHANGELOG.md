# Changelog

All notable changes to Compze.xUnitBDD will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-beta

### Changed
- Requires xUnit.net v3 4.0 or later (below 5.0). xUnit.net 4.0 changed the extensibility API in ways that are not
  binary compatible with 3.x, in either direction: 0.5.1-beta fails at runtime under xUnit.net 4.x, and this version
  fails under xUnit.net 3.x. Projects still on xUnit.net 3.x should stay on 0.5.1-beta.

## 0.5.1-beta

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.5.0-beta

- Initial release
