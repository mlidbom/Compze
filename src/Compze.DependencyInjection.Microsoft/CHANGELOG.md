# Changelog

All notable changes to Compze.DependencyInjection.Microsoft will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

### Changed

- Adapter rewritten for the new [Closure Composition Model](https://www.nuget.org/packages/Compze.DependencyInjection/) in Compze.DependencyInjection 0.3.0-alpha. The Microsoft adapter is now a thin wrapper around `Microsoft.Extensions.DependencyInjection` — no `AsyncLocal` scope tracking, no scope push/pop, no event subscriptions. Scope lifetime is owned by the caller via `IScope`.
- Hosting integration moved to the new [Compze.DependencyInjection.Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Compze.DependencyInjection.Microsoft.Extensions.Hosting/) package.

### Renamed

- Package renamed from [Compze.Utilities.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.Utilities.DependencyInjection.Microsoft/). Previous releases under the old name: up to 0.2.0-alpha.1.

## 0.2.0-alpha.1 *(as Compze.Utilities.DependencyInjection.Microsoft)*

Refactoring.

## 0.1.0-alpha.4 *(as Compze.Utilities.DependencyInjection.Microsoft)*

- Initial pre-release
