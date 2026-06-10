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

### What a paradigm is

Compze gives application code two different *ways to converse*, and these are what this document calls
**paradigms**. (Compze names its message concepts with a leading *t*: a *tessage* is a message, a *tevent* an
event, a *tommand* a command, a *tuery* a query.)

- **Tessaging** is the asynchronous messaging paradigm — the service-bus conversation style. Handlers publish
  tevents and send tommands; subscribers receive every tevent whose type they are compatible with; an
  inbox/outbox provides transactional, exactly-once delivery; a scheduler delivers tommands at a future time.
  Tessaging is for *telling the world what happened* and trusting it arrives.
- **Typermedia** is the request/response paradigm — hypermedia principles ("navigate an API by following
  links") extended with .NET types. A client navigates an endpoint's API by executing tueries and tommands
  and following the typed results to the next step. Typermedia is for *asking questions and getting answers,
  now*.

Each paradigm is a complete vertical: its own message kinds, routing, handler execution, and transport. They
are peers, and neither knows the other exists.

### The one design rule

Everything in the hosting design follows from a single rule:

> **The hosting machinery knows no paradigm. Paradigms plug themselves in. The application decides which
> paradigms each endpoint speaks.**

Why this rule? Because what an endpoint *is* (a container, a lifecycle, an identity) is a different concept
from what an endpoint *can say* (Tessaging? Typermedia? both?). Keeping them apart means each paradigm can be
developed, shipped, and — crucially — tested entirely on its own; an endpoint's capabilities are declared in
one visible place, its own setup; and a new paradigm can be added without touching the hosting machinery at
all.

The rule is applied twice, at two scales:

1. **Per endpoint:** paradigms plug into an endpoint being built, as *endpoint features*.
2. **Per testing host:** paradigms plug into the test host, as *testing host features*, so one paradigm-blind
   testing host serves Tessaging-only, Typermedia-only, and combined tests alike.

The rest of this document walks the model top-down: the code layout that enforces the rule, then endpoints
and hosts, then how a paradigm plugs in and runs, then production and testing composition.

## The shape of the code: three layers

The rule is enforced by the dependency directions between three layers:

| Layer | Home | Knows about paradigms? |
|---|---|---|
| **Contracts** | `Compze.Abstractions` → `Compze.Abstractions.Hosting.Public` | No |
| **Mechanism** | `Compze.Hosting` (production), `Compze.Hosting.Testing` (testing) | No |
| **Paradigm features** | `Compze.Tessaging`/`Compze.Tessaging.Hosting.Testing`, `Compze.Typermedia.Client`/`Compze.Typermedia.Hosting.Testing` | Each knows only itself |

The contracts define what hosting *is*: `IEndpointHost`, `IEndpoint`, `IEndpointBuilder`,
`IEndpointComponent`, plus the endpoint value types (`EndpointId`, `EndpointAddress`,
`EndpointConfiguration`) and `IEndpointRegistry`. The mechanism implements them without referencing any
paradigm project. Each paradigm references the contracts and plugs itself in. Composition — which paradigms an
endpoint actually gets — happens at the outermost layer: the application or the test.

## Endpoints and hosts

Declaring an endpoint looks like this, and is the same regardless of host:

```csharp
var host = EndpointHost.Production.Create(CreateContainerBuilder);
var endpoint = host.RegisterEndpoint("AccountManagement", new EndpointId(Guid.Parse("...")), builder =>
{
   builder.TypeMapper.RegisterMyDomainTypeMappings();
   builder.RegisterTessagingHandlers.ForTevent((IAccountTevent tevent) => ...);
   builder.RegisterTypermediaHandlers.ForTuery((AccountTuery tuery) => ...);
});
host.Start();
```

`RegisterEndpoint`'s callback receives the endpoint's `IEndpointBuilder` — the declaration surface through
which everything the endpoint will be is stated before its container is built. When the callback returns, the
mechanism builds the container and assembles the endpoint. The builder itself contributes only what every
endpoint needs regardless of paradigm: the type mapper (pre-mapped with the shared message hierarchy and the
discovery types), the endpoint's identity, the configuration provider, and the infrastructure-query executor
that endpoint discovery runs on.

Notice what the example already shows: the endpoint above speaks both paradigms, not because any host or
builder decided so, but because its own setup registered handlers with both. That is the design rule made
concrete — which brings us to how that line of code works.

## How a paradigm plugs in: features

The builder does not know which paradigms exist. It offers one seam, and each paradigm packages itself as a
**feature** behind it:

- `GetOrAddFeature<TFeature>(createFeature)` — creates the feature once per endpoint and remembers it.
  `AddTessaging()` and `AddTypermedia()` are one-line extension methods over this call, and
  `RegisterTessagingHandlers` / `RegisterTypermediaHandlers` are extension properties that add the feature on
  first touch and return its handler registrar — so registering a handler is all it takes to give an endpoint
  a paradigm.
- Inside its constructor, a feature uses the rest of the builder surface to wire its whole vertical: it
  registers its services with `builder.Registrar`, maps its message types with `builder.TypeMapper`,
  schedules post-container-build work with `builder.OnContainerBuilt(...)` (e.g. registering discovery
  handlers with the resolved query executor), and adds its runtime lifecycle with `builder.AddComponent(...)`.

`TessagingEndpointFeature` (in `Compze.Tessaging`) wires the inbox, outbox, tommand scheduler, router, and
service bus session. `TypermediaEndpointFeature` (in `Compze.Typermedia.Client`) wires the handler registry
and executor, the in-process navigator, the transport server, and discovery. A feature lives in its paradigm's
own assembly, where the internal access it needs is natural — no `InternalsVisibleTo` back-doors.

## How a paradigm runs: components and the lifecycle phases

A feature describes what an endpoint *has*; an `IEndpointComponent` (added via `AddComponent`) is what
actually *runs* — the paradigm's runtime lifecycle. Components are created when the endpoint starts
listening; the container is built by then, so the factory can resolve whatever it needs.

The lifecycle has two phases — listening, then sending — and the ordering guarantee is **host-wide**: when a
host starts, every endpoint's components finish `StartListeningAsync` before any component anywhere starts
`StartSendingAsync`. That is the whole point of the split: nothing can send to an endpoint that is not yet
ready to receive. Stopping runs in reverse. The sending-phase members are default no-ops because some
components only listen: Typermedia's component starts and stops its transport server and never sends;
Tessaging's component listens with its inbox and scheduler, then in the sending phase connects its router to
every address in the `IEndpointRegistry` and starts its outbox.

## Addresses are paradigm extension properties

`IEndpoint` has no address members, because "the endpoint's address" is not one concept — each transport has
its own. Each paradigm contributes an extension property reading its own component:
`endpoint.TessagingAddress` (the inbox address, from `EndpointTessagingExtensions`) and
`endpoint.TypermediaAddress` (the transport server's address, from `EndpointTypermediaExtensions`). Both are
null until the endpoint is listening — there is nothing to connect to before that — and for endpoints without
that paradigm's pipeline.

## Production hosting

`EndpointHost.Production.Create(containerFactory)` — the factory produces a fresh container builder per
endpoint. Endpoints read configuration through `IConfigurationParameterProvider`
(`AppSettingsJsonConfigurationParameterProvider` from `appsettings.json` is registered by default) and fall
back to `AppConfigEndpointRegistry` for the addresses of other endpoints.

## Testing hosting

Compze tests are black-box integration tests: they host real endpoints with the real pipelines against real
(throwaway, pooled) databases — no mocks. The testing side therefore needs everything production hosting has,
plus test concerns: a database pool, the pluggable-component matrix, and guarantees that a test cannot pass
while work is still in flight. `Compze.Hosting.Testing` provides this by repeating the design rule one level
up: `TestingEndpointHost` is paradigm-blind, and paradigms plug into the *host* as
`ITestingEndpointHostFeature`s:

```csharp
using var host = TestingEndpointHost.Create(new TessagingTestingEndpointHostFeature(),
                                            new TypermediaTestingEndpointHostFeature());
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
- **`TessagingTestingEndpointHostFeature`** registers, per endpoint: a host-wide tessages-in-flight tracker
  (this is what the dispose-time quiescence wait reads), an `IEndpointRegistry` listing the host's endpoints'
  inbox addresses (so routers connect to every endpoint in the host), the Tessaging transport, the Tessaging
  vertical's SQL persistence stack, and finally `AddTessaging()`. The pre-registrations matter:
  `TessagingEndpointFeature` guards its tracker and registry defaults with `IsRegistered`, so the host's
  versions win.
- **`TypermediaTestingEndpointHostFeature`** registers the Typermedia transport and `AddTypermedia()`.
- **`TypermediaTestClient`** (in `Compze.Typermedia.Hosting.Testing`) is a remote client in its own container,
  connecting to an endpoint's `TypermediaAddress` over HTTP exactly as an external application would.

Test containers are built through `TestEnv.DIContainer.CreateTestingContainerBuilder()`, whose registrar is a
`TestingComponentRegistrar`: when production wiring like `MsSqlConnectionPool(name)` asks it for a testing
override, it supplies one that resolves connection strings through the test database pool. This is how
production registrars run unmodified against throwaway pooled databases, across every SQL backend, DI
container, and serializer in the current test's `PluggableComponents` configuration.

The shared HTTP plumbing (the `IHttpClientFactoryCE` and the infrastructure-query transport and controller
that endpoint discovery runs on) is paradigm-neutral, so both paradigm transports demand it through the
guarded `CurrentTestsInfrastructureTransportIfNotRegistered()` — whichever registers first wins, and an
endpoint hosting both paradigms gets it once.

## Adding a new paradigm

The proof of the design rule is what it takes to add a third paradigm:

1. Define the pipeline's services and an `IEndpointComponent` for its runtime lifecycle.
2. Write an endpoint feature: a class whose constructor wires everything through the `IEndpointBuilder`
   surface, plus an `AddX()` extension method over `GetOrAddFeature` and (if it has handlers) a
   `RegisterXHandlers` extension property.
3. If the paradigm has an address, expose it as an extension property on `IEndpoint` reading your component.
4. For tests, write an `ITestingEndpointHostFeature` that registers the paradigm's test transport and calls
   `AddX()` for every endpoint — and, if the paradigm has background work, hold the tracker for it and
   implement the at-rest members.

Nothing in `Compze.Abstractions`, `Compze.Hosting`, or `Compze.Hosting.Testing` needs to change.
