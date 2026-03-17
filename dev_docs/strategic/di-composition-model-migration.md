# DI Composition Model Migration: ACM → CCM

## Decision

Migrate from Ambient Composition Model (AsyncLocal scope tracking) to Closure Composition Model (explicit scope objects). The dominant .NET containers (MS DI, Autofac) use CCM. Fighting them with ambient tracking causes ongoing pain (Autofac event subscriptions, disposal races, scope push/pop bugs).

## Current State

- `BeginScope()` returns `IServiceLocatorScope` with `Resolve<T>()` — Phase 1 complete
- All test and `ExecuteInIsolatedScope` call sites resolve from the scope object — Phase 2 partially complete
- AsyncLocal tracking still active in all three container adapters
- Infrastructure messaging/middleware sites still use ambient resolution

## Target State

- Callers resolve from the scope object they hold, not from ambient context
- Container adapters become thin wrappers — no `AsyncLocal`, no scope tracking
- Containers create scopes and forget about them; callers own scope lifetime

## Migration Progress

### Phase 1: Add explicit resolution to scope objects — DONE

`BeginScope()` returns `IServiceLocatorScope` with `Resolve<T>()` and `IDisposable`.

### Phase 2: Migrate callers to use explicit scope — IN PROGRESS

**Done:**
- `ExecuteInIsolatedScope` / `ExecuteTransactionInIsolatedScope` helpers — already used scope correctly in their lambdas (~70 call sites, no changes needed)
- All test `BeginScope()` sites — 55 sites across 15 files migrated from `using(x.BeginScope()) { x.Resolve<T>(); }` to `using var scope = x.BeginScope(); scope.Resolve<T>();`

**Remaining:**
- `InfrastructureQueryExecutor` / `InfrastructureQueryRegistrarWithDependencyInjectionSupport` — handler signature is `Func<object, object>`, has no scope parameter. The registrar's `Resolve<T>()` calls the root locator relying on AsyncLocal. Need to thread scope through: change handler signature, pass scope from `ExecuteQuery`.
- ASP.NET middleware (`AspNetInboxTransportServer`, `TypermediaTransportServer`) — both create a Compze scope in middleware but discard it (`_ => next.Invoke()`). Controller activators (`CompzeControllerActivator`, `ServiceLocatorControllerActivator`) resolve from root locator, relying on AsyncLocal. Interim fix: stash scope in `HttpContext.Items`, activators pull from there.

**Already explicit (no changes needed):**
- `TypermediaHandlerExecutor` — passes scope to handlers
- `EndpointRequestExecutor` — calls `scope.Resolve<IServiceBusSession>()`
- `Inbox.HandlerExecutionEngine.QueuedTessage` — passes scope to handlers

### Phase 3: Remove AsyncLocal tracking

Once all callers use explicit scopes, delete the `AsyncLocal` infrastructure from all three container adapters. Three files:
- `MicrosoftDependencyInjectionContainer` — `_scopeStack` AsyncLocal + `ScopeStack` property + `PushExternalScope`/`PopExternalScope`
- `AutofacDependencyInjectionContainer` — `_currentScope` AsyncLocal + `SubscribeToExternalScopeTracking` + event handlers
- `SimpleInjectorDependencyInjectionContainer` — uses SimpleInjector's built-in `AsyncScopedLifestyle`

## ASP.NET Integration — Future Direction

The `Hostable*Container` projects (`Compze.DependencyInjection.Microsoft.Extensions.Hosting`, `Compze.DependencyInjection.Autofac.Extensions.Hosting`) currently exist to synchronize Compze's AsyncLocal scope tracking with ASP.NET's scoping. They implement custom `IServiceProviderFactory<T>`, custom `IServiceProvider`, custom `IServiceScope` — all to intercept scope creation.

Once AsyncLocal is removed, these custom wrappers lose their reason to exist. The bridge projects should be simplified to use each container's standard ASP.NET Core integration:
- **MS DI**: Merge Compze's `ServiceDescriptor`s into ASP.NET's `IServiceCollection`. Let ASP.NET build the `ServiceProvider` normally.
- **Autofac**: Standard `AutofacServiceProviderFactory` + `Populate()`.
- **SimpleInjector**: Standard `AddSimpleInjector()` / `UseSimpleInjector()` with cross-wiring.

This eliminates the custom controller activators too — ASP.NET activates controllers from its own scopes, which contain the same registrations.

This is separate work from the AsyncLocal removal — do it after Phase 3. The `HttpContext.Items` approach unblocks Phase 3 without requiring the bridge refactoring.

## Key Insight

Domain code never resolves directly — it gets dependencies injected by the infrastructure that created the scope. The ambient model is only used by infrastructure code that could just as easily hold a scope reference.
