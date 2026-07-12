# Changelog

All notable changes to Compze.DependencyInjection.DryIoc will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

### Added

- **Native support for component sets**, added to [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection/) 0.6.0-alpha: a `ForSet<TService>()` registration is added as an additional DryIoc registration under its service type using `IfAlreadyRegistered.AppendNotKeyed` (rather than the `Throw` policy singular registrations use), resolved back via `IEnumerable<T>`.

## 0.4.0-alpha

- Compatible with the deferred `IServiceResolver<TService>` resolution added in [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection/) 0.4.0-alpha; no adapter changes were required. Version bumped in lockstep with the Compze.DependencyInjection package family.

## 0.3.0-alpha

- Initial pre-release
