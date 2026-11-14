# Changelog

All notable changes to Compze.DependencyInjection.DryIoc will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.Internals.Logging 0.7.0-alpha (was 0.4.1-alpha), Compze.Threading 0.12.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.1-alpha

- `DryIocContainer.CreateCloneContainerBuilder` is `protected`: only the base class calls it, so it leaves the public surface. Breaking for anyone who called it directly, which the narrowing established nobody does.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.5.0-alpha

### Added

- **Native support for component sets**, added to [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection/) 0.6.0-alpha: a `ForSet<TService>()` registration is added as an additional DryIoc registration under its service type using `IfAlreadyRegistered.AppendNotKeyed` (rather than the `Throw` policy singular registrations use), resolved back via `IEnumerable<T>`.

## 0.4.0-alpha

- Compatible with the deferred `IServiceResolver<TService>` resolution added in [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection/) 0.4.0-alpha; no adapter changes were required. Version bumped in lockstep with the Compze.DependencyInjection package family.

## 0.3.0-alpha

- Initial pre-release
