# Design Questions — Typermedia/Tessaging Separation

Questions that needed answers before the separation work could proceed. Most are now decided.

---

## 1. ~~Is an endpoint one thing or two?~~ DECIDED

**Decision: One endpoint, shared container, separate transports.**

Most services will use both Typermedia and Tessaging. Sharing a DI container is practical — both paradigms access the same domain services. But the endpoint is neither Tessaging nor Typermedia — it's a host for both.

Transports are fully separate: each paradigm gets its own server, its own port, its own lifecycle. No shared address.

---

## 2. ~~How does the endpoint builder become paradigm-neutral?~~ Phase 1 DONE

**Phase 1: DONE.** `Compze.Hosting` created. Moved `ServerEndpointBuilder`, `Endpoint`, `EndpointHost`, and `TestingEndpointHostBase` there.

`IEndpointBuilder` stays in `Compze.Tessaging.Abstractions` (needed by `Compze.Core`'s `IEndpointHost` and `DocumentDbRegistrar`). The `RegisterTypermediaHandlers` property was removed from the interface — it's now an extension method in `Compze.Hosting.EndpointBuilderTypermediaExtensions` that casts to `ServerEndpointBuilder`. Callers add `using Compze.Hosting;` and call `builder.RegisterTypermediaHandlers()` (method, not property).

`Compze.Tessaging` has zero Typermedia references — all three flex references (`Compze.Typermedia`, `.Client`, `.Hosting`) removed. `Compze.Tessaging.Abstractions` also Typermedia-free. `Compze.DocumentDb` got an explicit `Compze.Typermedia` reference (was previously transitive through `Compze.Tessaging.Abstractions`).

**Phase 2 (future): Make `Compze.Hosting` optional.** A user who only wants Typermedia shouldn't need Tessaging as a transitive dependency. This means splitting the builder into paradigm-specific composable pieces. The code is now in the right place to do it.

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

## 4. ~~How do transport servers become paradigm-neutral?~~ DECIDED

**Decision: Fully separate servers, separate ports.**

Each paradigm owns its entire transport stack — its own server, its own port/address, its own lifecycle. No shared address.

For ASP.NET, Typermedia starts its own `WebApplication` on its own port instead of sharing the Tessaging `WebApplication`.

**Blocker removed**: The `ISupplementalTransportServer` pattern was removed when memory transport was deleted (Phase 4d). Memory transport was the sole reason that pattern existed — ASP.NET never needed it. With memory transport gone, `Endpoint` no longer manages supplemental servers.

**Remaining work**: `AspNetInboxTransportServer` still hosts all three controllers (`TypermediaController`, `TessagingController`, `InfrastructureQueryController`) in a single `WebApplication` on a single port. Typermedia needs its own Kestrel instance.

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

`TestClient` creates an `ITypermediaRouter`, discovers endpoints, and exposes `IRemoteTypermediaNavigator`. The testing component registrars wire up both Tessaging and Typermedia transports. `DiContainerExtensions` registers Typermedia handler registries for tests.

Follows the same pattern as Q2: the test infrastructure that bridges both paradigms moves to `Compze.Hosting.Testing` (or similar). `TestClient` lives there because it's a consumer of both paradigms. Tessaging-only and Typermedia-only test infrastructure stays in their respective projects.

---

## Summary

| # | Question | Status |
|---|----------|--------|
| 1 | Unified or split endpoints? | **DECIDED** — one endpoint, shared container, separate transports |
| 2 | ~~Builder paradigm-neutral~~ | **Phase 1 DONE** — `Compze.Hosting` created; Phase 2 (optional hosting) future |
| 3 | ~~Discovery~~ | **DONE** — separate discovery queries |
| 4 | ~~Transport servers~~ | **DECIDED** — `ISupplementalTransportServer` removed; separate Kestrel instances not yet implemented |
| 5 | ~~Event store bridge~~ | **DONE** — `Compze.Tessaging.Teventive.TeventStore.Typermedia` |
| 6 | Test infrastructure? | **DECIDED** — follows Q2: bridge code moves to `Compze.Hosting.Testing` |

## Next steps

1. ~~Create `Compze.Hosting`~~ — **DONE**
2. ~~Remove `ISupplementalTransportServer` pattern~~ — **DONE** (removed with memory transport, Phase 4d)
3. Separate ASP.NET transport servers fully (Typermedia gets own `WebApplication` on own port)
4. Move test bridge infrastructure to `Compze.Hosting.Testing`

`Compze.Tessaging` and `Compze.Typermedia` now have zero references to each other. All cross-paradigm code lives in projects whose names say "I combine things": `Compze.Hosting`, `Compze.Hosting.Testing`, `Compze.Tessaging.Teventive.TeventStore.Typermedia`.
