# Changelog

All notable changes to Compze.xUnitBDD will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.7.0-beta

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.xUnit 0.3.0-alpha (was 0.2.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.6.0-beta

### Changed
- Requires xUnit.net v3 4.0 or later (below 5.0). xUnit.net 4.0 changed the extensibility API in ways that are not
  binary compatible with 3.x, in either direction: 0.5.1-beta fails at runtime under xUnit.net 4.x, and this version
  fails under xUnit.net 3.x. Projects still on xUnit.net 3.x should stay on 0.5.1-beta.

## 0.5.1-beta

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.5.0-beta

- Initial release
