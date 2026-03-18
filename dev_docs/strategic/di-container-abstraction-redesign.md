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
IContainerBuilder              — IComponentRegistrar Registrar, Build() → IContainer
IContainer                     — IRootResolver Resolver, IScopeFactory ScopeFactory, CreateChildContainerBuilder()
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
- **`IContainer`**: The built container. Composes `IRootResolver`, `IScopeFactory`, and `CreateChildContainerBuilder()` — doesn't inherit from them. Only orchestration code (endpoint startup, container lifecycle) holds this.
- **`IRootResolver`**: Root-level resolution (singletons, transients). Goes to code that only needs to resolve — `Endpoint`, registrars.
- **`IScopeFactory`**: Creates scopes. Goes to code that only needs to create scopes — transport servers, executors.
- **`IScopeResolver`**: Resolution within a scope (all lifestyles). Goes to handler code, controller activators — anything operating inside a scope.
- **`IServiceScope`**: Lifetime boundary. Owns an `IScopeResolver`. Disposed by the code that created the scope.
- **`IServiceResolver`**: Internal base. Only used by `InstantiationSpec.RunFactoryMethod()` so factory lambdas can accept either root or scoped resolvers.

### Composition over inheritance in order to achieve Interface Segregation Principle

Both `IContainerBuilder` and `IContainer` compose their capabilities via properties:
- `IContainerBuilder` *has* an `IComponentRegistrar` (doesn't inherit registration)
- `IContainer` *has* an `IRootResolver` and `IScopeFactory` (doesn't inherit resolution or scope creation)

Each sub-interface stays independently injectable. Consumers receive exactly what they need:
- Wiring code → `IComponentRegistrar`
- Orchestration code → `IContainerBuilder` (registration + finalization)
- Entry points → `IRootResolver`
- Request handlers → `IScopeFactory`
- In-scope code → `IScopeResolver`

### Key properties

- Builder can't resolve. Built container can't register. Type system enforces phases.
- Builder doesn't register — the registrar does. Builder only finalizes.
- `IContainer` doesn't inherit resolution or scope creation — it *has* them via properties.
- Each consumer receives exactly the capability it needs. ISP at the foundation level.

## Child Container Builder

`IContainer.CreateChildContainerBuilder()` returns an `IContainerBuilder` for a child container:
- All parent registrations inherited
- **Singletons delegate to parent** (same instance, not disposed by child)
- Scoped/transient registrations copied (fresh instances in child's scopes)
- Child builder accepts additional registrations (ASP.NET Core services)
- Child builds into its own independent `IContainer`

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

Child container builder = same loop but all singletons take the delegated path. `ContainerFacadeServiceTypes` exclusion still applies — `IContainer` etc. get fresh registrations pointing to the child.
