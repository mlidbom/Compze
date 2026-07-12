# Changelog

All notable changes to Compze.DependencyInjection will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

### Added

- **Component sets — `ForSet<TService>()` and `ResolveSet<TComponent>()`.** A component can register as a member of a component set instead of a singular service: `Singleton.ForSet<TService>()` (and the `Scoped`/`TrackedTransient` equivalents) joins many registrations under one contract type, all returned together by `ResolveSet<TComponent>()` on `IServiceResolver`/`IScopeResolver`/`IRootResolver`/`IDependencyInjectionContainer`/`IScope`. A service type is exclusively singular or a component-set type, container-wide — `ContainerBuilder` rejects a type registered as both, in either order, while multiple set-member registrations sharing a type is the entire point and is unrestricted. A component that needs to be both independently resolvable and a set member gets there by composing two registrations (a normal one plus a set registration whose `CreatedBy(...)` depends on the first) — not by relaxing the exclusivity rule.
- **`CreatedBy(...)` constructor dependencies on a component-set type are now rejected at `Build()`.** Such a dependency is always resolved singularly, which would silently bind to whichever set member the container resolves first rather than the whole set — this is now a clear `Build()`-time error instead of a runtime surprise.

### Note

- **`ResolveSet<TComponent>()` makes no promise about result order.** Registrations are handed to the underlying container in the order they were received, but what order they come back out in during collection resolution is up to that container — this varies across the supported containers and is not part of Compze's contract.

## 0.5.0-alpha

### Changed

- **Polish the fluent registration API so that CreatedBy always comes last.** 

## 0.4.2-alpha

### Fixed

- **The `AllowSingletonDependent()`/`AllowScopedDependent()` opt-ins now travel to the `IServiceResolver<TService>` registrations that `WithServiceResolver()` adds.** Previously the resolver registrations were always created without the opt-ins, so a singleton could depend on an opted-in transient directly but was rejected when depending on its resolver instead.

## 0.4.1-alpha

### Changed

- **`WithAssociatedRegistrations()` now expands recursively.** An associated registration may itself carry associated registrations — every registration reachable this way is added to the container exactly once. Previously only one level was expanded and deeper levels were silently dropped.

### Fixed

- **Duplicate service types within a single `Register()` call — or among associated registrations — were not detected.** Duplicate-registration validation only checked new registrations against those already registered, never the new batch against itself. Both now throw the same "already registered" `InvalidOperationException`.

## 0.4.0-alpha

### Added

- **`WithAssociatedRegistrations()` — a general registration extension point.** A registration can carry extra registrations that are added to the container alongside it (validated and lifestyle-checked like any other). Feature helpers build on this instead of being baked into the core as special cases, and consumers can write their own such helpers.
- **`IServiceResolver<TService>` — the supported way to break a constructor-injection cycle.** Opt a component in with `.WithServiceResolver()` on its registration, then depend on `IServiceResolver<TService>` instead of the service itself; the depending side is constructed immediately holding only the resolver and obtains the real service later via `Resolve()`. A resolver is exposed for each service type the component is registered under. Each is registered at the target's own lifestyle, so a dependency on it is subject to exactly the same lifestyle validation as a direct dependency (a singleton still may not take an `IServiceResolver<TScoped>`). Implemented as an ordinary extension on top of `WithAssociatedRegistrations()`, not a core special case. Replaces the old kludge of injecting the whole `IServiceResolver`/`IRootResolver` and resolving by hand.

### Fixed

- **Autofac adapter: the resolver handed to singleton and transient factory methods is now valid for the created component's lifetime, not just the resolve operation** (it wraps the owning `ILifetimeScope` rather than the operation-scoped `IComponentContext`). Required for deferred `IServiceResolver<TService>` resolution, which stores the resolver and uses it after construction; the other adapters already behaved this way.

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
- **Captive dependency validation.** Singleton→TrackedTransient and Scoped→TrackedTransient dependencies are rejected by default.
	- Opt-in via `.AllowSingletonDependent()` and `.AllowScopedDependent()` on transient registrations.
- **TrackedTransient lifestyle** (formerly UntrackedTransient): every resolve returns a new instance; the container disposes it with the resolving scope, or with the container itself when resolved from the root — matching Microsoft.Extensions.DependencyInjection's transient semantics. (A caller-owns-disposal plain `Transient` lifestyle was considered but judged too complex to be worth implementing.)
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
