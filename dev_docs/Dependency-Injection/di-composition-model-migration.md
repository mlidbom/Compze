# DI Composition Model Migration: ACM → CCM

## Decision

Migrate from Ambient Composition Model (AsyncLocal scope tracking) to Closure Composition Model (explicit scope objects). The dominant .NET containers (MS DI, Autofac) use CCM. Fighting them with ambient tracking causes ongoing pain (Autofac event subscriptions, disposal races, scope push/pop bugs).

## Current State

- Phases 1–3 complete. All AsyncLocal scope tracking removed.
- Callers resolve from the scope object they hold, not from ambient context.
- Container adapters are thin wrappers — no `AsyncLocal`, no scope tracking, no `PushExternalScope`/`PopExternalScope`.
- Phase 4 complete (see [di-container-abstraction-redesign.md](di-container-abstraction-redesign.md)): `ILegacyContainer` and `IServiceLocator` deleted, builder/container split done, all code uses new interface hierarchy.

## Target State — Achieved

- Callers resolve from the scope object they hold, not from ambient context
- Container adapters become thin wrappers — no `AsyncLocal`, no scope tracking
- Containers create scopes and forget about them; callers own scope lifetime

## Migration Progress

### Phase 1: Add explicit resolution to scope objects — DONE

`BeginScope()` returns `IScope` with `IScopeResolver Resolver` and `IDisposable`.

### Phase 2: Migrate callers to use explicit scope — DONE

All callers migrated from ambient resolution (`serviceLocator.Resolve()` while in scope) to explicit scope resolution (`scope.Resolve()` / `scope.Resolver.Resolve()`). Infrastructure code threads scope objects through handler signatures. ASP.NET middleware creates scopes and passes `IScopeResolver` to controller activators via `HttpContext.Items`.

### Phase 3: Remove AsyncLocal tracking — DONE

Removed from both container adapters (Microsoft DI and Autofac). `AsyncLocal` scope stacks, event subscriptions, `PushExternalScope`/`PopExternalScope` — all gone. Root `Resolve()` always uses the built container. `BeginScope()` creates a native scope without ambient tracking. `IsInScope()` eliminated — replaced with `kernel is ScopedKernel` check.

## Scope/Resolver Separation — DONE

Production code that resolves within a scope now receives `IScopeResolver` (resolution capability only), never `IScope` (lifetime management). `IScope` is held only by the code that owns the scope's lifetime.

- `TransactionalExtensions` (`ExecuteInIsolatedScope` etc.) pass `scope.Resolver` to lambdas
- `TypermediaHandlerExecutor`, `EndpointRequestExecutor`, `InfrastructureQueryExecutor` — handler delegates take `IScopeResolver`
- ASP.NET middleware stores `scope.Resolver` in `HttpContext.Items`; `CompzeControllerActivator` retrieves `IScopeResolver`
- Test code that manages scopes directly uses `scope.Resolver.X()` for resolution

Interface hierarchy: `IServiceResolver` (resolve by type) ← `IScopeResolver` (scoped resolution) and `IRootResolver` (root resolution). `IDependencyInjectionContainer` exposes `IRootResolver` and `IScopeFactory` via properties.

## ASP.NET Integration — Current Problem

### What exists now (interim)

Each endpoint runs two separate Kestrel instances (`AspNetInboxTransportServer` + `TypermediaTransportServer`), each with its own `WebApplication`. Both share one Compze container. Custom `CompzeControllerActivator` + per-request middleware manually creates Compze scopes and stashes `IScopeResolver` in `HttpContext.Items`. This only works for MVC controller activation — SignalR, minimal APIs, health checks, hosted services etc. all go through ASP.NET's standard `IServiceProvider` pipeline, which knows nothing about the Compze container.

### Why naive `UseAsServiceProviderFor` doesn't work

`UseAsServiceProviderFor(builder.Host)` hooks into `IServiceProviderFactory<T>`, which finalizes the container when `builder.Build()` is called. Two problems:

1. **One container, two hosts**: Two Kestrel instances share one Compze container. `UseAsServiceProviderFor` ties container finalization to one specific `WebApplicationBuilder.Build()` call. The second server can't also finalize the same container.
2. **Chicken-and-egg**: The transport servers are currently resolved *from* the built container as singletons. But `UseAsServiceProviderFor` must be called *before* the container is built. The server can't participate in wiring if it doesn't exist until after wiring is complete.

### Why `UseAsServiceProviderFor` can't work with the current architecture

See [di-container-abstraction-redesign.md](di-container-abstraction-redesign.md) for the builder/built type split and child container solution.

## Next Steps

See [di-container-abstraction-redesign.md](di-container-abstraction-redesign.md) for the interface redesign (builder/built type split, ISP for container abstractions, child container builder, ASP.NET Core integration).

## Key Insight

Domain code never resolves directly — it gets dependencies injected by the infrastructure that created the scope. The ambient model was only used by infrastructure code that could just as easily hold a scope reference.
