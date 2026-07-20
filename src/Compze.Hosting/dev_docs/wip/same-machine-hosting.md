# Same-machine hosting

This document takes a developer who is new to Compze from zero to understanding how multiple processes on one
machine form one application — how their endpoints find each other with zero configuration and converse with
no web stack and no database server. It is the companion to [the hosting model](../hosting-model.md), which
explains what an endpoint and a host *are*, and to
[the tevent delivery model](../../../Compze.Tessaging/dev_docs/tevent-delivery-model.md), which explains the
guarantees that hold once the conversation flows. This document explains how the conversation reaches across
process boundaries on one machine.

## The big picture

### The scenario

An application suite: several OS processes on one machine — say a desktop application, a background worker,
and a tray helper — each hosting Compze endpoints and conversing through them. Every piece of machinery a
network deployment justifies is dead weight here: a web server stack loaded into every process, port
assignments to manage, a configuration file listing who listens where, a database server to install and run.
Same-machine hosting removes all of it. What the application writes is exactly what the hosting model already
requires — endpoints, features, handlers — plus one line naming the registry the suite shares.

### The one design rule

Everything below follows from a single rule:

> **Addresses are ephemeral and never configured. An endpoint generates a fresh address every time it starts,
> announces it only while actually listening, and senders continuously converge on what the registry says
> right now.**

Unpacked:

- **A fresh address per start.** No address is worth writing down — not in a configuration file, not in
  another endpoint's setup. Whoever needs the address reads it from the registry at the moment of use.
- **Announced means listening.** Announcement is its own phase of the endpoint's lifecycle, after its
  listening phase and before its sending phase — so an announced address is always one that is actually
  listening. Retraction is the mirror phase, before the endpoint's sending stops. Addresses announced by a
  process that crashed are invisible, because the registry checks announcer liveness.
- **Senders converge; nothing is wired once.** Connections are not established at startup and assumed
  thereafter — they are *reconciled* against the registry's current membership, continuously. Endpoints that
  appear, disappear, or restart at a new address are connected, dropped, or re-connected as the registry
  changes.

One distinction carries the whole model: **an address identifies an endpoint *instance*; the `EndpointId`
identifies the endpoint**. The id is stable across restarts; the address never is. Discovery maps the stable
identity to the current instance's address, freshly, all the time.

Three pieces implement the rule, and the rest of this document walks them top-down: the **named-pipe
transport** (the wire), the **interprocess registry** (discovery), and the **router's reconciliation**
(dynamic topology) — then the composition that puts them together, and the specification that proves it
across real OS processes.

## The named-pipe transport

The HTTP transport exists for the general case: endpoints on different machines, standard middleboxes,
external clients. Same-machine conversations need none of that, and paying for it hurts exactly where an
application suite is sensitive — an ASP.NET Core/Kestrel server in every process means dependency weight,
per-process working set, and startup time. The named-pipe transport (`Compze.Tessaging.Internals.Transport.NamedPipes`)
replaces it with `System.IO.Pipes` from the base runtime: no web stack at all, and cross-platform — on
Linux/macOS the same API rides Unix domain sockets.

- **Addressing** (`NamedPipeAddress`): `compze.pipe://localhost/<pipe-name>`. A pipe name is generated fresh
  per server start (`NewUniquePipeName`) — the same-machine analog of the HTTP transport's dynamically
  allocated ports, and the "fresh address per start" the design rule demands.
- **The shape**: the endpoint runs **one transport server** (`NamedPipeEndpointTransportServer`, on this
  transport) — listening on one freshly named pipe, dispatching each request to the handler registered for
  its request kind. Each communication style the endpoint speaks *contributes* its request handlers to that
  server, and the server itself answers endpoint-discovery queries — every
  endpoint serves discovery no matter what it speaks. One server means one address per endpoint, which is
  what lets the registry map an endpoint to a single address. (The ASP.NET Core transport has the same shape:
  one Kestrel server per endpoint, serving the very same contributed request handlers.)
- **Framing**: length-prefixed UTF-8 strings; each connection is a lockstep request/response conversation.
- **Concurrency**: a fixed pool of listener loops (`max(2 × processor count, 8)`), each serving its accepted
  connection to completion before accepting the next. The pool bounds concurrent handler executions, and a
  connection burst beyond it queues in the clients' pending connects — the transport's natural backpressure.
- **Security**: pipes are created `CurrentUserOnly` — only processes running as the same user can connect.
- **Failures**: a handler exception crosses the pipe as an error response and rethrows in the client, exactly
  as the HTTP transport's controllers behave.

Choosing the transport is composition, per the hosting model's design rule — nothing in the hosting machinery
knows which transport an endpoint speaks. An endpoint setup declares its protocol exactly once:
`NamedPipeEndpointTransport()` where it would have declared `AspNetCoreEndpointTransport()` — registering the
endpoint transport client, the endpoint-discovery query transport that runs on it, and the endpoint's one
transport server. The communication styles register nothing protocol-specific: each feature registers its own
request-handler contribution and client, and both work over whichever protocol the endpoint declared. In
tests the choice is the `Transport` axis of the pluggable-component matrix: the same specifications run over
ASP.NET Core and over named pipes.

## Discovery: announcing into the interprocess registry

Discovery is two contracts in `Compze.Abstractions`, one per direction:

- **`IEndpointAddressAnnouncer`** — the write side: an endpoint announces where it listens, and retracts when
  it stops.
- **`IEndpointRegistry`** — the read side: senders read the announced addresses.

`InterprocessEndpointRegistry` (`Compze.Hosting.SameMachine`) implements both faces over one shared store: an
`IInterprocessObject<T>` — a memory-mapped file plus cross-process synchronization — so announcements are
immediately visible to every process that opens the registry, with no server and no configuration, and
survive process restarts. The registry's name and directory (`OpenOrCreateSessionLocal(registryName,
directory)`) ARE the application-suite boundary: processes that should discover each other's endpoints open
the same registry; unrelated applications use their own.

An endpoint declares who it announces to in its composition:

```csharp
endpoint.AnnounceAddressTo(registry);
```

Declaring none — a deployment whose endpoints are found through a fixed address list — means nothing is
announced. The announced address is the endpoint's one transport-server address, serving every distributed
capability the endpoint speaks. The announcement is made in the endpoint's announcing phase — after its
listening phase and before its sending phase — and retracted in the mirror phase before its sending stops,
so an announced address is always one that is actually listening.

**Crashed processes announce nothing — structurally.** The backing file outlives crashed processes by design
(that is how announcements survive restarts), so a crash cannot retract. Instead, every entry records the
`ProcessIdentity` of its announcing process — the process id *plus the process's start time*, because the OS
recycles process ids and only the pair identifies a process uniquely. `ProcessIdentity` compares start times
with a tolerance rather than for exact equality: `Process.StartTime` is not read-stable across processes on
every OS (on Unix each reader reconstructs it from its own coarse `now - uptime` boot-time sample), so exact
tick-equality would wrongly declare a live announcer dead when a *different* process reads its entry — which is
exactly what breaks same-machine discovery on Linux. Addresses whose announcing process is no longer running are
invisible to readers, and every announcement prunes them from the file — announcement is the registry's
self-cleaning moment. A crashed process's addresses are never routed to and never accumulate.

## Dynamic topology: the router reconciles

The sending phase does not connect the endpoint's router to a fixed address list and assume it thereafter.
It sets the one `TessagingRouter` *reconciling*: converge on the registry's membership now, then
keep converging. Between passes the router waits on the registry's change signal
(`IEndpointRegistry.AwaitPossibleMembershipChange`): the interprocess registry raises its backing
interprocess object's cross-process signal on every announcement and retraction, so a topology change wakes
every waiting router on the machine and propagates at signal latency — tens of milliseconds at most — rather
than at a polling interval. The wait also times out once a second, because two things no signal can carry
still need a periodic pass: a crashed process's addresses disappearing (a crash signals nothing — the
liveness filter just stops listing them), and retrying connections that failed. Each pass compares the
connected addresses with the registry's current addresses — minus the endpoint's own announced address:
routes lead only to *other* endpoints, since the roster serves in-roster tommands inline and the endpoint's
own tevent subscriptions by in-boundary participation:

- **An endpoint appears** → connect. The new connection learns the remote endpoint's identity and its one
  advertisement through the endpoint-discovery query, and routes are registered for every tessage kind it
  advertises: tevent subscriptions by type-assignability, tommand, typermedia-tommand, and tuery routes by
  exact type.
- **An endpoint's address leaves the registry** (it stopped and retracted, or crashed and was pruned by
  liveness) → the connection is dropped and its routes with it. Undelivered exactly-once tessages for it stay
  in the outbox's storage — exactly-once means they wait for the endpoint's return.
- **An endpoint returns at a new address** — addresses are per-instance; identity is the `EndpointId` — → the
  old connection is replaced by the new one. On the Tessaging side the endpoint's undelivered backlog loads
  into the new connection in send order (see the ordering guarantee in
  [the tevent delivery model](../../../Compze.Tessaging/dev_docs/tevent-delivery-model.md#ordering)) — the backlog
  follows the endpoint; on the Typermedia side the routes simply point at the new address.
- **A listed address does not answer** → topology churn, not a bug: the process may still be starting, or may
  have crashed a moment before the liveness filter would prune it. The failure is logged and the address
  retried on the next pass.

A dynamic topology implies contracts callers must know (the full story is
[the peer model](../../../Compze.Tessaging/dev_docs/peer-model.md)):

- **Subscribers join from now on.** A tevent published before an endpoint was first discovered is not
  retroactively delivered to it — exactly like a subscriber that did not exist yet. The exception is a peer
  the composition declares with `RequirePeers`: everything published before its first advertisement is held
  for it and delivered on first contact — which is what makes a multi-process suite's startup a non-event.
- **Tommand sends and typermedia navigations wait out discovery.** A send whose type has no live,
  unambiguous route right now waits — bounded by the endpoint's handler-availability patience (a flat 30
  seconds unless declared otherwise) — for the route to appear: a first contact, or a known peer's return.
  Only exhausted patience fails loud, telling known-but-down (naming the remembered peer) from never-seen
  (naming the probable deployment error). The startup discovery race is absorbed by the machinery, not by
  caller retry loops; an application that wants the wait paid at startup instead of by its first caller
  awaits `IEndpoint.AwaitReadinessAsync` before opening traffic.

Reconciliation is not same-machine-specific: the router converges on whatever `IEndpointRegistry` it is
given — a fixed address list converges once and stays; the interprocess registry is what makes membership
*live*. The testing host runs every test's endpoints on a real interprocess registry of its own
(`TestingEndpointHost.EndpointRegistry`), so every test exercises this same announce/discover pipeline.

## The whole composition

The endpoint host process the multi-process specification spawns
(`test/Compze.Tests.SameMachine.EndpointHostProcess`) is the reference composition — a production host, one
endpoint, named pipes, discovery, and sqlite database *files* standing in for the database server
(exactly-once delivery needs the outbox/inbox store; sqlite files provide it with nothing to install).
Trimmed to its shape:

```csharp
using var registry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal("MySuite.EndpointRegistry", dataDirectory);
var host = EndpointHost.Production.Create(() => new MicrosoftContainerBuilder(new ComponentRegistrar()));

host.RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(
   container, "BackgroundWorker", new EndpointId(Guid.Parse("...")), endpoint =>
{
   endpoint.RegisterComponents(registrar => registrar.RequireMappedTypesFromAssemblyContaining<MyTommand>());
   endpoint.NamedPipeEndpointTransport();
   endpoint.NewtonsoftSerializer();
   endpoint.SqliteDomainDatabase("BackgroundWorker");
   endpoint.ParticipateIn(registry);   // discover the others through it AND announce ourselves to it

   endpoint.RegisterTessageBusHandlers(handle => handle.ForTommand(async (MyTommand tommand, IUnitOfWorkResolver unitOfWork) => ...));
}));

await host.StartAsync();
```

The database declaration registers the whole engine pairing — the connection pool, the type-id interner, and
Tessaging's sqlite inbox/outbox sql layers — through one named declaration on the exactly-once tier's
declaration surface.

`ParticipateIn` declares the registry's two faces at once: `DiscoverEndpointsThrough`, the *read* side the
router reconciles against, and `AnnounceAddressTo`, the *write* side the endpoint's lifecycle drives — declare
the sides separately when a deployment is asymmetric. No address, port, or connection string appears anywhere — the
pipe names are generated, the announcements distribute them, and the connection strings resolve to sqlite
files in the process's data directory. No schema setup appears either: each sql layer contributes its
own schema-creation SQL as part of registering itself, and all of it runs as one batch before the database's
first use.

## Proven across real process boundaries

`Given_a_separate_process_hosting_an_endpoint_discovered_through_a_shared_interprocess_registry` (in
`Compze.Tests.Integration`, on the named-pipes leg of the transport matrix) runs the story end to end with a
REAL process boundary: it spawns the endpoint host process above as a separate OS process, both processes
open the same registry, each discovers the other through it, and an exactly-once tommand conversation crosses
in both directions — the specification sends a tommand, the other process handles it and sends a reply
tommand back. The send waits out the reconciliation loop's discovery of the still-starting process — the
waiting send described above, exercised for real.

## Implementation status

As of 2026-07-15, everything this document describes is built and verified:

**Built and verified:**

- The named-pipe transport for both communication styles, run against the full specification suite as a leg
  of the transport matrix.
- One transport server — one address — per endpoint, on both transports: every distributed capability the
  endpoint speaks is served through it, and it is the address that gets announced.
- The `InterprocessEndpointRegistry` with announce/retract and crashed-process liveness.
- The Tessaging router's continuous reconciliation — appear, disappear, and restart-at-a-new-address,
  including the backlog following a restarted endpoint.
- The multi-process specifications and their endpoint host process — also the first production-hosting
  composition exercised end to end.
- The no-SQL same-machine suite (2026-07-15): the endpoint host process hosting a best-effort endpoint, and
  a best-effort tevent conversation
  crossing real process boundaries in both directions with no database anywhere in either process — see
  [the tevent delivery model](../../../Compze.Tessaging/dev_docs/tevent-delivery-model.md).
- Typermedia over the same dynamic topology (2026-07-15): an endpoint that declares the registry it
  discovers through (`DiscoverEndpointsThrough`/`ParticipateIn`) routes typermedia tessages through its one
  reconciling router, and navigates other endpoints'
  typermedia through its own `IRemoteTypermediaNavigator` — proven across real OS processes with no database
  in either process: the specification's endpoint discovers the endpoint host process through the shared
  registry and its tuery is answered there. An endpoint that declares no registry only serves; navigating
  from it fails loud naming the missing declaration (an external client — the pure client — connects to an
  explicitly known address instead).
