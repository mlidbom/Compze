# DI Composition Model Migration: ACM → CCM

## Decision

Migrate from Ambient Composition Model (AsyncLocal scope tracking) to Closure Composition Model (explicit scope objects). The dominant .NET containers (MS DI, Autofac) use CCM. Fighting them with ambient tracking causes ongoing pain (Autofac event subscriptions, disposal races, scope push/pop bugs).

## Current State

- Phases 1–3 complete. All AsyncLocal scope tracking removed.
- Callers resolve from the scope object they hold, not from ambient context.
- Container adapters are thin wrappers — no `AsyncLocal`, no scope tracking, no `PushExternalScope`/`PopExternalScope`.
- `IsInScope()` abstract method eliminated — `TryCreateTransientInstance` checks `kernel is ScopedKernel` directly.

## Target State — Achieved

- Callers resolve from the scope object they hold, not from ambient context
- Container adapters become thin wrappers — no `AsyncLocal`, no scope tracking
- Containers create scopes and forget about them; callers own scope lifetime

## Migration Progress

### Phase 1: Add explicit resolution to scope objects — DONE

`BeginScope()` returns `IServiceLocatorScope` with `Resolve<T>()` and `IDisposable`.

### Phase 2: Migrate callers to use explicit scope — DONE

**What was migrated:**
- `ExecuteInIsolatedScope` / `ExecuteTransactionInIsolatedScope` helpers — already used scope correctly
- All test `BeginScope()` sites — migrated from `using(x.BeginScope()) { x.Resolve<T>(); }` to `using var scope = x.BeginScope(); scope.Resolve<T>();`
- `InfrastructureQueryExecutor` / `InfrastructureQueryRegistrarWithDependencyInjectionSupport` — threaded `IScopeServiceLocator` through handler signatures. The registrar passes the scope to factory methods, and the executor passes its scope to query handlers.
- ASP.NET middleware (`AspNetInboxTransportServer`, `TypermediaTransportServer`) — stash `IServiceLocatorScope` in `HttpContext.Items["Compze.Scope"]`. `CompzeControllerActivator` reads scope from `HttpContext.Items` and resolves the controller from that scope.
- Test code fixes: `Local_Tuery_Performance_tests`, `TeventStoreUpdaterTest`, `DocumentDbTests` — changed from `_serviceLocator.Resolve<T>()` (root) to `scope.Resolve<T>()` inside scope lambdas.

**Already explicit (no changes needed):**
- `TypermediaHandlerExecutor` — passes scope to handlers
- `EndpointRequestExecutor` — calls `scope.Resolve<IServiceBusSession>()`
- `Inbox.HandlerExecutionEngine.QueuedTessage` — passes scope to handlers

### Phase 3: Remove AsyncLocal tracking — DONE

Removed from all three container adapters:
- `MicrosoftDependencyInjectionContainer` — removed `_scopeStack` AsyncLocal, `ScopeStack` property, `CurrentProvider()`, `PushExternalScope`/`PopExternalScope`. Root `Resolve()` always uses `_serviceProvider`. `BeginScope()` creates a native scope without pushing to any stack. Removed `IMicrosoftContainerInternals.PushExternalScope`/`PopExternalScope`.
- `AutofacDependencyInjectionContainer` — removed `_currentScope` AsyncLocal, `SubscribeToExternalScopeTracking`, `OnChildLifetimeScopeBeginning` event handlers. Root `Resolve()` always uses `_container`. `BeginScope()` creates from the root container.
- `SimpleInjectorDependencyInjectionContainer` — `IsInScope()` changed to `false`. SimpleInjector's own `AsyncScopedLifestyle` is internal to that library and doesn't affect our scope model.
- Removed `IsInScope()` abstract method from `DependencyInjectionContainerBase` — replaced with `kernel is ScopedKernel` check in `TryCreateTransientInstance`.
- Deleted `When_a_scope_is_created_externally_via_Autofac_BeginLifetimeScope` test class (tested removed ambient tracking behavior).
- Simplified `HostableMicrosoftContainer.CompzeMicrosoftServiceProvider.CreateScope()` — delegates to native provider, no scope syncing.

## ASP.NET Integration — Future Direction

The `Hostable*Container` projects (`Compze.DependencyInjection.Microsoft.Extensions.Hosting`, `Compze.DependencyInjection.Autofac.Extensions.Hosting`) originally existed to synchronize Compze's AsyncLocal scope tracking with ASP.NET's scoping. They implement custom `IServiceProviderFactory<T>`, custom `IServiceProvider`, custom `IServiceScope` — all to intercept scope creation.

Now that AsyncLocal is removed, these custom wrappers are simplified but still exist as integration points. The bridge projects should eventually be further simplified to use each container's standard ASP.NET Core integration:
- **MS DI**: Merge Compze's `ServiceDescriptor`s into ASP.NET's `IServiceCollection`. Let ASP.NET build the `ServiceProvider` normally.
- **Autofac**: Standard `AutofacServiceProviderFactory` + `Populate()`.
- **SimpleInjector**: Standard `AddSimpleInjector()` / `UseSimpleInjector()` with cross-wiring.

This would eliminate the custom controller activators too — ASP.NET would activate controllers from its own scopes, which contain the same registrations. The `HttpContext.Items` approach currently handles the interim state.

## Key Insight

Domain code never resolves directly — it gets dependencies injected by the infrastructure that created the scope. The ambient model was only used by infrastructure code that could just as easily hold a scope reference.
