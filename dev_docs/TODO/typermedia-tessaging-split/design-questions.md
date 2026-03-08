# Design Questions — Typermedia/Tessaging Separation

These are the questions that must be answered before the real separation work can proceed. Without answers, the only thing that can be done is cosmetic file moves — which don't change the dependency graph.

Ordered by importance: each question's answer constrains the answers to subsequent questions.

---

## 1. Is an endpoint one thing or two?

Today a single `EndpointId` + `EndPointAddress` hosts both a Tessaging pipeline (inbox, outbox, event store) and a Typermedia pipeline (handler executor, handler registry). `ServerEndpointBuilder` wires both into one DI container. `IEndpointBuilder` exposes both `RegisterTessagingHandlers` and `RegisterTypermediaHandlers`.

**The question**: Should an endpoint remain a single concept that can host multiple paradigm pipelines? Or should a Typermedia endpoint and a Tessaging endpoint be fully independent — separate `EndpointId`, separate address, separate DI container, separate lifecycle?

**Why it matters**: This is the foundation. If endpoints stay unified, we need a plugin mechanism inside the endpoint. If they split, each paradigm owns its entire stack and the shared infrastructure shrinks to just "how do you run multiple endpoints in one process."

**Trade-off**: Unified endpoints are simpler operationally (one address, one container, one startup). Split endpoints are simpler architecturally (zero shared wiring) but mean two containers, two addresses, two discovery entries per logical service.

---

## 2. How does the endpoint builder become paradigm-neutral?

`IEndpointBuilder` currently hard-codes both registrar properties. `ServerEndpointBuilder` hard-codes creation of both handler registries, calls `TypermediaHandlerExecutor.RegisterWith()`, registers `InProcessTypermediaNavigator`, etc.

**The question**: What replaces the hard-coded wiring? Options include:

- **(a) Callback/contributor pattern**: `IEndpointBuilder` exposes only `Container` and `Configuration`. Each paradigm provides an `IEndpointContributor` (or similar) that receives the builder and registers its own infrastructure. User code calls `builder.UseTypermedia(setup => ...)` and `builder.UseTessaging(setup => ...)` as extension methods.

- **(b) Separate builders entirely**: `TypermediaEndpointBuilder` and `TessagingEndpointBuilder` are independent classes. No shared `IEndpointBuilder`. User code creates whichever they need (or both, if the answer to question 1 is "unified").

- **(c) Something else?**

**Why it matters**: This determines whether `Compze.Tessaging` can stop referencing `Compze.Typermedia` entirely. Until the builder is paradigm-neutral, every endpoint hard-wires both systems.

---

## 3. How does endpoint discovery become paradigm-neutral?

`TessageTypesInternal.RegisterInfrastructureQueryHandlers` resolves both `ITessageHandlerRegistry` and `ITypermediaHandlerRegistry` to build the `EndpointInformation` response — a single flat list of all handled type IDs. Routers on the client side then split this list by checking `IAtMostOnceTypermediaTommand` vs `IExactlyOnceTommand` etc.

**The question**: Should each paradigm advertise its own handled types independently? Options:

- **(a) Pluggable type providers**: Define `IHandledTypeProvider` with a method like `IEnumerable<TypeId> GetHandledTypeIds()`. Each paradigm registers its own. The infrastructure query handler resolves all of them and concatenates. Neither registry is referenced by name.

- **(b) Separate discovery queries**: Typermedia has its own `TypermediaEndpointInformationQuery` and Tessaging has its own. Each router queries only its own.

- **(c) Keep aggregated discovery but invert the dependency**: The infrastructure query handler resolves types via an interface that both registries implement, rather than knowing both concrete types.

**Why it matters**: This is the coupling point that forces `Compze.Tessaging` (where `TessageTypesInternal` lives) to reference `Compze.Typermedia` (for `ITypermediaHandlerRegistry`).

---

## 4. How do transport servers become paradigm-neutral?

`MemoryInboxTransportServer` currently resolves `TypermediaHandlerExecutor` and `InfrastructureQueryExecutor` from the DI container and binds them into their respective in-memory networks during `StartAsync`. On the ASP.NET side, `AspNetInboxTransportServer` adds controller assemblies for all three paradigms. One server does everything.

**The question**: How should transport server startup work?

- **(a) Pluggable bindings**: The transport server accepts a list of `ITransportBinding` objects. Each paradigm registers its own binding (e.g., `TypermediaMemoryBinding` that knows how to bind/unbind `TypermediaHandlerExecutor` to `InMemoryTypermediaNetwork`). The server calls `binding.Bind(address)` / `binding.Unbind(address)` without knowing what it's binding.

- **(b) Separate servers per paradigm**: `MemoryTypermediaTransportServer`, `MemoryTessagingTransportServer`, `MemoryInfrastructureTransportServer` — each owns its own binding. The endpoint starts all of them.

- **(c) One ASP.NET host, pluggable controller discovery**: For HTTP, keep a single `WebApplication` but have each paradigm register its own assembly parts via a callback rather than the transport server hard-coding them.

**Why it matters**: This is the coupling that forces `Compze.Tessaging` to reference `Compze.Typermedia.Client` and `Compze.Typermedia.Hosting`.

---

## 5. Where does the event store API bridge live?

`Compze.Tessaging/TyperMediaApi/EventStore/_TeventStoreApi.Implementation..cs` registers event store operations (get aggregate, save aggregate, get history, etc.) as Typermedia handlers. `TeventStoreRegistrar` (in `Compze.Tessaging.Teventive.TeventStore`) calls this code and passes the Typermedia handler registrar.

This isn't accidental entanglement — it's genuine cross-paradigm bridging. The event store is a Tessaging/Teventive concept. Exposing its operations as Typermedia tueries/tommands is an explicit design choice.

**The question**: Where should this bridge code live?

- **(a) Dedicated bridge project**: e.g. `Compze.Tessaging.Teventive.TeventStore.Typermedia` — references both sides, belongs to neither.
- **(b) In the Typermedia project tree**: The Typermedia handlers for event store operations live in a Typermedia project that references the event store.
- **(c) Keep it in Tessaging**: Accept that this specific code is cross-paradigm and keep it where it is, but make it the *only* Typermedia reference in the Tessaging tree.

**Why it matters**: This is the one coupling that might be *correct* — it exists because we genuinely want event store operations available over Typermedia. But it should be a *choice*, not a *structural dependency*.

---

## 6. What about test infrastructure?

`TestClient` creates an `ITypermediaRouter`, discovers endpoints, and exposes `IRemoteTypermediaNavigator`. The testing component registrars wire up both Tessaging and Typermedia transports (memory or HTTP). `DiContainerExtensions` registers Typermedia handler registries for tests.

**The question**: Should tests that exercise Typermedia use Typermedia-specific test infrastructure, or should there be one unified test harness?

This likely follows naturally from the answers to questions 1-4 — if the builder is paradigm-neutral, the test wiring follows. But it's worth calling out because `TestClient` is a direct consumer of `ITypermediaRouter` and `IRemoteTypermediaNavigator`, and it lives in a Tessaging project.

---

## Summary

| # | Question | Key constraint |
|---|----------|---------------|
| 1 | Unified or split endpoints? | Determines whether we need a plugin mechanism or separate stacks |
| 2 | How does the builder become paradigm-neutral? | Needed to break `Tessaging.Abstractions → Typermedia` |
| 3 | How does discovery become paradigm-neutral? | Needed to break `TessageTypesInternal → ITypermediaHandlerRegistry` |
| 4 | How do transport servers become paradigm-neutral? | Needed to break `Tessaging → Typermedia.Client` + `Typermedia.Hosting` |
| 5 | Where does the event store bridge live? | Cross-paradigm code needs a home |
| 6 | What about test infrastructure? | Follows from 1-4, but `TestClient` needs a home |
