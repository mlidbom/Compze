# Changelog

All notable changes to Compze.Must will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.1-alpha

- The assertion machinery moves below `_private` — the assertion context, the call-name reader, the diff generator and the invoking-must-throw extensions. The `Must` vocabulary itself is unchanged.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.4.0-alpha

- Move extension methods back to `Compze.Must` namespace.

## 0.3.0-alpha

- The assertion classes moved from the `Compze.Must` namespace to `Compze.Must.Assertions` — the namespace now matches the folder they always lived in. Consumers add `using Compze.Must.Assertions;` beside `using Compze.Must;`.

## 0.2.1-alpha

- First release under the new name. Previously published as [Compze.Utilities.Testing.Must](https://www.nuget.org/packages/Compze.Utilities.Testing.Must/).

## 0.2.0-alpha.1 *(as Compze.Utilities.Testing.Must)*

Refactoring.

## 0.1.0-alpha.3 *(as Compze.Utilities.Testing.Must)*

- Initial pre-release
