# Compze hosting

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
order, letting endpoints discover each other, and tearing it all down cleanly. An endpoint is
**first-class**: compose it, start it, dispose it — it drives its own lifecycle. A **host** is an optional
convenience owning several endpoints' lifecycles in one process; it adds nothing an endpoint cannot do
alone.

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

There are exactly three things an application composes, all in `Compze.Tessaging` (the paradigm itself —
tessage kinds, the consistency law, the engine — is
[Tessaging](../../Compze.Tessaging/dev_docs/tessaging.md)):

1. **The LocalTessagingEngine** — the tessage-conversing heart of one container, for the application that
   converses only with itself (see "The engine" below). Not an endpoint: no identity, no address, no wire.
2. **An endpoint** — an engine given identity and a wire. There are exactly two endpoint types, differing
   only in what crossing the endpoint boundary guarantees: `BestEffortEndpoint` (no database; per-peer
   best-effort tevent queues) and `ExactlyOnceEndpoint` (everything the best-effort endpoint has, plus the
   durable vertical: inbox, outbox, durable peer memory, and the tommand senders). **Both serve all
   four tessage kinds, unconditionally.**
3. **The pure client** — `TypermediaClient`: a navigator and a transport client with no server, for an
   external application navigating endpoints' typermedia at explicitly known addresses.

An endpoint is a plain composition root: its choices are parameters, not plugins. Its declaration
surface takes the transport protocol, the one serializer, the database (exactly-once tier), the topology
declarations, and the same engine declaration block every endpoint and plain container speaks — and a missing
declaration fails loud at build, naming what is missing.

## The shape of the code

| Layer | Home | Knows about the styles? |
|---|---|---|
| **Contracts** | `Compze.Tessaging` → `Compze.Tessaging.Endpoints` (+ `.Discovery`) | No — `IEndpointHost`, `IEndpoint`, the endpoint value types (`EndpointId`, `EndpointConfiguration`), and in `.Discovery` the address `EndpointAddress` with `IEndpointRegistry`/`IEndpointAddressAnnouncer` |
| **The host mechanism** | `Compze.Hosting` | No — `EndpointHost` starts and disposes `IEndpoint`s it never looks inside |
| **The composed shapes** | `Compze.Tessaging` (`Compze.Tessaging.Endpoints`, `Compze.Tessaging.Engine`, the Typermedia namespaces) | Yes — the endpoint types compose both siblings, always |

The host mechanism implements the contracts without referencing what an endpoint speaks: it hands each
registered declaration a fresh container builder from its factory and receives the built `IEndpoint` back
(`RegisterEndpoint(declaration)` — the declaration's `Build` runs in the host's environment). What each
endpoint actually is — is declared at the outermost layer: the application or the test.

## Endpoints and hosts

An endpoint is declared as a class — an endpoint-declaration — and the same declaration builds under every
host. (This section shows the shape; the full model — identity types, environments and their decoration, the
build template, and the reasoning behind the design — is
[endpoint-declarations](../../Compze.Tessaging/dev_docs/endpoint-declarations.md).)

```csharp
class AccountManagementEndpointDeclaration : ExactlyOnceEndpointDeclaration<AccountManagementEndpointDeclaration>, IEndpointIdentity
{
   public static string Name => "AccountManagement";
   public static EndpointId Id { get; } = new(Guid.Parse("..."));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMyDomainTypeMappings();

   protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) => handle
      .ForTevent(async (IAccountTevent tevent, IUnitOfWorkResolver unitOfWork) => ...);

   protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) => handle
      .ForTuery((AccountTuery tuery) => ...);
}

class ProductionEnvironment : IEndpointEnvironment
{
   public void Configure(EndpointBuilder endpointBuilder) => endpointBuilder
      .TransportProtocol(registrar => registrar.AspNetCoreEndpointTransport())
      .NewtonsoftSerializer()
      .ParticipateIn(registry);

   public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) =>
      endpointBuilder.SqliteDomainDatabase("AccountManagement");
}

var host = EndpointHost.Production.Create(CreateContainerBuilder, new ProductionEnvironment());
var endpoint = host.RegisterEndpoint(new AccountManagementEndpointDeclaration());
host.Start();
```

The declaration is what the endpoint IS — its compiler-enforced identity (the
`IEndpointIdentity` statics the `TIdentity` type parameter reaches; usually the declaration is its own
identity), its handler registrations, its topology stance — while the `IEndpointEnvironment` is everything a
deployment decides: the transport protocol, the one serializer, discovery participation, and — on the
exactly-once tier — the actual domain database. Building runs one template (`Build`): the environment
configures first, the declaration's scalar aspects and registration overrides follow, and the tier's general
`Declare` override receives the full `EndpointBuilder` (its tier subtype) last — store integrations (a
tevent store's `HandleTaggregate`, a document db's `HandleDocumentType`) plug in there, handler contributors
like any other. The build closes the roster: the builder exists only inside the build, and any attempt to
declare afterward explodes. The named environment declarations come from the implementation packages, each
filling the one parameter it names: the transport protocol (`NamedPipeEndpointTransport()` /
`AspNetCoreEndpointTransport()`), the endpoint's one serializer (`NewtonsoftSerializer()`), and the domain
database, whose named declaration registers the whole engine pairing (`SqliteDomainDatabase(...)` and kin:
the connection pool, the type-id interner, and Tessaging's sql layers for that engine).

## The lifecycle phases

Each endpoint drives its own lifecycle: `StartAsync` runs the up-phases in order — listen → announce →
send — and disposal runs the mirror — retract → stop sending → stop listening. The per-endpoint ordering is
what makes "an announced address is always one that is actually listening" true in every process topology,
and the mirror keeps an address from being advertised after the endpoint goes deaf. There is deliberately
**no ordering between endpoints** — a host starting several starts them in parallel, each running its own
phases — because any cross-endpoint ordering could only ever hold inside one process: whether endpoints have
discovered each other is topology convergence, identical for co-hosted and separated endpoints, covered by
readiness, waiting sends, queue-while-down, and `RequirePeers` (below).

What each phase does inside an endpoint: the exactly-once tier claims its process lock in the domain
database's endpoint catalog (the first act — whether this process may run the endpoint at all is decided
before anything else touches the database), peer memory loads, the durable vertical initializes, and the
one transport server starts in the listening phase; the address is announced to every
declared `IEndpointAddressAnnouncer` in the announcing phase; and in the sending phase the router converges
on the registry's membership — minus the endpoint's own announced address: routes lead only to *other*
endpoints — and the connections' delivery streams start, loading their recovery backlogs from the storage
the listening phase initialized.

An endpoint runs **one transport server** with one address, serving every remotable capability it speaks —
which is what lets an endpoint registry map an `EndpointId` to a single address, and it is where the
endpoint answers the one discovery question (its name, its `EndpointId`, and its advertisement — the
roster's one projection, covering every tessage kind). The endpoint's `Address` is null until it is
listening.

## Readiness and waiting sends — discovery stops being the application's race to lose

An endpoint's start completes when *it* has started; whether the rest of the topology — co-hosted
endpoints, other processes still starting, a peer restarting mid-day — has been discovered is continuous
convergence. Two composing mechanisms make that nobody's race to lose (see
[peers](../../Compze.Tessaging/dev_docs/peers.md)):

- **Waiting sends** — implicit, per-call: a send whose type has no live, unambiguous route right now waits,
  bounded by the endpoint's **handler-availability patience** (a flat 30 seconds unless the declaration
  declares otherwise — `EndpointBuilder.HandlerAvailabilityPatience`), for the world to become right — a
  first contact, a known peer's return, an ambiguity resolving — then proceeds normally. Only exhausted
  patience fails loud, and the failure tells known-but-down (naming the remembered peer) from never-seen
  (naming the probable deployment error).
- **Readiness** — explicit: `IEndpoint.AwaitReadinessAsync(ReadinessTypes, patience?)` completes when the
  endpoint can reach a handler for every named type — its own roster serves it, an exactly-once tommand
  type has a bindable receiver, or a request/response type has exactly one live route: precisely the
  availability a send would not have to wait for. Awaited on a started endpoint, typically at startup
  before opening traffic, it front-loads the discovery wait a waiting send would otherwise make the first
  unlucky caller pay; an orchestrator's readiness probe wires to it. The type sets come from the
  `ReadinessTypes` reflection factories (`InAssemblyContaining`, `InNamespaceOf`), which admit only the
  remotable single-handler kinds — tevents are multi-subscriber, have no one handler to await, and their
  delivery is fully served by the peer topology's queue-while-down machinery.

## The domain database — storage belongs to the domain, endpoints join it

An exactly-once endpoint declares the **domain database it joins** (`SqliteDomainDatabase(...)` and kin),
never a database of its own: the database is the domain's — the domain data the endpoint's executions touch
lives there, and the exactly-once machinery's atomicity *is* its co-location with that data. Any number of
endpoints join one domain database (the whole story:
[storage](../../Compze.Tessaging/dev_docs/storage.md)):

- **Each endpoint owns a prefixed table-set** (`EndpointTableSet`): its inbox, its outbox and outbox
  dispatching, and its durable peer memory, each table prefixed with the endpoint's name
  (`Backend_InboxTessages`). The prefix is what makes an exactly-once endpoint's name identifier material —
  a letter followed by letters, digits, or underscores, at most 28 characters — asserted loud at
  build, never sanitized silently.
- **The domain-level tables are deliberately unprefixed** — the type-id interner, the tevent store, the
  document db, and the endpoint catalog are the domain's data, shared by every endpoint that joins.
- **One shared table per domain database: the endpoint catalog** — each endpoint's name, `EndpointId`,
  creation time, and the recorded lock holder. It enforces name uniqueness (a name only ever belongs to one
  endpoint; an id never silently re-keys itself under a new name — renaming means decommissioning the old
  storage) and the one-process-per-endpoint rule, and it tells administration which endpoints inhabit the
  database.
- **The process lock** is exclusivity a live holder holds — a session-scoped database lock on a dedicated
  held connection (an OS-level machine-wide mutex on SQLite, which has no server sessions) — claimed as the
  first act of starting to listen, never a time-bounded lease. A crashed process's lock is released by the
  infrastructure (its database session dies with it), so the endpoint's next process claims it immediately —
  crash recovery needs no waiting and no manual cleanup — while a claimant finding the lock held has proof
  of a live holder and fails its start loud, immediately
  (`EndpointAlreadyRunningInAnotherProcessException`, naming the holder); no pause, however long, can make a
  live holder look dead. The lock is released at disposal, after the observation drain, once nothing in the
  process writes to the domain database.

Because endpoints start in parallel under the host, several endpoints' first boot against one fresh domain
database creates the shared schemas concurrently — schema creation therefore serializes under the engine's
advisory lock on every backend that needs one, correct across connections and processes.

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
   .RegisterTessageBusHandlers(handle => handle
      .ForTevent((ISomethingHappenedTevent tevent) => ...))
   .RegisterTypermediaHandlers(handle => handle
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
  and for persistent-store serialization. A plain container supplies a default type mapper
  when none is registered; an application whose tevent store needs domain mappings registers its own first.
- **Wanting guaranteed tommand delivery does not make an endpoint "in-process".** A Tessaging tommand's type
  declares its cross-boundary delivery contract — exactly-once, transactional, asynchronous. Within the
  boundary the consistency law applies instead: a tommand whose handler is in the endpoint's own roster
  executes inline, in the sender's execution — exactly-once by construction, one transaction, no delivery
  machinery involved. An application that wants the guarantees within a single process is a distributed
  endpoint that happens to be alone in its host.

## Production hosting

`EndpointHost.Production.Create(containerFactory, environment)` — the factory produces a fresh container
builder per endpoint, and the environment is where the deployment declares how its endpoints find each other:
`DiscoverEndpointsThrough(registry)` (the read side), `AnnounceAddressTo(announcer)` (the write side), or
`ParticipateIn(registry)` for a registry with both faces. An endpoint whose environment differs from its
co-hosted neighbors' — an extra announcement target, a shared domain database — registers with its own
environment (`RegisterEndpoint(declaration, environment)`), usually a decorating `IEndpointEnvironment`
wrapping the host's. Declaring no registry means the endpoint
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
var backend = host.RegisterEndpoint(new BackendEndpointDeclaration());
```

The declaration class is the same one production hosts — production and tests host the same endpoint by
construction — and the testing host's environment hands each registered declaration its test concerns at
build — the host's one tessages-in-flight tracker, the current test's transport protocol, the pooled
test database keyed by the endpoint's id (exactly-once tier, so an endpoint keeps its database across host
rebuilds and specs can script restarts), and participation in the host's endpoint registry: a real
`InterprocessEndpointRegistry` of the host's own (in a per-host temp directory deleted with the host), so
every test runs the production announce/discover pipeline, not a test-only registry. All endpoints are built
from clones of one root container, so they share the test database pool and serializers. On dispose the host
waits until no tessages are in flight — transport deliveries and queued tevent observations alike, since the
engines' observation dispatch reports to the tracker — and rethrows background exceptions no assertion
observed: a test cannot pass while silently dropping in-flight work or a throwing observer's failure
(`DisposeAsyncWithoutWaitingForEndpointsToBeAtRest` opts out, for tests that deliberately leave work
scheduled). A specification whose very next act rides the host's endpoints having discovered each other —
tevent fan-out membership, peer-memory assertions — awaits `AwaitEndpointsHaveMetEachOtherAsync()` after
starting, instead of racing the reconciliation.

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
