# The Compze hosting model

This document takes a developer who is new to Compze from zero to understanding how hosting works — what is
actually running when a Compze application runs, how the pieces relate, and how the same machinery hosts both
production applications and the test suite.

## The big picture

### What a running Compze application is

A Compze application is built as one or more **endpoints**. An endpoint is the unit of isolation and
deployment — think of it as a service: it has its own dependency injection container, its own connection
string and storage, its own registered message handlers, and its own transport listening on the network.
Endpoints do not call each other's code; they converse by sending messages. Several endpoints can run in one
process, or each in its own — the model is the same.

**Hosting** is everything between "here are my handlers and domain logic" and "a running system serving
them": building each endpoint's container, wiring its message pipelines, starting its transport in a safe
order, letting endpoints discover each other, and tearing it all down cleanly. A **host** is the object that
owns a set of endpoints and drives that shared lifecycle.

### Two ways to converse: Tessaging's two siblings

Compze gives application code two **communication styles** — two different ways for endpoints and clients to
converse, both built on the one Tessaging paradigm: conversation through *tessages*, messages routed by their
.NET type. (Compze names its message concepts with a leading *t*: a *tessage* is a message, a *tevent* an
event, a *tommand* a command, a *tuery* a query.)

- **TessageBus** is the asynchronous style — the message bus re-founded on tessages. Handlers publish tevents
  and send tommands; subscribers receive every tevent whose type they are compatible with; an inbox/outbox
  provides transactional, exactly-once delivery. TessageBus is for *telling the world what happened* and
  trusting it arrives.
- **Typermedia** is the request/response style — hypermedia principles ("navigate an API by following
  links") extended with .NET types. A client navigates an endpoint's API by executing tueries and tommands
  and following the typed results to the next step. Typermedia is for *asking questions and getting answers,
  now*.

A tessage's type is its whole contract: its kind, its delivery guarantee, its transactionality, its
remotability, and its synchrony are all declared by the interfaces the type extends, and the machinery that
carries a tessage is chosen by reading its type.

### The three composed shapes

There are exactly three things an application composes, all in `Compze.Tessaging`:

1. **The LocalTessagingEngine** — the tessage-conversing heart of one container, for the application that
   converses only with itself (see "The engine" below). Not an endpoint: no identity, no address, no wire.
2. **An endpoint** — an engine given identity and a wire. There are exactly two endpoint types, differing
   only in what crossing the endpoint boundary guarantees: `BestEffortEndpoint` (no database; per-peer
   best-effort tevent queues) and `ExactlyOnceEndpoint` (everything the best-effort endpoint has, plus the
   durable vertical: inbox, outbox, durable peer memory, and the tommand-sending doors). **Both serve all
   four tessage kinds, unconditionally.**
3. **The pure client** — `TypermediaClient`: a navigator and a transport client with no server, for an
   external application navigating endpoints' typermedia at explicitly known addresses.

An endpoint is a plain composition root: composition choices are parameters, not plugins. Its declaration
surface takes the transport protocol, the one serializer, the database (exactly-once tier), the topology
declarations, and the same engine declaration block every composition speaks — and a missing declaration
fails loud at composition, naming what is missing.

## The shape of the code

| Layer | Home | Knows about the styles? |
|---|---|---|
| **Contracts** | `Compze.Abstractions` → `Compze.Abstractions.Hosting.Public` | No — `IEndpointHost`, `IEndpoint`, the endpoint value types (`EndpointId`, `EndpointAddress`, `EndpointConfiguration`), `IEndpointRegistry`/`IEndpointAddressAnnouncer` |
| **The host mechanism** | `Compze.Hosting` | No — `EndpointHost` drives lifecycle phases host-wide over `IEndpoint`s it never looks inside |
| **The composed shapes** | `Compze.Tessaging` (`Compze.Tessaging.Endpoints`, `Compze.Tessaging.Engine`, the Typermedia namespaces) | Yes — the endpoint types compose both siblings, always |

The host mechanism implements the contracts without referencing what an endpoint speaks: it hands each
registration a fresh container builder from its factory and receives a composed `IEndpoint` back
(`RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, ...))`). Composition — what each
endpoint actually is — happens at the outermost layer: the application or the test.

## Endpoints and hosts

Declaring an endpoint looks like this, and is the same regardless of host:

```csharp
var host = EndpointHost.Production.Create(CreateContainerBuilder);
var endpoint = host.RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(
   container,
   "AccountManagement",
   new EndpointId(Guid.Parse("...")),
   endpoint =>
   {
      endpoint.AspNetCoreEndpointTransport();
      endpoint.NewtonsoftSerializer();
      endpoint.SqliteEndpointDatabase("AccountManagement");
      endpoint.ParticipateIn(registry);

      endpoint.MapTypes(mapper => mapper.RegisterMyDomainTypeMappings());
      endpoint.RegisterTessageHandlers(handle => handle
         .ForTevent(async (IAccountTevent tevent, IUnitOfWorkResolver unitOfWork) => ...)
         .ForTuery((AccountTuery tuery) => ...));
   }));
host.Start();
```

`Compose`'s callback receives the endpoint's declaration surface (`ExactlyOnceEndpointBuilder` /
`BestEffortEndpointBuilder`) — everything the endpoint will be is declared before its container is built,
and the builder exists only inside the callback: the callback's end is the declaration's end, the build
closes the roster, and any attempt to declare afterward explodes. The named declarations come from the
implementation packages, each filling the one parameter it names: the transport protocol
(`NamedPipeEndpointTransport()` / `AspNetCoreEndpointTransport()`), the endpoint's one serializer
(`NewtonsoftSerializer()`), and — on the exactly-once tier — the database, whose named declaration registers
the whole engine pairing (`SqliteEndpointDatabase(...)` and kin: the connection pool, the type-id interner,
and Tessaging's sql layers for that engine). Store integrations (a tevent store's `HandleTaggregate`, a
document db's `HandleDocumentType`) plug into this same surface — handler contributors like any other.

## The lifecycle phases

Each endpoint drives its own lifecycle in readable methods — listen → announce → send on the way up,
retract → stop sending → stop listening on the way down — and the host runs each phase **host-wide**: when a
host starts, every endpoint finishes `StartListeningAsync` before any endpoint runs `AnnounceAddressAsync`,
and every announcement lands before any endpoint starts `StartSendingAsync`. Nothing can send to an endpoint
that is not yet ready to receive, an announced address is always one whose whole endpoint is already
listening, and a router taking its first look at an endpoint registry when sending starts sees every
endpoint the host announced, never a partial membership decided by start-up racing. Stopping runs in
reverse: addresses are retracted before any sending stops, and sending stops before listening.

What each phase does inside an endpoint: peer memory loads, the durable vertical initializes (exactly-once
tier), and the one transport server starts in the listening phase; the address is announced to every
declared `IEndpointAddressAnnouncer` in the announcing phase; and in the sending phase the router converges
on the registry's membership — minus the endpoint's own announced address: routes lead only to *other*
endpoints — and the connections' delivery streams start, loading their recovery backlogs from the storage
the listening phase initialized.

An endpoint runs **one transport server** with one address, serving every remotable capability it speaks —
which is what lets an endpoint registry map an `EndpointId` to a single address, and it is where the
endpoint answers the one discovery question (its name, its `EndpointId`, and its advertisement — the
roster's one projection, covering every tessage kind). The endpoint's `Address` is null until it is
listening.

## The engine — when there is nothing to host

Everything above assumes an endpoint that converses with other endpoints. But the paradigm's purely
synchronous core is valuable entirely on its own, inside a single process: publishing tevents that
restructure the internal flow of an application, and executing strictly local tueries and tommands through
the `ILocalTypermediaNavigatorSession`. Everything in that core runs synchronously, on the calling thread,
within the caller's transaction — so there are no transports to start, no discovery, no background work, and
therefore *nothing to host*. In-process Compze is container wiring, not hosting.

That core is the **LocalTessagingEngine** — the tessage-conversing heart of one container — and a plain
container composes it from one declaration block, built on the same wiring the endpoint types compose, so
there is exactly one definition of what the engine is:

```csharp
var builder = /* any Compze container builder */;
builder.Registrar.LocalTessagingEngine(engine => engine
   .RegisterTessageHandlers(handle => handle
      .ForTevent((ISomethingHappenedTevent tevent) => ...)
      .ForTuery((SomeTuery tuery) => ...)));
var container = builder.Build();
```

The declaration block is the one and only way anything gets into the engine: the roster closes when the
engine is built, and the same declaration block is the surface everywhere — a plain container, a best-effort
endpoint, an exactly-once endpoint — so an application's handler declarations run unchanged under any
composition.

Three things to know:

- **"In-process" describes communication, not storage.** A real, persistent tevent store composes with
  the engine unchanged; a taggregate's committed tevents are simply delivered only to this
  process's handlers.
- **No type-id ceremony.** In-process dispatch routes by `System.Type`; type-id mappings exist for the wire
  and for persistent-store serialization. The plain-container composition supplies a default type mapper
  when none is registered; an application whose tevent store needs domain mappings registers its own first.
- **Wanting guaranteed tommand delivery does not make an endpoint "in-process".** A Tessaging tommand's type
  declares its cross-boundary delivery contract — exactly-once, transactional, asynchronous. Within the
  boundary the consistency law applies instead: a tommand whose handler is in the endpoint's own roster
  executes inline, in the sender's execution — exactly-once by construction, one transaction, no delivery
  machinery involved. An application that wants the guarantees within a single process is a distributed
  endpoint that happens to be alone in its host.

## Production hosting

`EndpointHost.Production.Create(containerFactory)` — the factory produces a fresh container builder per
endpoint. How endpoints find each other is a topology declaration in each endpoint's composition:
`DiscoverEndpointsThrough(registry)` (the read side), `AnnounceAddressTo(announcer)` (the write side), or
`ParticipateIn(registry)` for a registry with both faces. Declaring no registry means the endpoint
discovers nothing and only serves: it connects to no other endpoint, and a tommand it sends that
its own roster serves executes inline, in the sender's execution — nothing self-addressed ever crosses the
wire, so no discovery is needed for an endpoint's conversation with itself. For processes on one
machine the registry is the `InterprocessEndpointRegistry`, which is also the announcer, so an endpoint
declares both sides at once with `ParticipateIn(registry)` — endpoints announce their freshly generated
addresses and discover each other with zero configuration; the whole story lives in
[same-machine hosting](wip/same-machine-hosting.md).

## Testing hosting

Compze tests are black-box integration tests: they host real endpoints with the real pipelines against real
(throwaway, pooled) databases — no mocks. The testing side therefore needs everything production hosting has,
plus test concerns: a database pool, the pluggable-component matrix, and guarantees that a test cannot pass
while work is still in flight. The testing host (`TestingEndpointHost` in `Compze.Tessaging.Hosting.Testing`)
provides this as concrete per-tier wiring:

```csharp
using var host = TestingEndpointHost.Create();
var backend = host.RegisterExactlyOnceEndpoint("Backend", backendId, endpoint =>
{
   endpoint.MapTypes(mapper => mapper.RegisterMyDomainTypeMappings());
   endpoint.RegisterTessageHandlers(handle => ...);
});
```

`RegisterExactlyOnceEndpoint` / `RegisterBestEffortEndpoint` hand each endpoint its test concerns at
construction — the host's one tessages-in-flight tracker, the current test's transport protocol, the pooled
test database keyed by the endpoint's id (exactly-once tier, so an endpoint keeps its database across host
rebuilds and specs can script restarts), and participation in the host's endpoint registry: a real
`InterprocessEndpointRegistry` of the host's own (in a per-host temp directory deleted with the host), so
every test runs the production announce/discover pipeline, not a test-only registry. All endpoints are built
from clones of one root container, so they share the test database pool and serializers. On dispose the host
waits until no tessages are in flight and rethrows background exceptions no assertion observed — a test
cannot pass while silently dropping in-flight work (`DisposeAsyncWithoutWaitingForEndpointsToBeAtRest` opts
out, for tests that deliberately leave work scheduled).

**`TypermediaTestClient`** (in `Compze.Tessaging.Hosting.Testing`) is the pure client composed for tests: it
runs in its own container and connects to an endpoint's `Address` over the current test's transport exactly
as an external application would.

Test containers are built through `TestEnv.DIContainer.CreateTestingContainerBuilder()`, whose registrar is a
`TestingComponentRegistrar`: when production wiring like `MsSqlConnectionPool(name)` asks it for a testing
override, it supplies one that resolves connection strings through the test database pool. This is how
production registrars run unmodified against throwaway pooled databases, across every SQL backend, DI
container, and serializer in the current test's `PluggableComponents` configuration. The transport itself is
an axis of that same matrix (`TestEnv.Transport`): the same specifications run over HTTP (ASP.NET Core) and
over named pipes (see [same-machine hosting](wip/same-machine-hosting.md)).
