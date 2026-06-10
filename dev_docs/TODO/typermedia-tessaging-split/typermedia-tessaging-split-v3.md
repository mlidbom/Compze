# Typermedia–Tessaging Split v3

Supersedes v2 and `remaining-tessaging-to-typermedia-references.md` (whose coupling table is stale: `Compze.Tessaging`
and `Compze.Tessaging.Abstractions` no longer reference Typermedia — that coupling was since relocated into
`Compze.Hosting`).

## Verified current state (2026-06-10)

### Done — the paradigm code is disentangled

- **Typermedia → Tessaging: zero.** No Typermedia project references a Tessaging project; no Typermedia source file
  mentions `Compze.Tessaging`. Typermedia's only shared surface is the message-type hierarchy in `Compze.Abstractions`
  (the sanctioned common parent domain).
- **Tessaging core → Typermedia: zero.** `Compze.Tessaging` and `Compze.Tessaging.Abstractions` are clean.
- Pipelines fully split: transports, routing, handler execution, discovery, controllers. Memory transport (the worst
  coupler) deleted. Each paradigm runs its own Kestrel server (merged 2026-03-08).
- The TeventStore-over-Typermedia API lives in the explicit bridge project
  `Compze.Tessaging.Teventive.TeventStore.Typermedia` — a deliberate dependency on both. Correct; stays.

### Open — the hosting layer is one fused concept instead of three

The entanglement was quarantined, not dissolved. Two knots remain (two others — hosting abstractions in lying
Tessaging namespaces, and `Compze.Tessaging.Hosting.AspNetCore` composing Typermedia — were resolved by Phase A
and step B1, below):

1. **`Compze.Hosting` is a fused composition root.** `Endpoint` hard-codes both worlds (Tessaging inbox/outbox/
   scheduler/router AND `ITypermediaTransportServer`); `ServerEndpointBuilder.SetupContainer()` wires both pipelines in
   one method; it needs `InternalsVisibleTo` from `Compze.Tessaging` — the hosting SPI Tessaging should expose was
   never designed. Anyone using the endpoint model gets both frameworks.
2. **Testing is fused and one-sided.** `TestingEndpointHost` (in `Compze.Tessaging.Hosting.Testing`) wires both
   paradigms; `TestClient` — a Typermedia client helper — lives in the Tessaging testing project;
   `Compze.Typermedia.Hosting.Testing` and all three Typermedia spec projects are placeholders. Every real Typermedia
   spec runs through the fused host in `Compze.Tests.Integration/Tessaging/`. Typermedia standalone is compile-time
   truth only; neither paradigm can be hosted or tested alone. (`Compze.Tessaging.Hosting.Testing` referencing
   Typermedia is part of this knot: it is currently the de-facto composition layer for tests.)

Adjacent debt (not blocking, decide alongside): `Compze.Core` is not a shared core — its content is
DocumentDb + serialization + the Teventive/Tessaging domain, yet Typermedia transitively depends on it (via
`Compze.Internals.Transport`) — since Phase A only for infrastructure queries, no longer for the endpoint address
type.

## Target: three concepts, three homes

1. **Parent domain** (`Compze.Abstractions`): message-type hierarchy + endpoint identity/address/configuration +
   paradigm-neutral hosting contracts. No `Tessaging.*` namespaces for paradigm-neutral concepts.
2. **Two complete paradigm verticals**: each with its own wiring registrar, transport server, and testing host.
   Neither references the other.
3. **`Compze.Hosting` as an honest composition root** for apps wanting both paradigms: legitimately references both,
   composes them through a neutral plug-in seam, no `InternalsVisibleTo` back-doors.

## Phase A — give the shared hosting concepts their truthful home — DONE (2026-06-10)

All endpoint hosting contracts now live in `Compze.Abstractions/Hosting/Public/` → namespace
`Compze.Abstractions.Hosting.Public`. No new project references were needed: `Compze.Abstractions` already
referenced `Compze.DependencyInjection` (for `IRootResolver`/`IComponentRegistrar`) and `Compze.TypeIdentifiers`
(for `ITypeMapper`).

| Type | Was | Change made |
|---|---|---|
| `EndpointConfiguration`, `EndpointId` | `Compze.Abstractions.Tessaging.Hosting.Public` | namespace only |
| `EndpointAddress` | `EndPointAddress` in `Compze.Core.Tessaging.Transport.Internal` | moved + casing fixed; its type-id mapping moved from Core's to Abstractions' assembly type mapper (GUID unchanged) |
| `IEndpoint` | `Compze.Core.Tessaging.Hosting.Public` | moved + `Address` renamed `TessagingAddress` (it was the Tessaging inbox address; the name underspecified) |
| `IEndpointHost`, `ITestingEndpointHost` | `Compze.Core.Tessaging.Hosting.Public` | moved |
| `IEndpointBuilder` | `Compze.Tessaging.Abstractions.Tessaging.Hosting.Public` | moved + `RegisterTessagingHandlers` property dropped; both paradigms now register via identical extension properties in `Compze.Hosting` (`EndpointBuilderTessagingExtensions` / `EndpointBuilderTypermediaExtensions`) |
| `IEndpointRegistry` | `Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions`, **internal** | moved + made public — it is implemented by the host layer, which previously only compiled via the `InternalsVisibleTo` back-door |

Follow-on: `Compze.Core`'s reference to `Compze.Tessaging.Abstractions` (which existed for
`IEndpointHost` → `IEndpointBuilder`) was removed; only non-compiled `_docs` samples still mention it.
Verified: solution builds with 0 warnings; full suite green (3151 tests, 0 failed, 20 skipped).

Explicitly deferred to Phase B (seam design, needs a design conversation):
- Replacing `TessagingAddress`/`TypermediaAddress` with a per-registered-transport surface on `IEndpoint` —
  the neutral contract still naming both paradigms is a documented temporary state.
- The feature-module mechanism (`IEndpointPipelineContributor` or similar) that makes `ServerEndpointBuilder`
  paradigm-blind.
- Moving `ITessagesInFlightTracker` ("wait until at rest") out of the shared testing host into a
  Tessaging-contributed feature.
- `IInboxTransportServer` stays in `Compze.Core.Tessaging.Transport.Internal` for now — it is genuinely
  Tessaging-specific; its right home (Compze.Tessaging) is a Phase B/C concern.

## Phase B — make each paradigm a complete standalone vertical

### B1 — DONE (2026-06-10): `Compze.Tessaging.Hosting.AspNetCore` no longer knows Typermedia

`AspNetCoreTransport()` now registers only Tessaging + infrastructure queries. Typermedia's ASP.NET server pieces
(`TypermediaController`, `TypermediaTransportServer`) are registered by a new
`AspNetCoreTypermediaTransportServer()` extension in `Compze.Typermedia.Hosting.AspNetCore.Wiring`. The
composition (Tessaging transport + Typermedia client transport + Typermedia server) moved to the one caller,
`CurrentTestsTransport()` in `Compze.Tessaging.Hosting.Testing` — the de-facto composition layer until the
testing split. All three Typermedia project references were removed from `Compze.Tessaging.Hosting.AspNetCore`.

**Dependency state after B1: no Tessaging production project references Typermedia.** The only remaining
Tessaging → Typermedia references sit in the composition/testing layer (`Compze.Hosting`,
`Compze.Tessaging.Hosting.Testing`) and the deliberate TeventStore bridge.

Noted for the seam conversation: `AspNetCoreTransport()` now under-describes itself (it registers Tessaging +
infrastructure-query transport); candidate rename `AspNetCoreTessagingTransport()` — held back because where
infrastructure-query registration belongs is a seam-design question.

### The seam — design conversation needed before further code

The remaining steps hang on one decision: how a paradigm plugs into an endpoint. Options:

- **(a) Components collection + per-paradigm registration extensions (recommended).** An endpoint owns a
  collection of lifecycle components (generalize what `ISupplementalTransportServer` started: `ITransportServer`
  or `IEndpointComponent` with start-listening/start-sending/stop semantics). Each paradigm ships a registration
  extension (`AddTessaging()` / `AddTypermedia()`-style, in its own hosting package) that registers its pipeline
  plus its components into the container. `Endpoint` resolves the collection and runs lifecycle generically;
  `ServerEndpointBuilder` stops naming paradigms. Addresses move off `IEndpoint`: a test asks the specific
  component (or its feature's facade) for its address.
- **(b) Contributor interface.** `IEndpointPipelineContributor.Configure(IComponentRegistrar)` discovered/listed
  explicitly; otherwise like (a). Adds a named abstraction where (a) uses plain extensions — only worth it if
  contributors need ordering or metadata.
- **(c) Status quo shape, inverted ownership.** Keep a fused builder but move it (and `Endpoint`) so that only
  `Compze.Hosting` knows both paradigms, with Tessaging exposing a designed public hosting SPI (kills
  `InternalsVisibleTo`). Least change, but endpoints stay all-or-nothing: no Typermedia-only host.

Open sub-questions: where infrastructure-query transport registration belongs (it is endpoint plumbing, not a
paradigm); what the Tessaging hosting SPI looks like (today `Compze.Hosting` reaches into Tessaging internals
for `TommandScheduler`/`IInbox`/`IOutbox`/router/in-flight tracker); whether `IInboxTransportServer` moves from
`Compze.Core` into that SPI.

### Remaining (after the seam decision)
- Move `TestClient` + Typermedia test wiring into `Compze.Typermedia.Hosting.Testing`; build a minimal
  Typermedia-only testing host there. Give Tessaging its own testing host. (Not mechanical: `TestClient` leans on
  `TestEnv`/`TestingComponentRegistrar` machinery in the Tessaging testing project — part of the design
  conversation.)
- **Proof of done:** real specs in the three Typermedia placeholder spec projects, runnable from
  `Compze.Typermedia.slnx`; a new `Compze.Tessaging.slnx` that builds and tests with no Typermedia project on disk.

## Phase C — `Compze.Hosting` becomes an honest composition root

- Composes both paradigms through the Phase B seam; whatever it needs from Tessaging internals becomes a designed,
  public hosting SPI in Tessaging; remove `InternalsVisibleTo Compze.Hosting`.
- Combined integration tests and samples keep using it — that is its purpose.
- Revisit the name (`Compze.Hosting` vs something that states "composes Compze endpoints from both paradigms").
- Follow-up candidates: rename/split `Compze.Core` (content is Teventive/Tessaging domain, not a shared core);
  decide whether the "Tessage" parent-domain naming vs "Tessaging" framework naming overload should be teased apart.

## Status

- [x] Phase A — completed 2026-06-10 (commits 25797d10e, 1e1412b8a, 45a1ffbaf)
- [ ] Phase B — B1 done 2026-06-10; remaining steps need the seam-design conversation first
- [ ] Phase C
