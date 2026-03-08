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

## 3. ~~How does endpoint discovery become paradigm-neutral?~~ RESOLVED

**Decision: (b) Separate discovery queries.**

Each paradigm now advertises its own handled types independently via its own infrastructure query:

- `EndpointInformationQuery` (in `TessageTypesInternal`) returns only Tessaging handled types from `ITessageHandlerRegistry`
- `TypermediaEndpointInformationQuery` (new, in `Compze.Typermedia.Client`) returns only Typermedia handled types from `ITypermediaHandlerRegistry`
- `TypermediaRouter.ConnectAsync(address)` queries the Typermedia-specific query instead of the combined one
- The Typermedia discovery handler is registered by `TypermediaInfrastructureQueryRegistration` from `ServerEndpointBuilder`

**Result**: `_TessageTypesInternal` no longer references `ITypermediaHandlerRegistry`. The `TypeMapper` dependency was also removed (it was unused — passed as `_`).

---

## 4. ~~How do transport servers become paradigm-neutral?~~ RESOLVED (Memory transport)

**Decision: hybrid of (a) and (b) — supplemental transport servers with a shared interface.**

For the memory transport:

- `ISupplementalTransportServer` (new, in `Compze.Core`) defines `StartAsync(EndPointAddress)` / `StopAsync()` — a paradigm-neutral lifecycle interface
- `MemoryTypermediaTransportServer` (in `Compze.Typermedia.Client`) binds/unbinds `TypermediaHandlerExecutor` to `InMemoryTypermediaNetwork`
- `MemoryInfrastructureTransportServer` (in `Compze.Internals.Transport`) binds/unbinds `InfrastructureQueryExecutor` to `InMemoryInfrastructureNetwork`
- `MemoryInboxTransportServer` now only binds the Tessaging inbox — no Typermedia imports
- `Endpoint` resolves `IReadOnlyList<ISupplementalTransportServer>` and starts/stops them alongside the inbox, passing the inbox address
- The transport registrar (`TestingComponentRegistrar.Transport`) assembles the list

**Result**: `MemoryInboxTransportServer` no longer references `Compze.Typermedia.Client` or `Compze.Typermedia.Hosting`.

**Still open for ASP.NET**: `AspNetInboxTransportServer` still hard-codes all controller assemblies. The same `ISupplementalTransportServer` pattern could work — each paradigm registers its own ASP.NET controller parts — but this is deferred to the hosting design question (#2).

---

## 5. ~~Where does the event store API bridge live?~~ RESOLVED

**Decision: (a) Dedicated bridge project — `Compze.Tessaging.Teventive.TeventStore.Typermedia`.**

The bridge project was created with the following contents:

- `_TeventStoreApi..cs` + `_TeventStoreApi.Implementation..cs` — moved from `Compze.Tessaging/TyperMediaApi/EventStore/`
- `TeventStoreRegistrationBuilder` — the fluent builder that calls `TeventStoreApi.RegisterHandlersForTaggregate`. Moved from `Compze.Tessaging.Teventive.TeventStore`
- `TeventStoreTypermediaRegistrar.RegisterTeventStore(IEndpointBuilder)` — the extension method that returns `TeventStoreRegistrationBuilder`

**Dependency arrows**:
- Bridge → `Compze.Typermedia` (for handler registration)
- Bridge → `Compze.Tessaging.Teventive.TeventStore` (for `TeventStoreRegistrar.TeventStore()`)
- Bridge → `Compze.Tessaging.Abstractions` (for `IEndpointBuilder`)
- Bridge → `Compze.Core` (for teventive types)

No circular dependencies. `Compze.Tessaging.Teventive.TeventStore` is now Typermedia-free — it only exposes `IComponentRegistrar` extension methods for DI-level registration.

**Result**: The `TyperMediaApi/EventStore/` folder is gone from `Compze.Tessaging`. Callers keep the same `using Compze.Tessaging.Teventive.TeventStore.Wiring` namespace — they just add a project reference to the bridge.

---

## 6. What about test infrastructure?

`TestClient` creates an `ITypermediaRouter`, discovers endpoints, and exposes `IRemoteTypermediaNavigator`. The testing component registrars wire up both Tessaging and Typermedia transports (memory or HTTP). `DiContainerExtensions` registers Typermedia handler registries for tests.

**The question**: Should tests that exercise Typermedia use Typermedia-specific test infrastructure, or should there be one unified test harness?

This likely follows naturally from the answers to questions 1-4 — if the builder is paradigm-neutral, the test wiring follows. But it's worth calling out because `TestClient` is a direct consumer of `ITypermediaRouter` and `IRemoteTypermediaNavigator`, and it lives in a Tessaging project.

---

## Summary

| # | Question | Status | Key constraint |
|---|----------|--------|---------------|
| 1 | Unified or split endpoints? | **OPEN** | Determines whether we need a plugin mechanism or separate stacks |
| 2 | How does the builder become paradigm-neutral? | **OPEN** | Needed to break `Tessaging.Abstractions → Typermedia` |
| 3 | ~~How does discovery become paradigm-neutral?~~ | **RESOLVED** — separate discovery queries | `TessageTypesInternal` no longer references `ITypermediaHandlerRegistry` |
| 4 | ~~How do transport servers become paradigm-neutral?~~ | **RESOLVED** (memory) — supplemental transport servers | `MemoryInboxTransportServer` is now pure Tessaging; ASP.NET still open |
| 5 | ~~Where does the event store bridge live?~~ | **RESOLVED** — `Compze.Tessaging.Teventive.TeventStore.Typermedia` | `TyperMediaApi/EventStore/` gone from `Compze.Tessaging` |
| 6 | What about test infrastructure? | **OPEN** | Follows from 1-2, but `TestClient` needs a home |
