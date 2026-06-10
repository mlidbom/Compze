# The Compze endpoint hosting model

How Compze applications are hosted: what an endpoint is, how the Tessaging and Typermedia paradigms plug into
one, and how production and testing hosts compose them.

## The three layers

| Layer | Home | Knows about paradigms? |
|---|---|---|
| **Contracts** | `Compze.Abstractions` → `Compze.Abstractions.Hosting.Public` | No |
| **Mechanism** | `Compze.Hosting` (production), `Compze.Hosting.Testing` (testing) | No |
| **Paradigm features** | `Compze.Tessaging`/`Compze.Tessaging.Hosting.Testing`, `Compze.Typermedia.Client`/`Compze.Typermedia.Hosting.Testing` | Each knows only itself |

The contracts define what hosting *is*: `IEndpointHost`, `IEndpoint`, `IEndpointBuilder`, `IEndpointComponent`,
plus the endpoint value types (`EndpointId`, `EndpointAddress`, `EndpointConfiguration`) and `IEndpointRegistry`.
The mechanism implements them without referencing any paradigm. The paradigms plug themselves in. Composition —
which paradigms an endpoint actually gets — happens at the outermost layer: the application or the test.

## Endpoints and hosts

An **endpoint** is one deployable unit: its own DI container, the message pipelines wired into it, and the
components that listen and send on its behalf. A **host** owns a set of endpoints and drives their shared
lifecycle:

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

`RegisterEndpoint`'s callback receives the endpoint's `IEndpointBuilder`. When it returns, the mechanism builds
the container and assembles the endpoint. The builder itself registers only what every endpoint needs regardless
of paradigm: the type mapper (pre-mapped with the shared message hierarchy and the discovery types), the
endpoint's identity, the configuration provider, and the infrastructure-query executor.

## How a paradigm plugs in: features

The builder does not know which paradigms exist. Each paradigm wires its pipeline in as a **feature** through
the one seam `IEndpointBuilder` offers:

- `GetOrAddFeature<TFeature>(createFeature)` — creates the feature once and remembers it. `AddTessaging()` and
  `AddTypermedia()` are one-line extension methods over this call, and `RegisterTessagingHandlers` /
  `RegisterTypermediaHandlers` are extension properties that add the feature on first touch and return its
  handler registrar — so registering a handler is all it takes to give an endpoint a paradigm.
- Inside its constructor, a feature uses the rest of the builder surface: it registers its services with
  `builder.Registrar`, maps its message types with `builder.TypeMapper`, schedules post-container-build work
  with `builder.OnContainerBuilt(...)` (e.g. registering discovery handlers with the resolved query executor),
  and adds its runtime lifecycle with `builder.AddComponent(...)`.

`TessagingEndpointFeature` (in `Compze.Tessaging`) wires the inbox, outbox, tommand scheduler, router, and
service bus session. `TypermediaEndpointFeature` (in `Compze.Typermedia.Client`) wires the handler registry and
executor, the in-process navigator, the transport server, and discovery. A feature lives in its paradigm's own
assembly, where the internal access it needs is natural — no `InternalsVisibleTo` back-doors.

## Components and the lifecycle phases

What a feature adds via `AddComponent` is an `IEndpointComponent`: the paradigm's runtime lifecycle. Components
are created when the endpoint starts listening — the container is built by then, so the factory can resolve
whatever it needs.

The lifecycle has two phases, and the ordering guarantee is **host-wide**: when a host starts, every endpoint's
components finish `StartListeningAsync` before any component anywhere starts `StartSendingAsync`. That is the
whole point of the split — nothing can send to an endpoint that is not yet ready to receive. Stopping runs in
reverse. The sending-phase members are default no-ops because some components only listen: Typermedia's
component starts and stops its transport server and never sends; Tessaging's component listens with its inbox
and scheduler, then in the sending phase connects its router to every address in the `IEndpointRegistry` and
starts its outbox.

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

`Compze.Hosting.Testing` repeats the same pattern one level up: `TestingEndpointHost` is paradigm-blind, and
paradigms plug into the *host* as `ITestingEndpointHostFeature`s:

```csharp
using var host = TestingEndpointHost.Create(new TessagingTestingEndpointHostFeature(),
                                            new TypermediaTestingEndpointHostFeature());
```

Every endpoint the host registers gets, before the test's own setup runs, each feature's standard test wiring —
so individual tests don't repeat it. There is no "combined host" type: a Tessaging-only, Typermedia-only, or
combined host is just `Create` with the matching features.

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

The shared HTTP plumbing (the `IHttpClientFactoryCE` and the infrastructure-query transport and controller that
endpoint discovery runs on) is paradigm-neutral, so both paradigm transports demand it through the guarded
`CurrentTestsInfrastructureTransportIfNotRegistered()` — whichever registers first wins, and an endpoint
hosting both paradigms gets it once.

## Adding a new paradigm

1. Define the pipeline's services and an `IEndpointComponent` for its runtime lifecycle.
2. Write an endpoint feature: a class whose constructor wires everything through the `IEndpointBuilder`
   surface, plus an `AddX()` extension method over `GetOrAddFeature` and (if it has handlers) a `RegisterXHandlers`
   extension property.
3. If the paradigm has an address, expose it as an extension property on `IEndpoint` reading your component.
4. For tests, write an `ITestingEndpointHostFeature` that registers the paradigm's test transport and calls
   `AddX()` for every endpoint — and, if the paradigm has background work, hold the tracker for it and
   implement the at-rest members.

Nothing in `Compze.Abstractions`, `Compze.Hosting`, or `Compze.Hosting.Testing` needs to change.
