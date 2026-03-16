# DI Composition Model Migration: ACM → CCM

## Decision

Migrate from Ambient Composition Model (AsyncLocal scope tracking) to Closure Composition Model (explicit scope objects). The dominant .NET containers (MS DI, Autofac) use CCM. Fighting them with ambient tracking causes ongoing pain (Autofac event subscriptions, disposal races, scope push/pop bugs).

## Current State

- `BeginScope()` returns `IDisposable`, pushes scope onto `AsyncLocal` stack
- `Resolve<T>()` implicitly reads current scope from `AsyncLocal`
- Scope objects are never passed as parameters — completely ambient
- Each container adapter (Microsoft, SimpleInjector, Autofac) maintains its own `AsyncLocal` tracking

## Target State

- `BeginScope()` returns a scope object with `Resolve<T>()` and `CreateScope()` capabilities
- Callers resolve from the scope object they hold, not from ambient context
- Container adapters become thin wrappers — no `AsyncLocal`, no scope tracking
- Containers create scopes and forget about them; callers own scope lifetime

## Migration Strategy — Incremental

### Phase 1: Add explicit resolution to scope objects (non-breaking)

Change `BeginScope()` return type from `IDisposable` to a new interface (e.g. `IServiceScope`) that has both `IDisposable` and `Resolve<T>()` / `CreateScope()`. AsyncLocal tracking stays. All existing `using(serviceLocator.BeginScope()) { ... }` code continues to work unchanged.

### Phase 2: Migrate callers to use explicit scope

Convert call sites one at a time from:
```csharp
using(serviceLocator.BeginScope())
{
   var service = serviceLocator.Resolve<T>();
}
```
to:
```csharp
using var scope = serviceLocator.BeginScope();
var service = scope.Resolve<T>();
```

Priority order:
1. `ExecuteInIsolatedScope` / `ExecuteTransactionInIsolatedScope` — ~70 call sites use these, internal change only
2. Messaging infrastructure (`ExecuteTransactionalTessage`) — ~15 sites
3. ASP.NET middleware — 2 sites, needs coordination with framework scope model
4. Tests — ~50 sites, mechanical

### Phase 3 (Up for debate. We'll see) : Remove AsyncLocal tracking 

Once all callers use explicit scopes, delete the `AsyncLocal` infrastructure from all three container adapters.

## Call Site Inventory

| Category | Sites | Pattern |
|----------|-------|---------|
| `ExecuteInIsolatedScope` / `ExecuteTransactionInIsolatedScope` | ~70 | Lambda-based, internal change only |
| Startup-time `Resolve<T>()` (singletons from root) | ~20 | No scope involved, unchanged |
| Infrastructure `BeginScope()` (messaging, middleware) | ~15 | Needs explicit scope threading |
| Test `BeginScope()` | ~50 | Mechanical update |
| Domain code ambient resolution | 0 | Nothing to change |

## Key Insight

Domain code never resolves directly — it gets dependencies injected by the infrastructure that created the scope. The ambient model is only used by infrastructure code that could just as easily hold a scope reference.
