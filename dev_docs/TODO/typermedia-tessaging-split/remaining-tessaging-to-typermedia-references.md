# Remaining Tessaging → Typermedia References

After extracting `Compze.Typermedia.Client` and `Compze.Typermedia.Hosting.AspNetCore`, four Tessaging projects still reference Typermedia projects. This document catalogs every remaining reference, why it exists, and what it would take to remove it.

## Summary

| Tessaging project | References | Files | Root cause |
|---|---|---|---|
| **Tessaging.Abstractions** | Typermedia | 1 | `IEndpointBuilder` exposes `TypermediaHandlerRegistrarWithDependencyInjectionSupport` |
| **Tessaging** | Typermedia, Typermedia.Client, Typermedia.Hosting | 4 | Dual-pipeline setup, transport binding, discovery, event store API bridge |
| **Tessaging.Hosting.AspNetCore** | Typermedia.Client, Typermedia.Hosting.AspNetCore | 2 | Shared ASP.NET host registers both controllers and transports |
| **Tessaging.Hosting.Testing** | Typermedia, Typermedia.Client | 4 | Test wiring mirrors production; `TestClient` is Typermedia-aware |

`Compze.Tessaging.Teventive.TeventStore` has **no** Typermedia references — it is already fully decoupled.

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

### 2b. MemoryInboxTransportServer.cs — Transport binding

**References**: `Compze.Typermedia.Client` (for `InMemoryTypermediaNetwork`), `Compze.Typermedia.Hosting` (for `TypermediaHandlerExecutor`)

When the memory transport server starts, it binds executors into three separate in-memory networks:
```csharp
InMemoryTransportNetwork.BindServerToAddress(address, this);           // Tessaging
InMemoryTypermediaNetwork.BindExecutor(address, _typermediaExecutor);  // Typermedia ← here
InMemoryInfrastructureNetwork.BindExecutor(address, _infrastructureQueryExecutor);
```

On stop, it unbinds all three.

**Why it exists**: One transport server hosts all three message paradigms. It must bind/unbind all of them.

**To remove**: Make binding pluggable. The transport server could accept a list of `ITransportNetworkBinding` objects that each know how to bind/unbind their own network. Typermedia would register its own binding. Alternatively, have each paradigm register its own `IInboxTransportServer` implementation.

### 2c. _TessageTypesInternal.cs — Endpoint discovery

**References**: `Compze.Typermedia.HandlerRegistration` (for `ITypermediaHandlerRegistry`)

The `RegisterInfrastructureQueryHandlers` method builds the `EndpointInformation` response by concatenating handled types from BOTH registries:
```csharp
new EndpointInformation(
    tessagingRegistry.HandledRemoteTessageTypeIds()
        .Concat(typermediaRegistry.HandledRemoteTypermediaTypeIds()),  // ← Typermedia
    configuration);
```

**Why it exists**: Routers need to know ALL message types an endpoint handles — both Tessaging and Typermedia — to route correctly. The discovery query must aggregate types from both registries.

**To remove**: Make type advertisement pluggable. Define an `IHandledTypeProvider` interface that each paradigm implements. The infrastructure query handler would resolve all `IHandledTypeProvider` instances and concatenate their results. Neither registry would need to be known by name.

### 2d. TyperMediaApi/EventStore/_TeventStoreApi.Implementation..cs — Event store API bridge

**References**: `Compze.Typermedia`, `Compze.Typermedia.HandlerRegistration`

This file registers event store operations (get aggregate, get history, save, etc.) as Typermedia handlers:
```csharp
public static void RegisterHandlersForTaggregate<TTaggregate, TTevent>(
    TypermediaHandlerRegistrarWithDependencyInjectionSupport typermediaRegistrar) // ← Typermedia
{
    TommandApi.SaveTaggregate<TTaggregate>.RegisterHandler(typermediaRegistrar);
    TueryApi.TaggregateLink<TTaggregate>.RegisterHandler(typermediaRegistrar);
    // ... etc
}
```

**Why it exists**: This is genuine cross-concern code — it bridges the event store (a Tessaging-domain concept) into the Typermedia API layer so aggregates can be queried/saved via Typermedia.

**To remove**: Move the `TyperMediaApi/EventStore/` folder out of `Compze.Tessaging` into a bridge project (e.g., `Compze.Tessaging.Typermedia` or `Compze.Tessaging.Teventive.TeventStore.Typermedia`). This code explicitly bridges two paradigms, so it belongs in neither — it should live in a project that references both.

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

1. **Dual-pipeline endpoint**: A single endpoint hosts both Tessaging and Typermedia handler registries, and the builder/DI setup must know about both. (§1, §2a)

2. **Unified transport**: One transport server (Memory or ASP.NET) carries messages for all paradigms. Starting, stopping, binding, and registering controllers is done in one place. (§2b, §3a, §3b)

3. **Aggregated discovery**: Infrastructure queries report handled types from all paradigms so routers can find endpoints. (§2c)

The event store API bridge (§2d) is a fourth, distinct pattern — genuine cross-paradigm business logic that bridges two domains.

## Dependency direction

All coupling flows **Tessaging → Typermedia** (Tessaging orchestrates Typermedia). There are no Typermedia → Tessaging references. This means Tessaging currently acts as the "host framework" that wires up Typermedia as a co-tenant.

The long-term goal would be to invert this: make the transport/hosting infrastructure paradigm-neutral, and have both Tessaging and Typermedia plug into it as equals — neither aware of the other.
