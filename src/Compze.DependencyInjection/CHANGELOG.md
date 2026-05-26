# Changelog

All notable changes to Compze.DependencyInjection will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

### Changed

- **Migrated from the Ambient Composition Model (ACM) to the Closure Composition Model (CCM).** AsyncLocal scope tracking is gone; callers resolve from an explicit `IScope` / `IScopeResolver` they hold. Container adapters are now thin wrappers — no more `AsyncLocal`, scope stacks, or `PushExternalScope`/`PopExternalScope`. This aligns Compze with the dominant .NET containers (Microsoft.Extensions.DependencyInjection, Autofac) and eliminates the disposal races, event-subscription pain, and scope push/pop bugs the ambient model produced.
- **Builder/container type split.** `IContainerBuilder` (configure phase) and `IDependencyInjectionContainer` (use phase) are now separate types. The type system enforces the boundary: builders can't resolve, built containers can't register. Enables ASP.NET integration where multiple Kestrel instances share a parent container via child containers.
- **New interface hierarchy** replacing the conflated `IServiceLocator`:
	- `IComponentRegistrar` — registration only
	- `IContainerBuilder` — registrar + `Build()`
	- `IDependencyInjectionContainer` — composes `IRootResolver`, `IScopeFactory`, `Clone()`
	- `IRootResolver` — root-level resolution (singletons, transients)
	- `IScopeFactory` — `BeginScope() → IScope`
	- `IScopeResolver` — resolution within a scope (all lifestyles)
	- `IScope` — owns scope lifetime, exposes its resolver
	- Each consumer receives exactly the capability it needs (ISP at the foundation).

### Added

- **Child container support.** Each child gets its own `IServiceProviderFactory` lifecycle — required for ASP.NET Core scenarios where the host's DI integration finalizes the container.
- **Captive dependency validation.** Singleton→Transient and Scoped→Transient registrations are rejected by default.
	- Opt-in via `.AllowSingletonDependent()` and `.AllowScopedDependent()` on transient registrations.
- **TrackedTransient and Transient lifestyles** (formerly UntrackedTransient):
	- `Transient` — caller owns disposal, matches SimpleInjector's transient semantics.
	- `TrackedTransient` — container disposes with the scope, matches Microsoft.Extensions.DependencyInjection's transient semantics.
- Throws on resolving scoped services from the root (with opt-out for MS DI compliance).

### Removed

- **`IServiceLocator` and `ILegacyContainer`** — replaced by the new interface hierarchy above.
- **SimpleInjector adapter** — no longer maintained; use Autofac, DryIoc, LightInject, or Microsoft.Extensions.DependencyInjection.

### Renamed

- Package renamed from [Compze.Utilities.DependencyInjection](https://www.nuget.org/packages/Compze.Utilities.DependencyInjection/). Previous releases under the old name: up to 0.2.0-alpha.1.

## 0.2.0-alpha.1 *(as Compze.Utilities.DependencyInjection)*

Refactoring.

## 0.1.0-alpha.3 *(as Compze.Utilities.DependencyInjection)*

- Initial pre-release
