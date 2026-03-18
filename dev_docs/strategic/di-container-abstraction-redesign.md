# DI Container Abstraction Redesign

## Problem

`IServiceLocator` merges root resolution, scope creation, and container identity into one interface. `IDependencyInjectionContainer` hides the build/use boundary behind a `ServiceLocator` property getter that silently transitions from "still configuring" to "built and locked." No type-level distinction between build phase and use phase.

This blocks ASP.NET Core integration: `UseAsServiceProviderFor(builder.Host)` must be called during wiring, but transport servers are resolved from the already-built container. Two Kestrel instances share one container — `UseAsServiceProviderFor` ties finalization to one specific `WebApplicationBuilder.Build()` call.

## Current Usage Analysis

No production code needs both root resolution and scope creation from the same interface:

- **Resolve only**: `Endpoint`, `TypermediaHandlerRegistrarWithDependencyInjectionSupport`
- **BeginScope only**: `AspNetInboxTransportServer`, `TypermediaTransportServer`, `TypermediaHandlerExecutor`, `Inbox.HandlerExecutionEngine.Coordinator`
- **Both**: nothing in production
- **Clone/child**: test infrastructure only

`IServiceResolver` (the base resolution interface) is only invoked in `InstantiationSpec.RunFactoryMethod()` and its extension methods — the factory method registration pipeline. All other code uses `IScopeResolver` or `IRootResolver`.

## Target Interface Hierarchy

```
IContainerBuilder              — IComponentRegistrar Registrar, Build() → IDependencyInjectionContainer
IDependencyInjectionContainer  — IRootResolver Resolver, IScopeFactory ScopeFactory, CreateChildContainerBuilder()
IComponentRegistrar             — Register(), fluent API, testing strategy (unchanged role)
IRootResolver                  — Resolve() at root (singletons, transients)
IScopeFactory                  — BeginScope() → IServiceScope
IServiceScope                  — IScopeResolver Resolver, Dispose()
IScopeResolver                 — Resolve() within a scope (all lifestyles)
IServiceResolver               — internal base, only used by factory method pipeline
```

### Design rationale

- **`IContainerBuilder`**: Composes `IComponentRegistrar` and `Build()`. No `Register()` methods — registration is the registrar's job, building is the builder's job. Orchestration code holds the builder to finalize. Wiring code receives `builder.Registrar` or just `IComponentRegistrar` directly.
- **`IComponentRegistrar`**: Unchanged role. Fluent registration API (44+ extension methods target it). Testing strategy via subtype polymorphism (`TestingComponentRegistrar`). Wiring code receives this — never needs `Build()`.
- **`IDependencyInjectionContainer`**: The built container. Composes `IRootResolver`, `IScopeFactory`, and `CreateChildContainerBuilder()` — doesn't inherit from them. Only orchestration code (endpoint startup, container lifecycle) holds this.
- **`IRootResolver`**: Root-level resolution (singletons, transients). Goes to code that only needs to resolve — `Endpoint`, registrars.
- **`IScopeFactory`**: Creates scopes. Goes to code that only needs to create scopes — transport servers, executors.
- **`IScopeResolver`**: Resolution within a scope (all lifestyles). Goes to handler code, controller activators — anything operating inside a scope.
- **`IServiceScope`**: Lifetime boundary. Owns an `IScopeResolver`. Disposed by the code that created the scope.
- **`IServiceResolver`**: Internal base. Only used by `InstantiationSpec.RunFactoryMethod()` so factory lambdas can accept either root or scoped resolvers.

### Composition over inheritance in order to achieve Interface Segregation Principle

Both `IContainerBuilder` and `IDependencyInjectionContainer` compose their capabilities via properties:
- `IContainerBuilder` *has* an `IComponentRegistrar` (doesn't inherit registration)
- `IDependencyInjectionContainer` *has* an `IRootResolver` and `IScopeFactory` (doesn't inherit resolution or scope creation)

Each sub-interface stays independently injectable. Consumers receive exactly what they need:
- Wiring code → `IComponentRegistrar`
- Orchestration code → `IContainerBuilder` (registration + finalization)
- Entry points → `IRootResolver`
- Request handlers → `IScopeFactory`
- In-scope code → `IScopeResolver`

### Key properties

- Builder can't resolve. Built container can't register. Type system enforces phases.
- Builder doesn't register — the registrar does. Builder only finalizes.
- `IDependencyInjectionContainer` doesn't inherit resolution or scope creation — it *has* them via properties.
- Each consumer receives exactly the capability it needs. ISP at the foundation level.

## Child Container Builder

`IDependencyInjectionContainer.CreateChildContainerBuilder()` returns an `IContainerBuilder` for a child container:
- All parent registrations inherited
- **Singletons delegate to parent** (same instance, not disposed by child)
- Scoped/transient registrations copied (fresh instances in child's scopes)
- Child builder accepts additional registrations (ASP.NET Core services)
- Child builds into its own independent `IDependencyInjectionContainer`

### How this solves ASP.NET Core integration

Each Kestrel gets its own child container. `UseAsServiceProviderFor` is called on the child builder. ASP.NET Core finalizes the child, not the parent. Scoping, SignalR, minimal APIs — everything works through standard ASP.NET DI because the child IS the `IServiceProvider`.

Two Kestrel instances, two child containers, each with its own `UseAsServiceProviderFor`. Parent owns Kestrel lifecycle. Kestrel owns child container lifecycle. No circular ownership. Singletons (event store, document db, etc.) are the same instance across all children.

### Implementation approach

Reuses the existing `Clone()` / `CreateCloneRegistration()` mechanism but flips the singleton default:
- `Clone()` creates new singleton instances by default (opt-in delegation via `DelegateToParentServiceLocatorWhenCloning()`)
- `CreateChildContainerBuilder()` delegates all singletons by default

The existing `CreateCloneRegistration()` already handles both paths:
- Non-delegated: copy the factory (new instances in child)
- Delegated: resolve from parent, register as instance in child

Child container builder = same loop but all singletons take the delegated path. `ContainerFacadeServiceTypes` exclusion still applies — `IDependencyInjectionContainer` etc. get fresh registrations pointing to the child.

## Implementation Plan

Incremental migration — new interfaces alongside old ones, consumers migrated one by one, old interfaces deleted when empty. Compiles and tests pass at every step.

### Phase 1: Define new interfaces + adapter implementation — DONE
- `IContainerBuilder` and `IDependencyInjectionContainer` defined in the abstractions file
- `DependencyInjectionContainer` implements both (adapter — delegates to existing `IServiceLocator`)
- `IContainerBuilder.Registrar` returns the existing `IComponentRegistrar`
- `IContainerBuilder.Build()` triggers the existing lazy build and returns `IDependencyInjectionContainer`
- `IDependencyInjectionContainer.Resolver` returns the `IServiceLocator` as `IRootResolver`
- `IDependencyInjectionContainer.ScopeFactory` returns the `IServiceLocator` as `IScopeFactory`
- Old `IDependencyInjectionContainer` renamed to `ILegacyContainer` (temporary, removed in Phase 3)
- New types registered in `ServerEndpointBuilder`: `IContainerBuilder`, `IDependencyInjectionContainer`, `IRootResolver`, `IScopeFactory`
- `Endpoint` internal resolution switched from `IServiceLocator` to `IRootResolver`
- All 2231 tests pass

### Phase 2: Migrate remaining consumers to new interfaces — DONE
- Extension methods (`ExecuteInIsolatedScope`, `ExecuteTransactionInIsolatedScope`, etc.) moved from `IServiceLocator` to `IScopeFactory`
- `TypermediaHandlerRegistrarWithDependencyInjectionSupport` → `IRootResolver` (resolve only)
- `TypermediaHandlerExecutor` → `IScopeFactory` (scope + transaction)
- `InfrastructureQueryExecutor` → `IScopeFactory` (scope only)
- `TypermediaTransportServer` → `IScopeFactory` (scope per request)
- `AspNetInboxTransportServer` → `IScopeFactory` (scope per request)
- Inbox chain (`HandlerExecutionEngine` → `Coordinator` → `HandlerExecutionTask`) → `IScopeFactory`
- `Inbox` constructor: removed unused `IServiceLocator` parameter
- All factory registrations (`RegisterWith`, `CreatedBy`) updated to resolve `IScopeFactory`
- All tests pass

### Phase 3: Migrate public API and reduce legacy surface — DONE
- `IEndpoint.ServiceLocator` → `IRootResolver` (was `IServiceLocator`)
- `Endpoint` takes `IDependencyInjectionContainer`, disposes it directly
- `IEndpointBuilder.Container` → `IEndpointBuilder.Registrar` (type `IComponentRegistrar`)
- `ServerEndpointBuilder.Build()` uses `IContainerBuilder.Build()` instead of `.ServiceLocator`
- `EndpointRequestExecutor` resolves `IScopeFactory` from `IRootResolver` for scope methods
- `ComponentRegistration.CreateCloneRegistration()` takes `IRootResolver` (was `IServiceLocator`)
- `Inbox` removed unused `ILegacyContainer` constructor parameter
- All `builder.Container` callers migrated to `builder.Registrar`
- All 2228 tests pass

## Container Cloning

Cloning is **permanent** infrastructure (not being removed). It creates a new container with identical registrations, where a handful of hand-chosen registrations delegate their singleton instances to the source. Used in testing to create containers that are structurally independent except for special shared components like DbPool.

This is **distinct from child containers** (Phase 4). Child containers delegate **all** singletons to the parent. Cloning delegates **only** opted-in singletons. In tests, both are used together: clone a base container for independent test configs, then create child containers from clones for per-Kestrel instances.

### Clone usage patterns

**Hosting path** (Clone → modify → build):
- `TestingEndpointHost` passes `rootContainer.Clone` as `Func<ILegacyContainer>` to `EndpointHost`
- Each `RegisterEndpoint` call invokes the factory → gets a fresh clone
- `ServerEndpointBuilder.SetupContainer()` registers extensively into the clone before building
- **The clone is always modified before use in the hosting path**

**Test paths** (Clone → use immediately):
- DI cloning specs: `source.Clone()` → immediately resolve/verify behavior
- Migration/integration tests: `serviceLocator.Clone()` → immediately execute transactions
- These simulate "new app instance" scenarios — clone is used as a built container directly
- Goes through `ContainerCloner.Clone()` extension which adds one self-reference registration then accesses `.ServiceLocator`

### Where Clone() should live

Currently on `ILegacyContainer`. Target: `IDependencyInjectionContainer.Clone()` returning `IContainerBuilder`.

Returning `IContainerBuilder` (not a built container) because:
- The hosting path needs to register into the clone before building
- Test paths that use clones directly just call `.Build()` immediately
- Consistent with the builder pattern — clone produces something you can configure further

### Remaining `IServiceLocator` / `ILegacyContainer` references

**Container infrastructure** (74 total, src + test):
- Interface definitions: `IServiceLocator`, `ILegacyContainer` in abstractions
- Container implementations: Microsoft/Autofac containers implement `IServiceLocator`
- Container base class: `DependencyInjectionContainer` implements `ILegacyContainer`, has `Clone()`, `Register()`, `ServiceLocator`
- `IComponentRegistrar`: `Container()`, `SetContainer(ILegacyContainer)` — registrar→container coupling
- Cloning: `Clone()`, `ContainerCloner`, `IServiceLocator` self-registration in clones

**Hosting** (still uses `ILegacyContainer` for container factories):
- `EndpointHost`: `Func<ILegacyContainer>` container factory
- `TestingEndpointHostBase`: passes factory to `EndpointHost`
- `ServerEndpointBuilder`: `internal ILegacyContainer Container` — registers into it, then builds
- `TestingEndpointHost`: creates/clones `ILegacyContainer`, passes `.Clone` method group

**Test wiring**:
- `DiContainerExtensions`: factory methods return `ILegacyContainer`, register `IServiceLocator`
- `ContainerCloner`: casts `IServiceLocator` → `ILegacyContainer` to call `Clone()`
- `TestClient`: stores `IServiceLocator` for resolution + disposal
- ~12 test files: store `IServiceLocator` or `ILegacyContainer` for test operations

### Phase 4: Move Clone() to IDependencyInjectionContainer
- Add `Clone()` to `IDependencyInjectionContainer`, returning `IContainerBuilder`
- Migrate `EndpointHost` from `Func<ILegacyContainer>` to `Func<IContainerBuilder>`
- Migrate `ServerEndpointBuilder` from `ILegacyContainer` to `IContainerBuilder`
- Migrate `TestingEndpointHost` / `DiContainerExtensions` / `ContainerCloner`
- Migrate DI spec tests and integration tests that call `Clone()`
- Remove `ILegacyContainer` and `IServiceLocator` interfaces
- Build + test

### Phase 5: Child container builder
- Add `CreateChildContainerBuilder()` to `IDependencyInjectionContainer`
- Reuses `CreateCloneRegistration()` mechanism with flipped singleton default:
  - `Clone()`: new singleton instances by default, opt-in delegation via `DelegateToParentServiceLocatorWhenCloning()`
  - `CreateChildContainerBuilder()`: all singletons delegate to parent by default
- Build + test

### Phase 6: ASP.NET Core integration via child containers
- Rework transport servers to use child containers
- Each Kestrel gets `IContainerBuilder` from `CreateChildContainerBuilder()`
- `UseAsServiceProviderFor` on the child builder
- Remove custom `CompzeControllerActivator` and `HttpContext.Items` scope stashing
- Build + test
