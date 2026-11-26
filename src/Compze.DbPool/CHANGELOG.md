# Changelog

All notable changes to Compze.DbPool will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.1-alpha

- **Depends on Compze.Threading.Interprocess instead of Compze.InterprocessObject, which is retired:** `IInterprocessObject<T>`, which it keeps its machine-wide state in, moved to Compze.Threading.Interprocess. Nothing public changes.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.10.0-alpha (was 0.9.0-alpha), Compze.Internals.SystemCE 0.12.0-internal (was 0.8.0-internal), Compze.Threading 0.14.0-alpha (was 0.12.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.Internals.Logging 0.7.0-alpha (was 0.4.1-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.4.0-internal), Compze.InterprocessObject 0.9.0-alpha (was 0.7.1-alpha), Compze.Threading 0.12.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.2-alpha

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.1-alpha

- Refactoring.
- Version aligned with Compze.Internals.Sql.* 0.2.1-alpha.

## 0.1.0-alpha.3

- Initial pre-release
