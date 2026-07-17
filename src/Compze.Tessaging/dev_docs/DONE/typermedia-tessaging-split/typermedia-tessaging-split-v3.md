# Typermedia–Tessaging Split v3

Supersedes v2 and `remaining-tessaging-to-typermedia-references.md` (whose coupling table is stale: `Compze.Tessaging`
and `Compze.Tessaging.Abstractions` no longer reference Typermedia — that coupling was since relocated into
`Compze.Hosting`).

> **Names in this document predate the in-process/distributed split (2026-07-12).** Since then:
> `TessagingEndpointFeature` → `DistributedTessagingEndpointFeature` (`AddTessaging()` → `AddDistributedTessaging()`),
> `TypermediaEndpointFeature` → `DistributedTypermediaEndpointFeature` (`AddTypermedia()` → `AddDistributedTypermedia()`),
> the components and testing host features gained the same `Distributed` prefix, and each style also has an in-process
> core feature (`TessageHandlingEndpointFeature` + `InProcessTessagingEndpointFeature`; `InProcessTypermediaEndpointFeature`
> in `Compze.Typermedia`). See `src/Compze.Hosting/dev_docs/hosting-model.md`.

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

### Done — the testing infrastructure is split (B3)

Neither testing package references the other paradigm; each paradigm is tested alone in its own spec projects;
the combined suite composes both explicitly.

Adjacent debt (not blocking, decide alongside): `Compze.Core` is not a shared core — its content is
DocumentDb + serialization + the Teventive/Tessaging domain, yet Typermedia transitively depends on it (via
`Compze.Internals.Transport`) for infrastructure queries.

## Target: three concepts, three homes

1. **Parent domain** (`Compze.Abstractions`): message-type hierarchy + endpoint identity/address/configuration +
   paradigm-neutral hosting contracts. No `Tessaging.*` namespaces for paradigm-neutral concepts. ✓ (Phase A)
2. **Two complete paradigm verticals**: each with its own wiring registrar, transport server, and testing host.
   Neither references the other. ✓ for production code (B1, B2); testing host pending (B3).
3. **A paradigm-blind hosting mechanism** (`Compze.Hosting`): endpoints, hosts, and a builder that paradigm
   pipelines plug into as features — referencing no paradigm. Composition happens at the application/test layer.
   ✓ (B2 — this landed stronger than the original "honest composition root" target: the mechanism needs neither
   paradigm, so a future Typermedia-only or Tessaging-only host reuses it as-is.)

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

### B2 — DONE (2026-06-10): the seam; `Compze.Hosting` is paradigm-blind

How a paradigm plugs into an endpoint, as built:

- **`IEndpointComponent`** (`Compze.Abstractions.Hosting.Public`) — the neutral lifecycle contract: listening
  starts before sending, host-wide; default no-ops for the sending phase (Typermedia only listens).
- **`IEndpointBuilder` gained the seam:** `GetOrAddFeature<TFeature>()` (idempotent feature creation),
  `AddComponent()` (lifecycle components), `OnContainerBuilt()` (post-build hooks, used for discovery handler
  registration). A feature wires one capability into the endpoint being built; the builder does not know which
  capabilities exist.
- **`TessagingEndpointFeature` / `TessagingEndpointComponent`** (`Compze.Tessaging.Hosting`): inbox, outbox,
  tommand scheduler, router connect-to-all, service bus session, Teventive type mappings, discovery handlers,
  in-flight-tracker default (NullOp unless the host pre-registered one), endpoint-registry fallback,
  background-exception throw at dispose. Added via `AddTessaging()` / touched via `RegisterTessagingHandlers`.
- **`TypermediaEndpointFeature` / `TypermediaEndpointComponent`** (`Compze.Typermedia.Client`): handler registry
  and executor, in-process navigator, transport-server lifecycle, discovery handler, type mappings. Added via
  `AddTypermedia()` / `RegisterTypermediaHandlers`.
- **`IEndpoint` lost the paradigm-named address properties.** `TessagingAddress` / `TypermediaAddress` are now
  extension properties contributed by each paradigm, reading its own component — call sites unchanged.
- **`Compze.Hosting` references no paradigm.** The `InternalsVisibleTo` back-door is gone — features live inside
  their own assemblies where internal access is natural. `TestingEndpointHost` absorbed `TestingEndpointHostBase`,
  owns the in-flight tracker and endpoint registry, and adds both features to every test endpoint (mirrors prior
  behavior). `AppSettingsJsonConfigurationParameterProvider` moved to `Compze.Hosting.Configuration` — it is
  neutral config infrastructure consumed by the SQL pools, not a Tessaging concept. The dead
  `DocumentDb → Tessaging.Abstractions` edge is removed; `Compze.Tessaging → Tessaging.Abstractions` is now an
  explicit reference instead of a fragile transitive chain.

Verified: build 0 warnings, full suite green (3151 tests, 0 failed), solution-structure validation clean.

**Production-code isolation is complete: outside the deliberate TeventStore bridge, no Tessaging project
references Typermedia or vice versa, and the hosting mechanism references neither.**

### B3 — DONE (2026-06-10): the testing-infrastructure split

The testing host repeats the B2 seam one level up — there is no combined host type anywhere:

- **`Compze.Hosting.Testing`** (new package): the paradigm-blind `TestingEndpointHost` plus the
  `ITestingEndpointHostFeature` seam (per-endpoint test wiring + dispose-time quiescence/background-exception
  participation). Also owns the neutral pluggable-component wiring: `TestingComponentRegistrar`
  (+DbPool/serializer extensions), `TestingContainerBuilderFactory.CreateTestingContainerBuilder`,
  `ContainerCloner`, the dummy config provider, `PluggableComponents`/`TestEnv` extensions, and the guarded
  shared transport infrastructure (`CurrentTestsInfrastructureTransportIfNotRegistered`: HTTP client factory +
  infrastructure-query client/server — both paradigm transports demand it; first one wins).
- **`Compze.Tessaging.Hosting.Testing`**: `TessagingTestingEndpointHostFeature` (owns the in-flight tracker and
  the host's `IEndpointRegistry`; hosts no longer implement `IEndpointRegistry` themselves),
  `CurrentTestsTessagingTransport`, `CurrentTestsConfiguredSqlLayer` (the Tessaging vertical's storage stack:
  interner + DocumentDb + tessaging + tevent store). References no Typermedia project.
- **`Compze.Typermedia.Hosting.Testing`**: `TypermediaTestingEndpointHostFeature`,
  `TypermediaTestClient` (renamed from `TestClient`; maps the client-side mirror of a Typermedia endpoint's
  server mappings — Abstractions + Internals.Transport + Typermedia.Client — no Core), and the Typermedia
  endpoint/client transport wiring.
- **Combined composition** is just `TestingEndpointHost.Create(new TessagingTestingEndpointHostFeature(), new
  TypermediaTestingEndpointHostFeature())` at call sites; the combined container helpers
  (`CombinedTestingContainers`: `SetupTestingContainer`, `CurrentTestsPluggableComponents`, the four-assembly
  `TypeIdentifierMapper`) live in `Compze.Tests.Common.Wiring`. Samples compose the same way the suite does.
- `AspNetCoreTransport()` → `AspNetCoreTessagingTransport()` (Tessaging-only; the shared plumbing it used to
  register is the guarded neutral registrar above — this answers B1's open seam question).
- `Compze.Tests.Infrastructure` is paradigm-neutral (`TestWiringHelper` moved to `Compze.Tests.Common`), so
  paradigm spec projects use `[PCT]`/`UniversalTestBase` without the other paradigm in their closure.

**Proofs of done, all green:** real specs in the three Typermedia spec projects —
`Given_an_endpoint_hosting_only_the_typermedia_paradigm` runs host + `TypermediaTestClient` over HTTP and
asserts no `Compze.Tessaging*` assembly is loaded; `NavigationSpecification_specification`;
`TypermediaRouter_specification`. `Compze.Tessaging.slnx` exists with no Typermedia project. Tessaging-only
tests in the suite now create Tessaging-only hosts.

## Follow-ups (taste and naming, after the structure settles)

- `DistributedTypermediaEndpointFeature` lives in `Compze.Typermedia.Client` because that package already mixes client
  and endpoint concerns (discovery registration lives there). The in-process core now lives in `Compze.Typermedia`;
  still open: a dedicated endpoint package for the distributed feature, or renaming Client.
- `IEndpointRegistry` is namespaced as neutral but is consumed only by Tessaging routing; decide its true owner.
- `ServerEndpointBuilder` — "Server" claims a distinction (vs client endpoints?) that no longer exists.
- Rename/split `Compze.Core` (content is Teventive/Tessaging domain plus DocumentDb glue, not a shared core);
  `IInboxTransportServer` still sits in `Compze.Core.Tessaging.Transport.Internal` and belongs in Tessaging.
- Decide whether the "Tessage" parent-domain naming vs "Tessaging" framework naming overload should be teased
  apart.
- Production `EndpointHost` endpoints fall back to `AppConfigEndpointRegistry`, whose address lookup is a
  `NotSupportedException` stub — production multi-endpoint hosting has never actually worked; pre-existing,
  unchanged by this work.
- `CurrentTestsDbPoolIfNotCloneContainer` also registers serializers — a hidden coupling its name doesn't admit;
  either rename or split the serializer registration out of it.
- `PluggableComponents` is a `record struct`, against the no-records convention (pre-existing shape, moved as-is).
- `Compze.Hosting.Testing` hard-codes the AspNet/HTTP transport infrastructure even though a `Transport` test
  dimension exists; fine while AspNetCore is the only transport, revisit if another appears.
- Serializer specs (`SerializerTest`) now declare their own type mappings (Abstractions + Core) instead of the
  framework-wide helper; review whether that is the mapping set they should mean.

## Status

- [x] Phase A — completed 2026-06-10 (commits 25797d10e, 1e1412b8a, 45a1ffbaf)
- [x] Phase B1 (ASP.NET transport split) — completed 2026-06-10 (commit cd729f19a)
- [x] Phase B2 (the seam; paradigm-blind hosting) — completed 2026-06-10 (commit 9a6a82746)
- [x] Phase B3 (testing-infrastructure split) — completed 2026-06-10 (commit 4ef531656 + spec/doc commits)
- [ ] Follow-ups (taste and naming)
