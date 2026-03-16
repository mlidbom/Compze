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
- Infrastructure messaging (`Inbox.HandlerExecutionEngine`, `InfrastructureQueryExecutor`, etc.) — ~15 sites
- ASP.NET middleware (`AspNetInboxTransportServer`, `TypermediaTransportServer`) — 2 sites, scope parameter currently unused in lambda

### Phase 3: Remove AsyncLocal tracking

Once all callers use explicit scopes, delete the `AsyncLocal` infrastructure from all three container adapters.

## Key Insight

Domain code never resolves directly — it gets dependencies injected by the infrastructure that created the scope. The ambient model is only used by infrastructure code that could just as easily hold a scope reference.
