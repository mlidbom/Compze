# Endpoint-declarations

This document explains how an endpoint is declared and built: the declaration class, the identity type, the
environment, and how a host puts them together. It is the companion to
[Compze hosting](../../Compze.Hosting/dev_docs/hosting.md), which explains what an endpoint and a
host *are* and how they run; this document explains how an endpoint comes to exist. The final section records
*why* the model has this shape — the decisions behind it.

## The three pieces

Creating a running endpoint always involves exactly three pieces:

| Piece | Type | Answers | Who writes it |
|---|---|---|---|
| The **declaration** | a class inheriting `ExactlyOnceEndpointDeclaration<TIdentity>` or `BestEffortEndpointDeclaration<TIdentity>` | what the endpoint IS: identity, handlers, topology stance | the application, once per endpoint |
| The **environment** | a class implementing `IEndpointEnvironment` | where it runs: transport, serializer, discovery, the actual domain database | the deployment, once per deployment |
| The **build** | `declaration.Build(containerBuilder, environment)` | a running-ready `IEndpoint` | the framework; a host calls it for you |

The same declaration builds in any environment: production, the test suite, and a same-machine process suite
all host the same endpoint by construction, because they all build the same declaration class.

```csharp
class AccountManagementEndpointDeclaration : ExactlyOnceEndpointDeclaration<AccountManagementEndpointDeclaration>, IEndpointIdentity
{
   public static string Name => "AccountManagement";
   public static EndpointId Id { get; } = new(Guid.Parse("..."));

   protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireMyDomainTypeMappings();

   protected override void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) => handle
      .ForTevent(async (IAccountTevent tevent, IUnitOfWorkResolver unitOfWork) => ...);
}

var host = EndpointHost.Production.Create(CreateContainerBuilder, new ProductionEnvironment());
var endpoint = host.RegisterEndpoint(new AccountManagementEndpointDeclaration());
```

A declaration is a blueprint, not the running endpoint: instantiable and inspectable with no container and no
lifecycle, and buildable any number of times — one declaration, several endpoint instances across restarts
and across hosts.

## Identity is a type: `IEndpointIdentity`

An endpoint's identity — its human-readable `Name` and its durable `EndpointId` — is carried by a type
implementing `IEndpointIdentity`, whose members are `static abstract`:

```csharp
public interface IEndpointIdentity
{
   static abstract string Name { get; }
   static abstract EndpointId Id { get; }
}
```

The declaration bases take the identity as the type parameter `TIdentity` and read `TIdentity.Name` /
`TIdentity.Id` in their constructor. The type parameter exists for exactly one reason: C# can only reach
`static abstract` interface members through a generic type parameter. **Usually the declaration is its own
identity** — `class X : ExactlyOnceEndpointDeclaration<X>, IEndpointIdentity` — and the generic argument is
simply the class naming itself.

What compiler-enforced identity buys:

- **One home.** Before, an endpoint's name and id were strings and `Guid`s repeated at every place that
  composed or referred to the endpoint, free to drift apart. Now every reference is a symbol:
  `RequiredPeers => [StatisticsEndpointDeclaration.Id]`, `answeredBy: EndpointHostProcessEndpointIdentity.Name`.
- **Renaming works.** A reference to an identity is a reference the tooling can find and rename.
- **A declaration class IS one endpoint.** Identity is type-level, so two instances of one declaration are
  two builds of the *same* endpoint — which is exactly how restart scenarios and the
  one-process-per-endpoint rule are specified: register the same declaration on two hosts and the second
  process is refused by the endpoint's process lock.

### Standalone identity types

When an identity must be known to parties that do not share the endpoint's declaration — most concretely,
across process boundaries — the identity becomes its own small class, and declarations reference it as their
`TIdentity` instead of themselves. The multi-process test suite is the working example
(`test/Compze.Tests.SameMachine.EndpointHostProcess`):

- `EndpointHostProcessEndpointIdentity` is shared by **two different declarations** — the endpoint host
  process hosts either an exactly-once or a database-less endpoint depending on which conversation a
  specification wants, but both build under the one identity.
- `SpecificationProcessEndpointIdentity` lets the endpoint host process declare
  `RequiredPeers => [SpecificationProcessEndpointIdentity.Id]` — requiring, by compiler-checked symbol, a
  peer that lives in another process.

This is also the shape a future system-wide contract of endpoint identities would take: identity types are
pure data with no behavior and no dependencies, so an assembly holding only identities references nothing.

### Endpoint names are identifier material (exactly-once tier)

An exactly-once endpoint's name prefixes its table-set in the domain database it joins
(`AccountManagement_InboxTessages`, …), so the name must be a letter followed by letters, digits, or
underscores, at most 28 characters. Violations fail loud when the declaration is built — pinned in
`Given_an_exactly_once_endpoint_declaration`.

## What a declaration declares

Declaring is overriding. Three groups of overrides, all with do-nothing defaults so a declaration overrides
only what its endpoint actually uses:

**Scalar aspects** — plain value overrides:

| Override | Meaning |
|---|---|
| `RequiredPeers` | peers this endpoint requires by id; everything published before a required peer's first contact is held for it |
| `PeersNotQueuedFor` | peers this endpoint deliberately keeps nothing for — the per-peer opt-down from queue-while-down |
| `HandlerAvailabilityPatience` | how long a send with no live route waits for one to appear before failing loud |

**Registration overrides** — one virtual method per tessage-handler kind, each receiving *the minimal
registrar for exactly that kind*: an interface exposing only the registrations that kind allows.

| Override | Registrar it receives | Tier |
|---|---|---|
| `RegisterExactlyOnceTommandHandlers` | `IExactlyOnceTommandHandlerRegistrar` | exactly-once only |
| `RegisterExactlyOnceTeventHandlers` | `IExactlyOnceTeventHandlerRegistrar` | exactly-once only |
| `RegisterBestEffortTeventHandlers` | `IBestEffortTeventHandlerRegistrar` | both |
| `RegisterTypermediaTommandHandlers` | `ITypermediaTommandHandlerRegistrar` | both |
| `RegisterTueryHandlers` | `ITueryHandlerRegistrar` | both |
| `ObserveTevents` | `ITeventObservationRegistrar` | both |
| `RegisterComponents` | `IComponentRegistrar` (the container itself) | both |

The tier split is the point: **the wiring rule is structural**. A best-effort endpoint wires no exactly-once
delivery machinery, so its declaration base simply *has no* exactly-once registration overrides — the mistake
of registering an exactly-once handler on a best-effort endpoint is mostly not expressible at all. What the
type system cannot express (a subscription's guarantee arrives via wrapper types no generic constraint can
see) is asserted at registration, and the build-time roster check remains the last line of defense.

**The general `Declare` override** — receives the endpoint's full `ExactlyOnceEndpointBuilder` /
`BestEffortEndpointBuilder`. This is the escape hatch for everything the specific overrides do not cover,
store integrations foremost: the tevent store's `RegisterTeventStore().HandleTaggregate<...>()` and the
document db's `RegisterDocumentDb().HandleDocumentType<...>()` are extension methods from their own packages
over the builder, and the declaration bases know nothing about them. The principle: the base class
coordinates and simplifies everything it *can* know about; the general override covers what it cannot.

## The environment: `IEndpointEnvironment`

Everything a deployment decides, and a declaration deliberately does not:

```csharp
public interface IEndpointEnvironment
{
   void Configure(EndpointBuilder endpointBuilder);
   void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder);
}
```

- `Configure` declares the transport protocol, the serializer, and discovery participation
  (`ParticipateIn` / `DiscoverEndpointsThrough` / `AnnounceAddressTo`) on the builder.
- `ConfigureDomainDatabase` binds an exactly-once endpoint to the actual domain database it joins — the
  engine and connection string, through a backend's public registrar extension (`MsSqlDomainDatabase(...)`,
  `SqliteDomainDatabase(...)`, …). It is only called when building the exactly-once tier: the best-effort
  tier persists nothing.

A host owns one environment as the default for every registration: production hosts take it at
`EndpointHost.Production.Create(containerFactory, environment)`; `TestingEndpointHost` builds its own (the
current test's transport, the pooled test database keyed by the endpoint's id, participation in the host's
own interprocess registry, the tessages-in-flight tracker).

### Per-registration environments and decoration

An endpoint whose environment differs from its co-hosted neighbors' registers with its own:
`RegisterEndpoint(declaration, environment)`. The host's default environment is public
(`EndpointHost.Environment`) precisely so such an environment can be written as a decorator — implement
`IEndpointEnvironment`, delegate to the wrapped environment, change or add the one thing that differs:

```csharp
//The testing host's environment, plus announcing to one more registry:
class EnvironmentAlsoAnnouncingTo(IEndpointEnvironment environment, IEndpointAddressAnnouncer additionalTarget) : IEndpointEnvironment
{
   public void Configure(EndpointBuilder endpointBuilder)
   {
      environment.Configure(endpointBuilder);
      endpointBuilder.AnnounceAddressTo(additionalTarget);
   }

   public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) => environment.ConfigureDomainDatabase(endpointBuilder);
}
```

The same shape swaps only the database binding to join several endpoints to one shared domain database
(`Given_two_exactly_once_endpoints_joined_to_one_domain_database`), or supplies a deliberately incomplete
environment to pin a missing-declaration failure (`Given_two_best_effort_endpoints`).

## The build

`declaration.Build(containerBuilder, environment)` runs one fixed template:

1. The tier's `EndpointBuilder` is created on the fresh container builder.
2. The environment configures: `Configure`, then — exactly-once tier — `ConfigureDomainDatabase`.
3. The declaration's scalar aspects and shared registration overrides apply.
4. The tier's own registration overrides apply.
5. The general `Declare` override runs, receiving the full builder.
6. The build closes: foundation asserts (transport, serializer, database) fail loud naming what is missing,
   the tier machinery and shared endpoint core register, the container builds, the type map composes, and
   the roster's soundness is asserted. The builder exists only inside the build; declaring afterward
   explodes.

A host's `RegisterEndpoint(declaration)` is exactly this, on a fresh container builder from the host's
factory, in the host's environment. Endpoints stay first-class — `Build` is public on the declaration faces
(`IExactlyOnceEndpointDeclaration` / `IBestEffortEndpointDeclaration`), so an endpoint needs no host.

## Why the model has this shape

**Why a class instead of a callback.** Endpoints used to be composed by passing name, id, and a wiring
callback to a `Build` method at every hosting site. Every site assembled the endpoint's wiring itself, so the
sites drifted: production declared `RequirePeers` while the tests worked around discovery timing instead;
production co-located two endpoints in one domain database while the tests gave each its own. What an
endpoint IS had no home — it was re-stated wherever the endpoint was hosted. A declaration class is that
home: production and tests register the same class, so they host the same endpoint *by construction*, and a
difference between them can only be environmental.

**Why identity is compiler-enforced.** Name/id pairs as literals drift, cannot be renamed by tooling, and
give a peer reference (`RequirePeers`) nothing to point at. A `static abstract` identity makes every
reference a symbol and makes "one declaration = one endpoint" a fact of the type system.

**Why the environment is a separate object.** Transport, serializer, discovery, and the concrete database
vary per deployment while the endpoint stays the same endpoint — the testing host is the proof, hosting every
production-shaped declaration in a completely different environment. Folding environment choices into the
declaration would re-create the per-site drift the declaration exists to end.

**Why minimal registrars per kind.** An override receiving a registrar that exposes only its kind's
registrations cannot register the wrong kind — guarantee-fit becomes structure where the type system reaches,
and one targeted assert where it does not. The concrete registrars (`TessageBusHandlerRegistrar`,
`TypermediaHandlerRegistrar`, `TeventObservationRegistrar`) implement the minimal interfaces as facets.

**Why the general `Declare` override exists.** The stores (tevent store, document db) are orthogonal
packages: no endpoint is obliged to use them, and the declaration bases must not reference them. Their
integrations arrive as extension methods over the builder, so the base cannot offer them a named override —
it offers the full builder once, last, instead.

**History.** This model replaced the callback surface in July 2026 (branch `refactor-endpoint-declaration`);
the callback statics and the builder's fluent self-typing generic died with it. The per-package changelogs
(`Compze.Tessaging`, `Compze.Hosting`, `Compze.Tessaging.Hosting.Testing`, 2026-07 sections) record the
deltas.
