# DI Container Abstraction Redesign

## Problem (solved)

The old `IServiceLocator` merged root resolution, scope creation, and container identity into one interface. `IDependencyInjectionContainer` hid the build/use boundary behind a `ServiceLocator` property getter that silently transitioned from "still configuring" to "built and locked." No type-level distinction between build phase and use phase.

This blocked ASP.NET Core integration: two Kestrel instances sharing one container couldn't both finalize it. The solution was the builder/container type split (Phase 4) plus child containers (Phase 5) that each get their own `IServiceProviderFactory` lifecycle.

## Interface Hierarchy (implemented)

```
IContainerBuilder              — IComponentRegistrar Registrar, Build() → IDependencyInjectionContainer
IDependencyInjectionContainer  — IRootResolver RootResolver, IScopeFactory ScopeFactory, Clone() → IContainerBuilder
IComponentRegistrar             — Register(), fluent API, testing strategy, Clone()
IRootResolver                  — Resolve() at root (singletons, transients)
IScopeFactory                  — BeginScope() → IScope
IScope                         — IScopeResolver Resolver, Dispose()
IScopeResolver                 — Resolve() within a scope (all lifestyles)
IServiceResolver               — internal base, only used by factory method pipeline
```

All interfaces defined in `Compze.DependencyInjection/Abstractions/_Compze.Utilities.DependencyInjection.Abstractions.cs`.

### Design rationale

- **`IContainerBuilder`**: Composes `IComponentRegistrar` and `Build()`. No `Register()` methods — registration is the registrar's job, building is the builder's job. Not disposable — the built container owns all resources. Orchestration code holds the builder to finalize. Wiring code receives `builder.Registrar` or just `IComponentRegistrar` directly.
- **`IComponentRegistrar`**: Unchanged role. 71 extension methods target it across the codebase. Testing strategy via subtype polymorphism (`TestingComponentRegistrar`). Wiring code receives this — never needs `Build()`. Owns `Clone()` for cloning the registrar with its registrations.
- **`IDependencyInjectionContainer`**: The built container. Composes `IRootResolver`, `IScopeFactory`, and `Clone()` — doesn't inherit from them. Only orchestration code (endpoint startup, container lifecycle) holds this.
- **`IRootResolver`**: Root-level resolution. Goes to code that only needs to resolve — `Endpoint`.
- **`IScopeFactory`**: Creates scopes. Goes to transport servers, executors, extension methods (`ExecuteInIsolatedScope`, etc.).
- **`IScopeResolver`**: Resolution within a scope. Goes to handler code, controller activators.
- **`IScope`**: Lifetime boundary. Owns an `IScopeResolver`. Disposed by the code that created the scope.
- **`IServiceResolver`**: Internal base. Only used by `InstantiationSpec.RunFactoryMethod()` so factory lambdas can accept either root or scoped resolvers.

### Key properties

- Builder can't resolve. Built container can't register. Type system enforces phases.
- Builder doesn't register — the registrar does. Builder only finalizes.
- `IDependencyInjectionContainer` doesn't inherit resolution or scope creation — it *has* them via properties.
- Each consumer receives exactly the capability it needs. ISP at the foundation level.

## Production consumer migration (complete)

All production consumers have been migrated to the new interfaces:

| Consumer | Interface | Role |
|----------|-----------|------|
| `Endpoint` | `IDependencyInjectionContainer` | Owns container lifecycle, exposes `IRootResolver` |
| `TypermediaHandlerExecutor` | `IScopeFactory` | Scope + transaction |
| `InfrastructureQueryExecutor` | `IScopeFactory` | Scope only |
| `TypermediaTransportServer` | `IScopeFactory` | Scope per request |
| `AspNetInboxTransportServer` | `IScopeFactory` | Scope per request |
| `Inbox` / `HandlerExecutionEngine` / `Coordinator` | `IScopeFactory` (via registration) | Scope per handler execution |
| `ServerEndpointBuilder` | `IContainerBuilder` | Registers into builder, calls `Build()` |
| `EndpointHost` | `Func<IContainerBuilder>` | Container factory |

`IEndpoint.ServiceLocator` returns `IRootResolver`. `IEndpointBuilder.Registrar` returns `IComponentRegistrar`.
`EndpointRequestExecutor` resolves `IScopeFactory` from `IRootResolver` for scope methods.
`ComponentRegistration.CreateCloneRegistration()` takes `IRootResolver`.

## Container Cloning

Cloning is **permanent** infrastructure (not being removed). It creates a new container with identical registrations, where a handful of hand-chosen registrations delegate their singleton instances to the source. Used in testing to create containers that are structurally independent except for special shared components like DbPool.

This is **distinct from child containers** (future). Child containers delegate **all** singletons to the parent. Cloning delegates **only** opted-in singletons.

`Clone()` lives on `IDependencyInjectionContainer`, returning `IContainerBuilder`. `IComponentRegistrar` also has `Clone()` for cloning the registrar with its registrations (used internally by the container clone implementation).

### Clone usage patterns

**Hosting path** (Clone → modify → build):
- `TestingEndpointHost` passes `rootContainer.Clone` to `EndpointHost`
- Each `RegisterEndpoint` call invokes the factory → gets a fresh clone
- `ServerEndpointBuilder.SetupContainer()` registers extensively into the clone before building

**Test paths** (Clone → build → use):
- DI cloning specs: `source.Clone()` → `Build()` → resolve/verify behavior
- Migration/integration tests: `container.CloneAndBuild()` → execute transactions
- Goes through `IDependencyInjectionContainer.Clone()` → `Build()` or the convenience `CloneAndBuild()` extension

## Child Container Builder (complete)

`IDependencyInjectionContainer.CreateChildContainerBuilder()` returns an `IContainerBuilder` for a child container:
- All parent registrations inherited
- **Singletons delegate to parent** (same instance, not disposed by child)
- Scoped/transient registrations copied (fresh instances in child's scopes)
- Child builder accepts additional registrations (ASP.NET Core services)
- Child builds into its own independent `IDependencyInjectionContainer`

Reuses the existing `Clone()` / `CreateCloneRegistration()` mechanism but flips the singleton default:
- `Clone()` creates new singleton instances by default (opt-in delegation via `DelegateToParentServiceLocatorWhenCloning()`)
- `CreateChildContainerBuilder()` delegates all singletons by default

Intrinsic container types (`IDependencyInjectionContainer`, `IRootResolver`, `IScopeFactory`) are auto-registered via closures during `Build()`, so clones and child containers automatically get fresh registrations pointing to themselves.

## ASP.NET Core Hosting Integration (complete)

Each transport server (`TypermediaTransportServer`, `AspNetInboxTransportServer`) receives `IChildContainerHostIntegration` via DI. On startup, it creates a child container and hooks it into the host's `IServiceProviderFactory` pipeline. ASP.NET Core's standard `ServiceBasedControllerActivator` handles controller activation — no custom activator or `HttpContext.Items` scope stashing needed.

### Hosting architecture

**Published public API** (in `Compze.DependencyInjection.*.Extensions.Hosting` packages):
- `CompzeMicrosoftServiceProviderFactory` / `CompzeAutofacServiceProviderFactory` — `IServiceProviderFactory<T>` implementations that merge host services into a Compze builder and build it
- `IChildContainerHostIntegration` — DI-agnostic abstraction for transport servers that need to host a child container
- `MicrosoftChildContainerHostIntegration` / `AutofacChildContainerHostIntegration` — concrete implementations

The factory classes are the only real logic: `CreateBuilder()` merges the host's `IServiceCollection` into the Compze builder's service collection, then `CreateServiceProvider()` builds the Compze container and returns the native `IServiceProvider` directly (no wrapper classes).

`IHostableContainer` and its implementations were removed — they were single-line wrappers over the factory constructors with no production consumers. `CompzeMicrosoftServiceProvider` and `CompzeAutofacServiceProvider` wrapper classes were also eliminated since the underlying providers already implement the required interfaces.

## Remaining work

None. All phases complete.

## Phase 4: Remove `ILegacyContainer` and `IServiceLocator` + Builder/Container split (complete)

Both legacy interfaces have been deleted. The single `DependencyInjectionContainer` class has been split into separate builder and container types. All code now uses the new interface hierarchy exclusively.

**Builder/Container split:**
- `ContainerBuilderBase` — abstract builder. Holds registrations, `IComponentRegistrar`, `Register()`, `Build()`. Not disposable. `Build()` throws on second call.
- `DependencyInjectionContainer` — abstract built container. Holds registration list copy, `Clone()`, `Dispose()`. Concrete types implement `IRootResolver` + `IScopeFactory`.
- `MicrosoftContainerBuilder` + `MicrosoftContainer` — replaced `MicrosoftDependencyInjectionContainer`
- `AutofacContainerBuilder` + `AutofacContainer` — replaced `AutofacDependencyInjectionContainer`

**Intrinsic type registration:**
- `IDependencyInjectionContainer`, `IRootResolver`, `IScopeFactory` are auto-registered into the underlying DI engine during `Build()` via closures — no `ContainerFacadeServiceTypes` needed.

**Interface renames:**
- `IServiceScope` → `IScope`

**Container infrastructure changes:**
- `Register()` is `internal void` on `ContainerBuilderBase` — external callers use `IComponentRegistrar`
- `RegisterInContainer()` is `protected abstract void` on `ContainerBuilderBase`
- `Clone()` on `DependencyInjectionContainer` creates a new builder via `CreateBuilderForClone()`, replays registrations with `CreateCloneRegistration()`
- No lazy initialization — `Build()` is explicit and one-shot

**Test wiring changes:**
- `DiContainerExtensions`: method names: `CreateContainerForTesting`, `SetupTestingContainer`, `CreateWithContainerRegistrations`, `CreateWithContainerRegistrationsAndCurrentTestsPluggableComponents`
- `ContainerCloner`: uses `IDependencyInjectionContainer.Clone()` + `Build()`; `CloneAndBuild()` convenience extension
- `TestClient`: takes `IDependencyInjectionContainer` instead of `IServiceLocator`
- `DependencyInjectionContainerFactory`: returns `IContainerBuilder` (named `CreateContainerBuilder`)
- `TestingEndpointHost.Create()`: accepts `IContainerBuilder` (builds it) or `IDependencyInjectionContainer` (uses it directly for restart scenarios)
- Convenience extensions on `IDependencyInjectionContainer`: `Resolve<T>()`, `BeginScope()`, `ExecuteInIsolatedScope(...)`, `ExecuteTransactionInIsolatedScope(...)`

### Phase 5: Child container builder (complete)
- `CreateChildContainerBuilder()` added to `IDependencyInjectionContainer`
- Reuses `CreateCloneRegistration()` mechanism with flipped singleton default

### Phase 6: ASP.NET Core integration via child containers (complete)
- Transport servers use `IChildContainerHostIntegration` to create child containers
- Each Kestrel gets its own child container via `UseChildContainerAsServiceProviderFor`
- `CompzeControllerActivator` and `HttpContext.Items` scope stashing removed
- ASP.NET Core's standard `ServiceBasedControllerActivator` handles activation
- `IHostableContainer`, `CompzeMicrosoftServiceProvider`, `CompzeAutofacServiceProvider` removed (unnecessary wrappers)
- `CompzeMicrosoftServiceProviderFactory` / `CompzeAutofacServiceProviderFactory` made public
