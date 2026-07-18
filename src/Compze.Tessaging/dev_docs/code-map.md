# The Tessaging code map: where everything lives

This document orients a developer entering the Tessaging code: which projects exist, what each namespace
holds, which types are the load-bearing ones, and where the specifications live. The concepts themselves
are in [the Tessaging model](tessaging-model.md); this is the street map.

## The projects

| Project | What it is |
|---|---|
| `Compze.Tessaging` | The paradigm project: the engine, the endpoint types, both siblings' machinery, the named-pipe transport, the wire, the SQL-layer contracts. Almost everything in this document is here. |
| `Compze.Tessaging.AspNetCore` | The ASP.NET Core transport — its own project so the web stack is not dragged into every consumer. |
| `Compze.Tessaging.Sqlite` / `.MicrosoftSql` / `.MySql` / `.PostgreSql` | One project per SQL engine: the inbox/outbox/peer-registry/endpoint-catalog SQL layers, their schemas, and the `«Engine»DomainDatabase(...)` pairing declarations. |
| `Compze.Tessaging.Hosting.Testing` | The testing host (`TestingEndpointHost`) and `TypermediaTestClient` — per-tier test wiring over the production pipelines. |

Neighbors Tessaging is built on:

| Project | What Tessaging takes from it |
|---|---|
| `Compze.Abstractions` | The tessage-type hierarchy and the door contracts (`Tessaging/Public`: `_TessageTypes..Interfaces.cs`, the `TessageTypes` base-class families, `IUnitOfWorkTeventPublisher`/`IIndependentTeventPublisher`, `IUnitOfWorkTommandSender`/`IIndependentTommandSender`); the hosting contracts (`Hosting/Public`: `IEndpoint`, `IEndpointHost`, `EndpointId`, `EndpointAddress`, `EndpointConfiguration`, `IEndpointRegistry`/`IEndpointAddressAnnouncer`). |
| `Compze.Hosting` | `EndpointHost` (the optional convenience owning several endpoints' lifecycles in one process — endpoints are first-class) and `InterprocessEndpointRegistry` (`SameMachine/` — the zero-configuration same-machine discovery registry). |
| `Compze.Teventive` | The tevent wrapper (`PublisherIdentifyingTevent<TTevent>`, implementing `IPublisherTevent<TTevent>`) and the taggregate/tevent-store machinery that publishes through Tessaging's doors. |
| `Compze.DependencyInjection` | The container abstractions and the unit-of-work model (`IUnitOfWorkResolver`, `IScopeResolver`) every handler shape receives — see its `dev_docs/unit-of-work-model.md`. |

## Inside `Compze.Tessaging`, namespace by namespace

### The public composition surfaces

- **`Endpoints/`** — the composed shapes: `Endpoint` (the abstract lifecycle — six phase methods),
  `BestEffortEndpoint` and `ExactlyOnceEndpoint` (each with its `Compose(...)` composition root), their
  declaration surfaces `EndpointBuilder` / `BestEffortEndpointBuilder` / `ExactlyOnceEndpointBuilder`, and
  `EndpointAlreadyRunningInAnotherProcessException`.
- **`Engine/`** — the LocalTessagingEngine: `LocalTessagingEngineRegistrar` (the
  `Registrar.LocalTessagingEngine(engine => ...)` plain-container composition),
  `LocalTessagingEngineBuilder`, `TessageHandlerRegistrar` (the `ForTevent`/`ForTommand`/`ForTuery`
  declaration verbs), `TessageHandlerRoster` (the immutable map, and the advertisement's source),
  `TessageHandlerExecutor` (the one execution choreography), `TeventObservationRegistrar` and
  `TeventObservationDispatcher` (observation's declaration and off-thread dispatch), `NoHandlerException`.
- **`Typermedia/`** — the navigator doors and their implementations: `ILocalTypermediaNavigatorSession` /
  `IIndependentLocalTypermediaNavigator` (strictly-local), `IRemoteTypermediaNavigator` (an endpoint
  navigating its peers), and `NavigationSpecification` (the composable navigation description).
  - **`Typermedia/Client/`** — the pure client: `TypermediaClient` (+ builder), its explicit-connect
    `TypermediaClientRouter`, and the typermedia request handlers the server side serves.
  - **`Typermedia/Hosting/`** — `TypermediaHandlerExecutor`: wire-arriving typermedia execution through the
    engine's executor.
- **`Hosting/`** — the tommand-sender door implementations (`UnitOfWorkTommandSender`,
  `IndependentTommandSender`), registered by the exactly-once tier.

### The delivery machinery (`Implementation/`)

- **`Outbox/`** — the sending half of exactly-once: `Outbox` (fan-out membership from peer memory inside
  the publish's transaction, receiver binding at send, commit-hook enqueueing), its `TessageStorage`, and
  the `PeerLifecycleObserver` that reconciles undelivered rows against replaced advertisements.
- **`TessageHandling/`** — the receiving half: `Inbox` (receiver dedup, transactional handling, retry —
  `DefaultRetryPolicy`, the `HandlerExecutionEngine` coordinator), `Dispatching/`
  (`BestEffortTeventDirectDispatcher`, the no-handler exception family).
- **`BestEffortDelivery/`** — the guarantee-free tier's queues: `BestEffortTeventQueues` (per-peer
  queue-while-down, the 10,000 bound, tombstones), `BestEffortTeventDeliveryLeg`,
  `BestEffortTeventQueueOverflowException`.
- **`Peers/`** — peer memory and administration: `IPeerRegistry`, `DurablePeerRegistry` /
  `ProcessLifetimePeerRegistry`, `RememberedPeer`, `IPeerLifecycleObserver`,
  `IPeerDecommissionParticipant`, `IPeerAdministration` / `PeerAdministration`,
  `PeerDecommissionReport` — see [the peer model](peer-model.md).
- **`HandlerAvailability/`** — waiting sends: `IHandlerAvailability`, `HandlerAvailabilityPatience`.
- **`EndpointCatalog/`** — the process lease: `EndpointProcessLease`, `ProcessLeaseDuration` — see
  [the storage model](storage-model.md).
- **`Transport/Client/`** — the router and its connections: `TessagingRouter` (continuous reconciliation
  against the endpoint registry; routes for all four kinds), `TessagingConnection` with its
  `ExactlyOnceDeliveryStream` (storage-backed, recovery backlog, single-in-flight ordering) and
  `BestEffortDeliveryStream` (queue-draining).
- **`Abstractions/`** — the internal seams: `IOutbox`, `IBestEffortTeventDeliveryLeg` /
  `IExactlyOnceTeventDeliveryLeg` (the legs the publisher routes through), `ITessagesInFlightTracker`.
- The root also holds the door implementations for tevents (`UnitOfWorkTeventPublisher`,
  `IndependentTeventPublisher`) and the tracker (`TessagesInFlightTracker`).

### The wire (`Internals/Transport/`)

The transport-protocol strategy and everything protocol-independent above it: `IEndpointTransportClient` /
`IEndpointTransportServer`, the wire envelope (`TransportRequest`, `TransportRequestKind`), the
request-handler contribution seam (`ITransportRequestHandlerContribution`, `TransportRequestHandlerMap` —
how each capability's request kinds join the endpoint's one server), the endpoint-discovery query
round-trip (`EndpointDiscovery`, `EndpointDiscoveryQueryExecutor`, its fixed serializer), the HTTP client,
and **`NamedPipes/`** — the named-pipe transport (`NamedPipeAddress`, framing, client, server). The
ASP.NET Core twin of the named-pipe server lives in `Compze.Tessaging.AspNetCore`.

### The SQL-layer contracts (`Transport/SqlLayer/`)

`ITessagingSqlLayer` — the per-backend contracts for inbox, outbox, peer registry, and endpoint catalog —
plus `EndpointTableSet` (the per-endpoint prefixed table names and the endpoint-name rule) and the shared
schema-string classes. Implementations live in the four backend projects.

### Odds and ends

- **`Transport/`** (project root) — the per-tier transport request handlers
  (`BestEffortTessagingRequestHandlers`, `ExactlyOnceTessagingRequestHandlers`).
- **`SystemCE/ThreadingCE/`** — `TaskRunner` (tracked genuinely-async background work) and
  `IBackgroundExceptionReporter` (where observer failures, stolen leases, and other background failures
  surface; the testing host rethrows what it collects).

## The doors, in one table

What application code injects, by need:

| Need | Door | Requires |
|---|---|---|
| Publish a tevent inside my unit of work | `IUnitOfWorkTeventPublisher` | Ambient transaction (asserted) |
| Publish a tevent as its own unit of work | `IIndependentTeventPublisher` | No ambient transaction (asserted) |
| Send an exactly-once tommand inside my unit of work | `IUnitOfWorkTommandSender` | Exactly-once tier |
| Send an exactly-once tommand as its own unit of work | `IIndependentTommandSender` | Exactly-once tier |
| Execute strictly-local tueries/tommands in my session | `ILocalTypermediaNavigatorSession` | — |
| Execute strictly-local tueries/tommands independently | `IIndependentLocalTypermediaNavigator` | — |
| Navigate another endpoint's typermedia | `IRemoteTypermediaNavigator` | An endpoint (any tier) |
| Navigate typermedia from outside any endpoint | `TypermediaClient` | An explicitly known address |

## The specifications

- **`test/Compze.Tessaging.Specifications/`** — the project's own specs: `Storage/` (the table-set and
  name rules), `Typermedia/` (navigation, waiting sends, the pure client).
- **`test/Compze.Tests.Integration/`** — the black-box integration suite:
  - `Tessaging/Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler/` — the central
    conversation specs plus the peer-memory pins (registry, downtime delivery, receiver binding, shrink,
    decommission).
  - `Tessaging/` — readiness, the exactly-once bind wait, the shared-domain-database and process-lease
    specs.
  - `Hosting/` — queue-while-down, required peers, opt-down, host lifecycle.
  - `SameMachine/` — the real-OS-process specs, spawning
    `test/Compze.Tests.SameMachine.EndpointHostProcess` — which is also the reference production
    composition.
- **`test/Compze.Tests.Common/`** — shared fixtures, notably `EndpointHostTestBase` (the
  backend+remote endpoint pair most integration specs converse through).
- Tests run on the **pluggable-component matrix** (`[PCT]`): every spec runs per configured combination of
  persistence layer, DI container, serializer, and transport — see
  `.claude/rules/02-universal-local/040-build-and-test.md` for running them and
  `src/TestUsingPluggableComponentCombinations` for selecting combinations.

## The documentation

Current-state docs live beside this one: [tessaging-model.md](tessaging-model.md),
[tevent-delivery-model.md](tevent-delivery-model.md), [peer-model.md](peer-model.md),
[storage-model.md](storage-model.md); hosting and same-machine topology in
`src/Compze.Hosting/dev_docs/`. Work in flight is under [WIP/](WIP/tessaging-WIP.md); completed effort
records under `DONE/`. The public docs (the website) are in `_docs/`.
