# Changelog

All notable changes to Compze.DependencyInjection.LightInject will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

### Added

- **Native support for component sets**, added to [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection/) 0.6.0-alpha: a `ForSet<TService>()` registration is added as an additional LightInject registration under its service type, given its own service name derived from its position in the registration list. Resolved back by looking each member up by that exact name, not via `GetAllInstances`/`IEnumerable<T>` — under `EnableMicrosoftCompatibility` (which this adapter always enables), `GetAllInstances` matches by assignability across the *entire* container rather than by exact registered service type, so it would incorrectly pull in any unrelated singularly-registered component whose concrete type happens to implement the set's contract type.

## 0.3.0-alpha

- Initial pre-release
