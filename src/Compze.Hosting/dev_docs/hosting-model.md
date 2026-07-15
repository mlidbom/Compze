# The Compze hosting model

This document takes a developer who is new to Compze from zero to understanding how hosting works — what is
actually running when a Compze application runs, how the pieces relate, and how the same machinery hosts both
production applications and the test suite.

## The big picture

### What a running Compze application is

A Compze application is built as one or more **endpoints**. An endpoint is the unit of isolation and
deployment — think of it as a service: it has its own dependency injection container, its own connection
string and storage, its own registered message handlers, and its own transports listening on the network.
Endpoints do not call each other's code; they converse by sending messages. Several endpoints can run in one
process, or each in its own — the model is the same.

**Hosting** is everything between "here are my handlers and domain logic" and "a running system serving
them": building each endpoint's container, wiring its message pipelines, starting its transports in a safe
order, letting endpoints discover each other, and tearing it all down cleanly. A **host** is the object that
owns a set of endpoints and drives that shared lifecycle.

### Two ways to converse: Tessaging and Typermedia

Compze gives application code two **communication styles** — two different ways for endpoints and clients to
converse. (Compze names its message concepts with a leading *t*: a *tessage* is a message, a *tevent* an
event, a *tommand* a command, a *tuery* a query.)

- **Tessaging** is the asynchronous style — the service-bus conversation. Handlers publish tevents and send
  tommands; subscribers receive every tevent whose type they are compatible with; an inbox/outbox provides
  transactional, exactly-once delivery; a scheduler delivers tommands at a future time. Tessaging is for
  *telling the world what happened* and trusting it arrives.
- **Typermedia** is the request/response style — hypermedia principles ("navigate an API by following
  links") extended with .NET types. A client navigates an endpoint's API by executing tueries and tommands
  and following the typed results to the next step. Typermedia is for *asking questions and getting answers,
  now*.

Each style is a complete vertical: its own message kinds, routing, handler execution, and transport. They are
peers, and neither knows the other exists.

### The one design rule

Everything in the hosting design follows from a single rule:

> **The hosting machinery knows nothing of Tessaging, Typermedia, or any other capability. Capabilities plug
> themselves in as features. The application decides what each endpoint speaks.**

Why this rule? Because what an endpoint *is* (a container, a lifecycle, an identity) is a different concept
from what an endpoint *can say* (Tessaging? Typermedia? both?). Keeping them apart means each communication
style can be developed, shipped, and — crucially — tested entirely on its own; an endpoint's capabilities are
declared in one visible place, its own setup; and a new capability can be added without touching the hosting
machinery at all.

The rule is applied three times, at three scales:

1. **Per endpoint:** capabilities plug into an endpoint being built, as *endpoint features*.
2. **Within each communication style:** the style's synchronous in-process core is itself a feature, and
   distribution is a feature composed on top of it — so a style can be used entirely in-process, with no
   transports and nothing to host.
3. **Per testing host:** capabilities plug into the test host, as *testing host features*, so one neutral
   testing host serves Tessaging-only, Typermedia-only, and combined tests alike.

The rest of this document walks the model top-down: the code layout that enforces the rule, then endpoints
and hosts, then how a communication style plugs in and runs, then production and testing composition.

## The shape of the code: three layers

The rule is enforced by the dependency directions between three layers:

| Layer | Home | Knows about Tessaging/Typermedia? |
|---|---|---|
| **Contracts** | `Compze.Abstractions` → `Compze.Abstractions.Hosting.Public` | No |
| **Mechanism** | `Compze.Hosting` (production), `Compze.Hosting.Testing` (testing) | No |
| **Features** | `Compze.Tessaging`/`Compze.Tessaging.Hosting.Testing`, `Compze.Typermedia` + `Compze.Typermedia.Client`/`Compze.Typermedia.Hosting.Testing` | Each knows only itself |

The contracts define what hosting *is*: `IEndpointHost`, `IEndpoint`, `IEndpointBuilder`,
`IEndpointComponent`, plus the endpoint value types (`EndpointId`, `EndpointAddress`,
`EndpointConfiguration`) and `IEndpointRegistry`. The mechanism implements them without referencing any
Tessaging or Typermedia project. Each communication style references the contracts and plugs itself in.
Composition — what each endpoint actually speaks — happens at the outermost layer: the application or the test.

## Endpoints and hosts

Declaring an endpoint looks like this, and is the same regardless of host:

```csharp
var host = EndpointHost.Production.Create(CreateContainerBuilder);
var endpoint = host.RegisterEndpoint("AccountManagement", new EndpointId(Guid.Parse("...")), builder =>
{
   builder.TypeMapper.RegisterMyDomainTypeMappings();

   var foundation = builder.ComposeEndpoint(it => it.AspNetCoreEndpointTransport()
                                                    .SqliteEndpointDatabase("AccountManagement"));
   foundation.AddExactlyOnceTessaging(tessaging => tessaging.NewtonsoftSerializer());
   foundation.AddDistributedTypermedia(typermedia => typermedia.NewtonsoftSerializer());

   builder.RegisterTessagingHandlers.ForTevent((IAccountTevent tevent) => ...);
   builder.RegisterTypermediaHandlers.ForTuery((AccountTuery tuery) => ...);
});
host.Start();
```

`ComposeEndpoint` declares the endpoint's *foundation* — its transport protocol and, when it persists, its
database — exactly once; the features are added on top of it. The foundation's type carries the database
engine, so adding exactly-once Tessaging on a sqlite foundation registers Tessaging's sqlite sql layers: the
pairing is routed by the compiler, and a foundation the feature needs but the setup never declared fails at
setup time with an error naming the missing declaration.

`RegisterEndpoint`'s callback receives the endpoint's `IEndpointBuilder` — the declaration surface through
which everything the endpoint will be is stated before its container is built. When the callback returns, the
mechanism builds the container and assembles the endpoint. The builder itself contributes only what every
endpoint needs no matter what it speaks: the type mapper (pre-mapped with the shared message hierarchy and
the discovery types), the endpoint's identity, the configuration provider, and the endpoint-discovery query
executor through which the endpoint answers discovery.

Notice what the example already shows: the endpoint above converses with other endpoints in both styles, not
because any host or builder decided so, but because its own setup declares `AddExactlyOnceTessaging()` and
`AddDistributedTypermedia()`. Registering handlers declares something smaller — that the endpoint
*understands* those tessages in-process; conversing over the network is its own explicit declaration. That is
the design rule made concrete — which brings us to how those lines of code work.

## How a capability plugs in: features

The builder does not know which capabilities exist. It offers one seam, and each capability packages itself
as a **feature** behind it:

- `GetOrAddFeature<TFeature>(createFeature)` — creates the feature once per endpoint and remembers it.
  `AddExactlyOnceTessaging()`, `AddTransientTessaging()`, `AddInProcessTessaging()`, `AddDistributedTypermedia()`, and
  `AddInProcessTypermedia()` are one-line extension methods over this call, and
  `RegisterTessagingHandlers` / `RegisterTypermediaHandlers` are extension properties that add the style's
  in-process core on first touch and return its handler registrar.
- Inside its constructor, a feature uses the rest of the builder surface to wire its whole vertical: it
  registers its services with `builder.Registrar`, maps its message types with `builder.TypeMapper`,
  schedules post-container-build work with `builder.OnContainerBuilt(...)` (e.g. registering discovery
  handlers with the resolved query executor), and adds its runtime lifecycle with `builder.AddComponent(...)`.

The design rule is applied a third time *inside* each communication style: the style's in-process core is
itself a feature, and distribution is a feature composed on top of it (via `GetOrAddFeature`, so the core is
wired once whether it arrives alone or under distribution).

- **Tessaging splits three ways**, each layer containing the one below it.
  `InProcessTessagingEndpointFeature` (in `Compze.Tessaging`, `AddInProcessTessaging()`) is the style's
  synchronous core: the handler registry, the synchronous in-process tevent delivery every tevent travels,
  and the endpoint's one `ITeventPublisher` — the one way to publish a tevent, routing each by the delivery
  contract its type declares (see
  [the tevent delivery model](../../Compze.Tessaging/dev_docs/tevent-delivery-model.md)). With nothing but this
  feature the endpoint wires no remote delivery legs — no transport, inbox, outbox, or tommand scheduler —
  so tevents are delivered synchronously to this process's handlers, in the publisher's transaction.
  `TransientTessagingEndpointFeature` (`AddTransientTessaging()`) composes it into the transport-speaking
  core: the transport server, the router that connects to the other endpoints, and the transient tevent
  delivery leg — guarantee-free Tessaging, persisting nothing, so it composes on the database-less
  foundation; everything exactly-once fails loud at setup or publish.
  `ExactlyOnceTessagingEndpointFeature` (`AddExactlyOnceTessaging()`) composes that core and adds the
  exactly-once vertical: inbox, outbox, tommand scheduler, and service bus session — and wiring the outbox is
  what wires the durable tevent delivery leg the publisher routes every `IExactlyOnceTevent` through, and
  what grants the router's connections their durable exactly-once delivery streams. Whether a tevent
  crosses the wire is a property of the tevent's type, honored by the legs the composition wires — not an
  endpoint-wide mode — which is also what keeps `RegisterTessagingHandlers` order-independent of every other
  Tessaging declaration.
- **Typermedia splits two ways**, because it has no mode-exclusive service: distributed Typermedia simply
  contains in-process Typermedia. `InProcessTypermediaEndpointFeature` (in `Compze.Typermedia`,
  `AddInProcessTypermedia()`) wires the handler registry and the `IInProcessTypermediaNavigator` through
  which strictly local tueries and tommands execute synchronously, in the caller's transaction.
  `DistributedTypermediaEndpointFeature` (in `Compze.Typermedia.Client`, `AddDistributedTypermedia()`)
  composes it and adds the handler executor that serves remote clients, and discovery.
- **Serving is shared.** An endpoint runs **one transport server**, whatever it speaks: every
  transport-speaking feature composes `EndpointTransportServerFeature` (in `Compze.Internals.Transport`) — the same
  `GetOrAddFeature` pattern one level down — and *contribute their request handling to it* (request-kind
  handlers, served identically by the named-pipe and ASP.NET Core transports) rather than each running a
  server of its own. One server means one address per endpoint, which is what lets an endpoint registry map
  an `EndpointId` to a single address; the server itself answers the endpoint-discovery queries endpoint
  discovery runs on, which every endpoint serves no matter what it speaks. Which transport implements the
  server is composition: each transport registers its `IEndpointTransportServer` implementation guarded, so
  every style's transport registration can demand a server and the first wins.

A feature lives in its own capability's assembly, where the internal access it needs is natural — no
`InternalsVisibleTo` back-doors.

## How a capability runs: components and the lifecycle phases

A feature describes what an endpoint *has*; an `IEndpointComponent` (added via `AddComponent`) is what
actually *runs* — the capability's runtime lifecycle. Components are created when the endpoint starts
listening; the container is built by then, so the factory can resolve whatever it needs.

The lifecycle has two phases — listening, then sending — and the ordering guarantee is **host-wide**: when a
host starts, every endpoint's components finish `StartListeningAsync` before any component anywhere starts
`StartSendingAsync`. That is the whole point of the split: nothing can send to an endpoint that is not yet
ready to receive. Stopping runs in reverse. The sending-phase members are default no-ops because some
components only listen. The components in play: `EndpointTransportServerFeature`'s component runs the
endpoint's one transport server through the listening phase, and announces the endpoint's address to every
declared `IEndpointAddressAnnouncer` as the first act of the sending phase — the host-wide ordering makes
that the moment every listener everywhere is ready, so an announced address is always one whose whole
endpoint can actually serve (retraction is the mirror image: the first act of the host's stopping).
`TransientTessagingEndpointComponent` — the transport-speaking Tessaging core's lifecycle — sets its router
reconciling against the `IEndpointRegistry`'s membership in the sending phase (continuously, so endpoints
that appear, disappear, or restart at a new address are followed — see
[same-machine hosting](same-machine-hosting.md)) and starts the connections' delivery streams.
`ExactlyOnceTessagingEndpointComponent` listens with its inbox and scheduler and initializes the outbox's
durable storage — all in the listening phase, so that when sending starts anywhere, every connection's
exactly-once stream finds the storage ready to load its recovery backlog from.
`DistributedTypermediaEndpointComponent` drives nothing — the shared server serves its requests — and exists
as the endpoint surface's evidence that distributed Typermedia is listening. Only the
transport-speaking features add components at all — an endpoint declaring only in-process features has no
runtime lifecycle, and the host starts it with nothing to drive.

## Addresses are extension properties

`IEndpoint` has no address members, because the hosting machinery does not know which communication styles an
endpoint speaks — per-style *presence* is each style's own concern. Each communication style contributes an
extension property reading its own component: `endpoint.TessagingAddress` (from `EndpointTessagingExtensions`)
and `endpoint.TypermediaAddress` (from `EndpointTypermediaExtensions`). The *value* behind both is the same —
the endpoint's one transport-server address — but each property is null for endpoints without that style's
distributed pipeline, so a property answers "can I converse with this endpoint in this style, and where".
Both are also null until the endpoint is listening — there is nothing to connect to before that (an
in-process endpoint has no address: there is nothing to connect to, ever).

## In-process composition — when there is nothing to host

Everything above assumes an endpoint that converses with other endpoints. But both communication styles have
a purely synchronous core that is valuable entirely on its own, inside a single process: publishing tevents
that restructure the internal flow of an application, and executing strictly local tueries and tommands
through the `IInProcessTypermediaNavigator`. Everything in that core runs synchronously, on the calling
thread, within the caller's transaction — so there are no transports to start, no discovery, no background
work, and therefore *nothing to host*. In-process Compze is container wiring, not hosting.

Each style is composed into a plain container by one registrar extension — the same wiring units its endpoint
features are built from, so there is exactly one definition of what each style is:

```csharp
var builder = /* any Compze container builder */;
builder.Registrar
       .InProcessTessaging()    // handler registry, synchronous in-process tevent delivery, ITeventPublisher (no remote legs)
       .InProcessTypermedia();  // handler registry, IInProcessTypermediaNavigator
var container = builder.Build();
```

Handlers are registered after the container is built, through the resolved `ITessageHandlerRegistrar` /
`ITypermediaHandlerRegistrar` — and since application handler-registration code is written against those same
registrar interfaces in every composition, the same registrations run unchanged under an in-process container,
an in-process endpoint, or a distributed endpoint.

Three things to know:

- **"In-process" describes communication, not storage.** A real, persistent tevent store composes with
  in-process Tessaging unchanged; a taggregate's committed tevents are simply delivered only to this
  process's handlers.
- **No type-id ceremony.** In-process dispatch routes by `System.Type`; type-id mappings exist for the wire
  and for persistent-store serialization. The compositions supply a default type mapper when none is
  registered; an application whose tevent store needs domain mappings registers its own first.
- **Wanting guaranteed tommand delivery does not make an endpoint "in-process".** A Tessaging tommand's type
  *is* its delivery contract — exactly-once, transactional, asynchronous — and stripping that synchronously
  would lie about the type. The synchronous local ask already has a truthful home: Typermedia's strictly
  local tommand. An application that wants the guarantees within a single process is a distributed endpoint
  that happens to be alone in its host; Compze already routes self-sent tommands through the outbox for
  exactly this reason.

The same cores are also available to hosted endpoints as the `AddInProcessTessaging()` /
`AddInProcessTypermedia()` features — for an endpoint that, say, speaks exactly-once Tessaging to the world
while structuring its own request handling with strictly local Typermedia navigation, without paying for a
transport server it never serves.

## Production hosting

`EndpointHost.Production.Create(containerFactory)` — the factory produces a fresh container builder per
endpoint. Endpoints read configuration through `IConfigurationParameterProvider`: an endpoint setup that
registers its own provider wins; `AppSettingsJsonConfigurationParameterProvider` reading `appsettings.json`
is only the default. How endpoints find each other is a declaration on the transport-speaking Tessaging core:
`AddTransientTessaging().DiscoverEndpointsThrough(registry)`, which `AddExactlyOnceTessaging()` delegates to — an endpoint declaring no registry falls back
to reading other endpoints' addresses from configuration (`AppConfigEndpointRegistry`). For processes on one
machine the registry is the `InterprocessEndpointRegistry`, which is also the announcer, so an endpoint
declares both sides at once with `ParticipateIn(registry)` — endpoints announce their freshly generated
addresses and discover each other with zero configuration; the whole story lives in
[same-machine hosting](same-machine-hosting.md).

## Testing hosting

Compze tests are black-box integration tests: they host real endpoints with the real pipelines against real
(throwaway, pooled) databases — no mocks. The testing side therefore needs everything production hosting has,
plus test concerns: a database pool, the pluggable-component matrix, and guarantees that a test cannot pass
while work is still in flight. `Compze.Hosting.Testing` provides this by repeating the design rule one level
up: `TestingEndpointHost` knows nothing of Tessaging or Typermedia either, and capabilities plug into the
*host* as `ITestingEndpointHostFeature`s:

```csharp
using var host = TestingEndpointHost.Create(new ExactlyOnceTessagingTestingEndpointHostFeature(),
                                            new DistributedTypermediaTestingEndpointHostFeature());
```

Every endpoint the host registers gets, before the test's own setup runs, each feature's standard test
wiring — so individual tests don't repeat it. There is no "combined host" type: a Tessaging-only,
Typermedia-only, or combined host is just `Create` with the matching features.

What the pieces do:

- **`TestingEndpointHost`** builds all endpoints from clones of one root container, so they share the test
  database pool and serializers, and gives every endpoint the shared transport infrastructure. On dispose it
  asks each feature to wait until its background work is at rest and rethrows background exceptions no
  assertion observed — a test cannot pass while silently dropping in-flight work
  (`DisposeAsyncWithoutWaitingForEndpointsToBeAtRest` opts out, for tests that deliberately leave work
  scheduled).
- **`ExactlyOnceTessagingTestingEndpointHostFeature`** registers, per endpoint: a host-wide
  tessages-in-flight tracker (this is what the dispose-time quiescence wait reads), the endpoint transport of
  the current test's protocol, the Tessaging vertical's SQL persistence stack, and finally `AddExactlyOnceTessaging()` declaring
  `DiscoverEndpointsThrough` an `IEndpointRegistry` listing the host's endpoints' addresses (so routers
  connect to every endpoint in the host). The tracker pre-registration matters:
  the transport-speaking Tessaging core guards its tracker default with `IsRegistered`, so the host's
  version wins.
- **`DistributedTypermediaTestingEndpointHostFeature`** registers the Typermedia transport and
  `AddDistributedTypermedia()`.
- **`TypermediaTestClient`** (in `Compze.Typermedia.Hosting.Testing`) is a remote client in its own container,
  connecting to an endpoint's `TypermediaAddress` over the current test's transport exactly as an external
  application would.

Test containers are built through `TestEnv.DIContainer.CreateTestingContainerBuilder()`, whose registrar is a
`TestingComponentRegistrar`: when production wiring like `MsSqlConnectionPool(name)` asks it for a testing
override, it supplies one that resolves connection strings through the test database pool. This is how
production registrars run unmodified against throwaway pooled databases, across every SQL backend, DI
container, and serializer in the current test's `PluggableComponents` configuration.

The transport itself is an axis of that same matrix (`TestEnv.Transport`): the same specifications run over
HTTP (ASP.NET Core) and over named pipes (see [same-machine hosting](same-machine-hosting.md)). The
endpoint-discovery query transport every endpoint needs no matter what it speaks belongs to no single
communication style, so every communication style's transport registration demands it itself through the
guarded `…EndpointDiscoveryQueryTransportIfNotRegistered()` registrars: whichever registers first wins, and
an endpoint hosting both styles gets it once.

## Adding a new communication style

The proof of the design rule is what it takes to add a third communication style:

1. Define the style's in-process core — its handler registry and synchronous dispatch — and wrap it in an
   endpoint feature behind `AddInProcessX()` and (if it has handlers) a `RegisterXHandlers` extension
   property, plus a registrar extension composing the same wiring into a plain container.
2. Define the distribution pipeline's services and an `IEndpointComponent` for its runtime lifecycle, and
   wrap them in a feature that composes the in-process core via `GetOrAddFeature`, behind
   `AddDistributedX()`.
3. If the style has an address, expose it as an extension property on `IEndpoint` reading your component.
4. For tests, write an `ITestingEndpointHostFeature` that registers the style's test transport and calls
   `AddDistributedX()` for every endpoint — and, if the style has background work, hold the tracker for it
   and implement the at-rest members. The in-process core needs no testing host feature: there is no
   transport to wire and no background work to await.

Nothing in `Compze.Abstractions`, `Compze.Hosting`, or `Compze.Hosting.Testing` needs to change.
