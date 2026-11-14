# Changelog

All notable changes to Compze.TypeIdentifiers will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.SystemCE 0.8.0-internal (was 0.4.0-internal). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.0-alpha

- **`ITypeMapper`/`TypeMapper` give way to `TypeMapBuilder` and an immutable `ITypeMap`.** A map is built once from every component's declared requirements and never mutated afterwards, so what a container persists a type as no longer depends on which component registered first. Composing the map from those declarations is `Compze.TypeIdentifiers.DependencyInjection`.
- `UseStableNameStrategyForPublicKeyToken` is deleted: nothing declares publisher-wide type-name stability by signature.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.1.0-alpha

- Initial pre-release
