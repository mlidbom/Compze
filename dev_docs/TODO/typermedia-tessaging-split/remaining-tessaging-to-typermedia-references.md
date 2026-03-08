# Remaining Tessaging → Typermedia References

After extracting `Compze.Typermedia.Client` and `Compze.Typermedia.Hosting.AspNetCore`, four Tessaging projects still reference Typermedia projects. This document catalogs every remaining reference, why it exists, and what it would take to remove it.

## Summary

| Tessaging project | References | Files | Root cause | Status |
|---|---|---|---|---|
| **Tessaging.Abstractions** | Typermedia | 1 | `IEndpointBuilder` exposes `TypermediaHandlerRegistrarWithDependencyInjectionSupport` | Open — hosting question |
| **Tessaging** | Typermedia, Typermedia.Client, Typermedia.Hosting | 1 | `ServerEndpointBuilder` dual-pipeline setup | Open — hosting question |
| **Tessaging.Hosting.AspNetCore** | Typermedia.Client, Typermedia.Hosting.AspNetCore | 2 | Shared ASP.NET host registers both controllers and transports | Open — hosting question |
| **Tessaging.Hosting.Testing** | Typermedia, Typermedia.Client | 4 | Test wiring mirrors production; `TestClient` is Typermedia-aware | Open — follows hosting |

`Compze.Tessaging.Teventive.TeventStore` has **no** Typermedia references — it is already fully decoupled.

### Resolved coupling (removed in this iteration)

| Former coupling | Resolution |
|---|---|
| `_TessageTypesInternal` → `ITypermediaHandlerRegistry` | Separate discovery: `TypermediaEndpointInformationQuery` in `Compze.Typermedia.Client` |
| `MemoryInboxTransportServer` → `TypermediaHandlerExecutor` + `InMemoryTypermediaNetwork` | `MemoryTypermediaTransportServer` + `ISupplementalTransportServer` |
| `MemoryInboxTransportServer` → `InfrastructureQueryExecutor` + `InMemoryInfrastructureNetwork` | `MemoryInfrastructureTransportServer` + `ISupplementalTransportServer` |
| `TyperMediaApi/EventStore/` in `Compze.Tessaging` | Moved to `Compze.Tessaging.Teventive.TeventStore.Typermedia` bridge project |

---

## 1. Compze.Tessaging.Abstractions → Compze.Typermedia

### IEndpointBuilder.cs

```csharp
public interface IEndpointBuilder
{
    IDependencyInjectionContainer Container { get; }
    EndpointConfiguration Configuration { get; }
    TessageHandlerRegistrarWithDependencyInjectionSupport RegisterTessagingHandlers { get; }
    TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterTypermediaHandlers { get; } // ← Typermedia
}
```

The `IEndpointBuilder` interface — the central abstraction for configuring an endpoint — has a **hard-coded** property for Typermedia handler registration alongside Tessaging handler registration. Every endpoint builder must provide both.

**Why it exists**: The current design treats Typermedia and Tessaging as co-equal tenants of every endpoint. Endpoint setup code (in samples, in `TeventStoreRegistrar`, etc.) calls `builder.RegisterTypermediaHandlers.ForTuery(...)` directly on the builder.

**To remove**: Make `IEndpointBuilder` paradigm-neutral. Options:
- **(a)** Remove `RegisterTypermediaHandlers` from `IEndpointBuilder` and have Typermedia registration happen through an extension method or a separate interface (`ITypermediaEndpointBuilder`).
- **(b)** Replace both specific registrar properties with a generic mechanism — e.g., `IEndpointBuilder.GetRegistrar<T>()` — so endpoint builders don't need to know which paradigms exist at compile time.

---

## 2. Compze.Tessaging → Compze.Typermedia + Typermedia.Client + Typermedia.Hosting

Four files create three distinct categories of coupling.

### 2a. ServerEndpointBuilder.cs — Dual pipeline setup

**References**: `Compze.Typermedia`, `Compze.Typermedia.Hosting`, `Compze.Typermedia.HandlerRegistration`

The `ServerEndpointBuilder` is the implementation of `IEndpointBuilder`. It:
- Creates a `TypermediaHandlerRegistry` (from `Compze.Typermedia`)
- Creates a `TypermediaHandlerRegistrarWithDependencyInjectionSupport` (from `Compze.Typermedia.HandlerRegistration`)
- Calls `TypermediaHandlerExecutor.RegisterWith(...)` (from `Compze.Typermedia.Hosting`)
- Calls `.InProcessTypermediaNavigator()` (from `Compze.Typermedia`)
- Registers `ITypermediaHandlerRegistry` and `ITypermediaHandlerRegistrar` as singletons

**Why it exists**: `ServerEndpointBuilder` is the single place where both paradigms' handler pipelines are wired into the DI container. It must know about both.

**To remove**: Extract Typermedia wiring into a pluggable component. For example, define an `IEndpointPipelineContributor` interface with a `Configure(IComponentRegistrar)` method that `ServerEndpointBuilder` calls without knowing what it registers. Typermedia would provide its own contributor.

### 2b. ~~MemoryInboxTransportServer.cs~~ — RESOLVED

**Previously**: `MemoryInboxTransportServer` resolved `TypermediaHandlerExecutor` and `InfrastructureQueryExecutor` and bound them into `InMemoryTypermediaNetwork` and `InMemoryInfrastructureNetwork` during start/stop.

**Now**: `MemoryInboxTransportServer` only binds the Tessaging inbox. Typermedia and infrastructure bindings are handled by `MemoryTypermediaTransportServer` and `MemoryInfrastructureTransportServer` respectively, started via `ISupplementalTransportServer` from `Endpoint`.

### 2c. ~~_TessageTypesInternal.cs~~ — RESOLVED

**Previously**: `RegisterInfrastructureQueryHandlers` resolved both `ITessageHandlerRegistry` and `ITypermediaHandlerRegistry` to build a combined `EndpointInformation` response.

**Now**: `_TessageTypesInternal` only returns Tessaging handled types. Typermedia types are advertised through a separate `TypermediaEndpointInformationQuery` (in `Compze.Typermedia.Client`), registered by `TypermediaInfrastructureQueryRegistration` from `ServerEndpointBuilder`.

### 2d. ~~TyperMediaApi/EventStore/~~ — RESOLVED

**Previously**: `_TeventStoreApi.Implementation..cs` in `Compze.Tessaging` registered event store operations as Typermedia handlers.

**Now**: Moved to `Compze.Tessaging.Teventive.TeventStore.Typermedia` bridge project, along with `TeventStoreRegistrationBuilder` and `TeventStoreTypermediaRegistrar`. The `TyperMediaApi/EventStore/` folder no longer exists in `Compze.Tessaging`.

---

## 3. Compze.Tessaging.Hosting.AspNetCore → Typermedia.Client + Typermedia.Hosting.AspNetCore

### 3a. AspNetCoreTransportRegistration.cs — Shared transport registration

```csharp
registrar.HttpClientFactoryCE()
         .HttpApiTransportClient()
         .HttpTypermediaTransport()              // ← Typermedia.Client
         .HttpInfrastructureQueryTransport()
         .Register(CompzeControllerActivator.RegisterWith,
                   AspNetInboxTransportServer.RegisterWith,
                   TypermediaController.RegisterWith,  // ← Typermedia.Hosting.AspNetCore
                   InfrastructureQueryController.RegisterWith,
                   TessagingController.RegisterWith);
```

A single extension method registers transport for ALL paradigms — Tessaging, Typermedia, and infrastructure queries.

**Why it exists**: Currently there's one `AspNetCoreTransport()` call that wires everything.

**To remove**: Split into paradigm-specific transport registrars. `AspNetCoreTransport()` would register only Tessaging + infrastructure. Typermedia would provide its own `.AspNetCoreTypermediaTransport()` extension. The testing/setup code would call both.

### 3b. AspNetInboxTransportServer.cs — Controller assembly discovery

```csharp
it.ApplicationParts.Add(new AssemblyPart(typeof(TypermediaController).Assembly)); // ← Typermedia.Hosting.AspNetCore
```

The ASP.NET server registers the `TypermediaController` assembly so MVC discovers the controller.

**Why it exists**: Same as above — one server hosts all controller types.

**To remove**: Use a plugin mechanism. Each paradigm could register its own assembly parts, or use ASP.NET's built-in assembly scanning instead of explicit `AssemblyPart` additions.

---

## 4. Compze.Tessaging.Hosting.Testing → Typermedia + Typermedia.Client

### 4a. TestingComponentRegistrar.ClientTransport.cs — Client transport wiring

Registers `.HttpTypermediaTransport()` or `.MemoryTypermediaTransport()` depending on current test transport configuration. Mirrors the production transport setup.

### 4b. TestingComponentRegistrar.Transport.cs — Full transport wiring

For memory: calls `.MemoryTypermediaTransport()` and `.MemoryInfrastructureQueryTransport()`. For ASP.NET: delegates to `AspNetCoreTransport()` (which internally registers Typermedia — see §3a).

### 4c. DiContainerExtensions.cs — DI setup for tests

Calls `.TypermediaHandlerRegistry()` when creating a test service locator.

### 4d. TestClient.cs — Typermedia-aware test client

The `TestClient` class is a test helper that creates an `ITypermediaRouter`, connects to a remote endpoint, and exposes an `IRemoteTypermediaNavigator`. This is a **consumer** of the Typermedia client — it exists so integration tests can issue Typermedia queries against test endpoints.

**Why all four exist**: Testing infrastructure mirrors production wiring. Tests need both Tessaging and Typermedia systems running to verify end-to-end behavior.

**To remove**: Same pluggable wiring pattern as production. Once `ServerEndpointBuilder` and `AspNetCoreTransportRegistrar` are paradigm-neutral, the testing registrars follow naturally. `TestClient` could move to `Compze.Typermedia.Hosting.Testing`.

---

## Patterns

Three structural patterns create all of the coupling:

1. **Dual-pipeline endpoint**: A single endpoint hosts both Tessaging and Typermedia handler registries, and the builder/DI setup must know about both. (§1, §2a) — **OPEN**

2. ~~**Unified transport**~~: — **RESOLVED** for memory transport via `ISupplementalTransportServer`. ASP.NET still open (§3a, §3b).

3. ~~**Aggregated discovery**~~: — **RESOLVED** via separate `TypermediaEndpointInformationQuery`.

The event store API bridge (§2d) — **RESOLVED** via `Compze.Tessaging.Teventive.TeventStore.Typermedia`.

## Dependency direction

All coupling flows **Tessaging → Typermedia** (Tessaging orchestrates Typermedia). There are no Typermedia → Tessaging references. This means Tessaging currently acts as the "host framework" that wires up Typermedia as a co-tenant.

The long-term goal would be to invert this: make the transport/hosting infrastructure paradigm-neutral, and have both Tessaging and Typermedia plug into it as equals — neither aware of the other.
