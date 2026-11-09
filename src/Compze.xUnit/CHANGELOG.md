# Changelog

All notable changes to Compze.xUnit will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

### Changed
- Requires xUnit.net v3 4.0 or later (below 5.0). xUnit.net 4.0 changed the extensibility API in ways that are not
  binary compatible with 3.x, so this version does not work with xUnit.net 3.x; stay on 0.1.1-alpha for that.

## 0.1.1-alpha

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.1.0-alpha

- Initial pre-release
