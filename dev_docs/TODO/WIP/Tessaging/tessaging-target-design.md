# The Tessaging target design

**Status: the imagined target design, described straight up — what the system is, not how the codebase gets
there.** The rationale and the path live in
[style-substrate-and-hosting-evaluation.md](style-substrate-and-hosting-evaluation.md); this document never
compares or narrates. Settled decisions are Magnus's (⚖); what is deliberately left open is listed at the
end. Working name flagged inline: `LocalTessagingEngine` is not final. API sketches are illustrative, not
final signatures.

## Tessaging and its two siblings

**Tessaging** is the common paradigm: conversation through **tessages** — messages routed by their .NET
type. A tessage's type is its whole contract: its kind, its delivery guarantee, its transactionality, its
remotability, and its synchrony are all declared by the interfaces the type extends, and the machinery that
carries a tessage is chosen by reading its type.

Two siblings are built on the paradigm:

- **Typermedia** — the request/response style: hypermedia re-founded on types. A caller executes
  **tueries** and **typermedia tommands** and follows the typed results to the next step. Ask now, get an
  answer now.
- **TessageBus** — the asynchronous style: the message bus re-founded on tessages. Handlers publish
  **tevents** (multi-subscriber facts, routed by type compatibility) and send **tommands** (single-handler
  instructions). Tell the world what happened — or direct one receiver — and trust it arrives.

The delivery-guarantee ladder — best-effort, at-most-once, exactly-once — is declared in the shared type
hierarchy and spans both siblings: a typermedia tommand is at-most-once; a TessageBus tommand is
exactly-once; a tevent's type declares whether it is best-effort or exactly-once.

## ⚖ The consistency law

**The consistency boundary is the endpoint in space and the execution in time: after each completed
execution, the endpoint is consistent. Not eventually consistent — consistent.**

The law is definitional and universal. It is not a mode, not an endpoint type, and has no opt-out: every
endpoint, of every tier, obeys it, which is what lets a reader determine the consistency model of any
system by reading its topology and nothing else.

What the law means concretely:

- **Consistency is decided by one thing only: which side of the endpoint boundary a handler lives on.**
  A tessage's kind decides the *shape* of the conversation — a fact fans out, an instruction has one
  receiver, an ask returns a value. The handler's *home* decides the when and the atomicity.
- **In-boundary is immediate and transactional.** Every handler in the endpoint's roster that responds to
  something raised within an execution runs *inside* that execution: tevent handlers participate in the
  publisher's transaction; a tommand whose handler is in the roster executes inline, in the sender's
  execution. The endpoint's entire state — domain consequences, query models, tessaging bookkeeping —
  commits atomically, once, per execution. In-boundary failure fails the execution: everything rolls back
  together, which is precisely what the law demands.
- **Cross-boundary is eventual, carried by the guarantee the tessage's type declares** and the delivery
  machinery the receiving tier provides. Cross-boundary failure retries at the receiver; the sender's
  execution is unaffected.
- **The delivery-guarantee ladder is the orthogonal axis.** The tier (best-effort vs exactly-once) governs
  what crossing the boundary guarantees; the law governs what happens within. A best-effort endpoint with
  no durable state satisfies the law trivially — inline handling *is* its consistency.

Because the roster is fixed per endpoint instance, a given tommand type is *always*-inline or
*never*-inline for that instance — there is no per-send lottery. Moving a handler across an endpoint
boundary is therefore the framework's one consistency lever, and it is deliberately visible: it is a real
consistency refactoring, identical in meaning for tevents and tommands. `IStrictlyLocalTessage` kinds are
the *pinned* ones — the type-level promise that their handlers may never leave the boundary at all — where
an ordinary tommand is location-flexible and inherits whatever consistency its current home implies.

**The price tag, stated up front:** an execution is one transaction, so everything in-boundary sums — every
participating handler adds latency and lock-hold time, and locks are held across the awaits of async
handlers. The boundary is a performance boundary too. The sanctioned responses all live inside the model,
at per-relationship granularity, each visible in structure:

1. **Split the endpoint** — buy an eventual-consistency seam, deliberately, exactly where scale demands
   it. Endpoints are cheap by design (see "Domains, endpoints, processes").
2. **Observe instead of participate** — for reactions the endpoint's consistency does not depend on.
3. **Defer across the seam** — send a tommand to another endpoint (including a purpose-built worker
   endpoint) when the work is genuinely new work.

**The law's limit, stated plainly:** it is a *handling* boundary, not a data prison. The framework enforces
it absolutely over its own machinery — in-boundary there is no eventual path at all, and every endpoint's
tessaging state is segregated — but domain data is the domain designers' business, and nothing stops domain
code in one endpoint from touching tables another endpoint considers its own. The boundary has the same
status as an aggregate boundary in DDD: a discipline the framework supports structurally and cannot police.

## The LocalTessagingEngine

*(working name — the concept is settled, the final name is not)*

The **LocalTessagingEngine** is the tessage-conversing heart of one container — one set of wiring. It is
what every Tessaging application composes, and for many applications it is all of Tessaging they ever
compose: tevents restructuring the internal flow of one process, local navigation structuring its request
handling, all of it on the caller's execution, in the caller's execution context.

- ⚖ **Exactly one per container.** Composing a second explodes at build time.
- ⚖ **It is not an endpoint.** It has no identity, no address, no peers, no advertisement. An endpoint is
  an engine plus identity and a wire (see "Endpoints" below).
- **It has no lifecycle phases.** Its only background machinery is observation dispatch — created with it,
  drained at its disposal; everything else it does happens on its callers' executions.

The engine owns four things:

1. **The roster** — the closed set of what this component understands.
2. **The executor** — the one implementation of handler execution.
3. **The doors** — the publishers and navigators application code injects.
4. **Tevent observation** — the transaction-ignoring watch surface.

### The roster

The `TessageHandlerRoster` is an immutable map from tessage type to handler, produced when the engine is
built and never modified afterward:

- **Tevent handlers** are multi-subscriber: a published tevent reaches every handler whose subscribed type
  it is compatible with, through the type hierarchy.
- **Tueries and tommands** (of either sibling) have exactly one handler each; declaring a second handler
  for the same type explodes at build.
- The roster is the single source of truth for the engine's **advertisement**: the remotable types it
  serves, as canonical type-id strings, computed once. An engine's advertisement can only change by
  building a new engine — which is what a process restart does.

### The executor

One choreography executes **the roster's full response** to a tessage in one execution context:

- a **tevent**'s response is *every* compatible handler, all within the one unit of work — the tessage is
  the unit of handling, so its handlers commit or retry as a whole; partial handling is unrepresentable;
- a **tommand** handler runs in a unit of work — it receives an `IUnitOfWorkResolver`;
- a **tuery** handler runs in a scope — it receives an `IScopeResolver`.

Every arrival path calls this one executor: the local doors (including a tommand send whose handler is in
the roster — the inline path the consistency law mandates), an endpoint's inbox, an endpoint's transport
server. There is exactly one implementation of "run this roster's response correctly", and therefore
exactly one policy for its failures: a tessage of a single-handler kind that reaches an engine with no
handler for it raises the no-handler exception family — which, per
[readiness-and-waiting-sends.md](readiness-and-waiting-sends.md), is exclusively the patience-exhausted
failure.

### The doors

Application code never touches the engine directly; it injects the doors:

- `IUnitOfWorkTeventPublisher` / `IIndependentTeventPublisher` — publishing tevents from inside a unit of
  work, or from code that must not be inside one.
- `ILocalTypermediaNavigatorSession` / `IIndependentLocalTypermediaNavigator` — executing strictly-local
  tueries and tommands in the caller's session, or independently.

The UnitOfWork/Independent axis is the
[unit-of-work model](../../../../src/Compze.DependencyInjection/dev_docs/unit-of-work-model.md): the prefix
names the weakest context the whole surface requires, and Independent doors assert no ambient transaction.

Every published tevent gets in-boundary participation — delivery to this engine's subscribed handlers,
inside the publisher's execution, per the consistency law. Whether a tevent *also* travels further is a
property of its type, honored by whatever delivery machinery the composition possesses; the publisher door
is the same either way.

### Tevent observation

Observation is the deliberately transaction-ignoring watch surface — and it is the one in-boundary surface
*explicitly outside the consistency model*: watching, never participating.

- **Observers observe committed facts only.** A tevent published within an execution is queued for its
  observers at commit and dispatched after — an uncommitted tevent is not yet a fact, and an observer must
  never watch an execution that never happened.
- **Observation runs off-thread**, isolated from the caller in both directions: an observer's failure never
  fails the caller, and an observer's latency never blocks the caller. Dispatch is per-observer FIFO, so an
  observing read model still sees order.
- **Observer failures are never swallowed**: they route to the background-exception reporter — production
  logs loud, and the testing host's disposal rethrows them. The testing host's at-rest wait covers the
  observation queues, so a test cannot pass with observation work in flight.

⚖ Observation is declared through the same builder as everything else, under its own verb, so the distinct
semantics are visible at the declaration site.

## Building an engine

The engine is declared through its **builder** in one visible block, and the declaration is the one and
only way anything gets into the engine.

⚖ **Every declaration surface follows one idiom**: a builder method takes an `Action` over a short-lived
registrar and returns the builder —

```csharp
LocalTessagingEngineBuilder RegisterTessageHandlers(Action<TessageHandlerRegistrar> register);
```

The registrar exists only inside the callback. Nothing can hold a registrar and mutate the engine later:
the callback's end is the registration's end, the build closes the roster, and any attempt to register
afterward explodes. Encapsulation and chainability come from the same shape.

One handler registrar covers all four message kinds — the tessage's own type carries its kind, guarantee,
and synchrony, so the verbs differ only by handler shape:

```csharp
container.Registrar.LocalTessagingEngine(engine => engine
   .MapTypes(mapper => mapper.MapTypesFromAssemblyContaining<IAccountTevent>())
   .RegisterTessageHandlers(handle => handle
      .ForTevent(async (IAccountTevent tevent, IUnitOfWorkResolver unitOfWork) => ...)   //exactly-once kind: async handler
      .ForTuery((AccountTuery tuery, IScopeResolver scope) => ...)                       //strictly-local kind: sync stays first-class
      .ForTommand(async (DeactivateAccount tommand, IUnitOfWorkResolver unitOfWork) => ...)
      .ForTommand((Register tommand, IUnitOfWorkResolver unitOfWork) => new RegistrationResult(...)))
   .ObserveTevents(observe => observe
      .ForTevent((IAccountTevent tevent) => ...)));
```

Type mappings are declared on the same builder — they serve persistent stores and, when an endpoint wraps
the engine, the wire. Store integrations (a tevent store's `HandleTaggregate`, a document db's
`HandleDocumentType`) plug into this same declaration surface: they are handler contributors like any
other, arriving before the build like everything else.

The same declaration block is the surface *everywhere* — a plain container, a best-effort endpoint, an
exactly-once endpoint — so an application's handler registrations run unchanged under any composition.

## Synchrony follows the type

Synchrony is part of the tessage's contract, declared like everything else by its type:

- **Exactly-once kinds are async end to end.** Their doors are async — publishing an exactly-once tevent
  or sending a tommand writes durable rows inside the caller's transaction, which is database I/O — and
  their handlers are async-only: an exactly-once handler transactionally modifies a database by
  construction, and that does not happen synchronously in 2026.
- **Strictly-local kinds keep sync as first-class** — the memory-bound, in-caller's-transaction case — with
  async available for handlers that read actual stores.
- **Remote typermedia is async-first** on the wire and at its navigator surface.
- **The administration surfaces are async** — readiness, decommission, quiescence are awaitables by nature.

The unit-of-work choreography is async-flow-safe: the ambient transaction flows across awaits. The
consequence the consistency law makes explicit — locks held across the awaits of in-boundary handlers — is
part of the law's price tag, answered by its three escapes, never by weakening a handler's transactionality.

## Domains, endpoints, processes

Three concepts, deliberately orthogonal:

- **The domain (or subdomain)** owns the database and the model. **A database belongs to a domain — never
  to an endpoint.** The domain's aggregates, query models, and stores live in it, shared among the domain's
  endpoints as the domain designers see fit — domain data is not Tessaging's business.
- **The endpoint** is a consistency-and-handling unit *within* a domain: its own container, engine, roster,
  identity, address, and segregated tessaging state. An endpoint is a tiny microservice inside a
  potentially huge domain, knowing almost nothing of the rest of it — it declares its own type mappings and
  its own serializer, which is what makes extracting it to its own process a deployment act rather than an
  untangling. **Any number of endpoints live in one domain, and adding one is a handful of lines of code.**
- **The process** is pure deployment. A domain's endpoints may span processes; one process may host
  endpoints of several domains; the choice is free — except that ⚖ **an endpoint runs in exactly one
  process at a time**. Two processes claiming the same `EndpointId` is a misconfiguration that fails loud
  at startup (the endpoint catalog's process lease, below); tolerating it — failover, fault tolerance — is
  its own future design effort.

A **host** is an optional convenience owning several endpoints' lifecycles in one process, driving their
phases together. Endpoints are first-class; a host adds nothing an endpoint cannot do alone, and co-hosted
endpoints converse exactly as separated ones do.

## Tessaging storage within the domain database

The exactly-once machinery's atomicity *is* its co-location with domain data: the outbox row commits inside
the sending execution's transaction, and the inbox's handled-bookkeeping commits inside the receiving
execution's transaction. Every endpoint's tessaging state therefore lives in the **same database as the
domain data its executions touch** — segregated per endpoint:

- **Each endpoint owns a prefixed table-set** — a logical schema (its inbox, outbox, peer registry, and
  kin, under the endpoint's name as prefix). Creating an endpoint needs nothing beyond the `CREATE TABLE`
  rights the framework's startup schema-creation already uses, on every backend identically — which is what
  keeps endpoint creation a handful of lines with zero operational ceremony. Decommissioning an endpoint's
  storage is dropping its set.
- **Endpoint names are identifier material**: sanitized, length-capped, and unique per domain database —
  asserted loud at startup.
- **One shared table per domain database: the endpoint catalog** — each endpoint's name, `EndpointId`,
  creation time, and process lease. The catalog is domain-level data *about* endpoints, inherently shared;
  it enforces name uniqueness and the one-process-per-endpoint rule, and it tells administration which
  endpoints inhabit the database.
- **One type-id interner per domain database**, shared by its endpoints — co-located endpoints agree on
  interned ids by construction.
- A best-effort endpoint has no database and none of the above; the law and the boundary hold for it
  unchanged.

On SQLite, a domain database is one single-writer file: co-located busy endpoints share its write gate.
That is the price of the domain being one database, accepted for SQLite's role; the heavier backends have
no equivalent coupling beyond the shared commit log every one-database domain implies.

## Endpoints

An **endpoint** is an engine given identity and a wire: an `EndpointId` (stable identity across restarts),
one transport server with one address, discovery and announcement, the router, peer memory, and the
TessageBus delivery machinery of its tier. There are exactly two endpoint types, and they differ **only in
their TessageBus rung** — what crossing the boundary guarantees:

- **The best-effort endpoint.** No database. Process-lifetime peer memory; per-peer best-effort tevent
  queues that outlive connections (queue-while-down, `RequirePeers` pens for peers that must be met before
  anything is dropped, a bound per peer, `DoNotQueueTeventsFor` opt-down); arriving tessages dispatch
  through the engine's executor.
- **The exactly-once endpoint.** Everything above, plus the durable vertical in its domain's database:
  the inbox (receiver dedup, transactional retry), the outbox (durable rows, recovery backlog, per-peer
  exactly-once in-order delivery streams), durable peer memory, and the tommand-sending doors —
  `IUnitOfWorkTommandSender` / `IIndependentTommandSender`.

Both endpoint types serve **all four message kinds, unconditionally**. What an endpoint understands is its
roster; what it tells peers is its advertisement; which machinery carries a given tessage is decided by the
tessage's type *and the consistency law*: a send whose handler is in the roster executes inline, in the
sender's execution — exactly-once by construction, no delivery machinery involved — and a send whose
handler lives elsewhere crosses the boundary through the tier's machinery. Typermedia behaves identically
on both tiers — request/response neither queues nor persists. A send whose guarantee the composition cannot
honor fails loud, never silently downgrades.

An endpoint also extends the door set with the remote-facing doors: `IRemoteTypermediaNavigator` for
navigating other endpoints' typermedia, and (on the exactly-once tier) the tommand senders.

The third composed shape is the **pure client**: a navigator and a transport client with no server — an
external application navigating an endpoint's typermedia at a known address.

### One router, one advertisement

Each endpoint runs one router. Connections are keyed by `EndpointId`; routes are derived from peers'
advertisements — tevent subscriptions by type-assignability, single-handler kinds by exact type — with one
route policy across all kinds: several remembered handlers for a single-handler type is a diagnosable
send-time condition whose remedy is decommission, never a crash and never a silent pick.

Discovery is one question: every endpoint serves the endpoint-information query over its one transport
server, answering with its name, its `EndpointId`, and its advertisement. One advertisement describes
everything the endpoint serves, for every message kind.

Routes lead only to *other* endpoints. Nothing self-addressed ever crosses the wire: the roster serves
tommands inline and tevents by in-boundary participation, so the router maintains no connection to self.

### Peer memory and administration

An endpoint remembers every peer it has ever met — identity and last advertisement — with durability
following the tier: process-lifetime on the best-effort endpoint, database-backed on the exactly-once
endpoint. Remembered peers are what fan-out, receiver binding, queue-while-down, and waiting are computed
against; a peer's advertisement shrinking prunes what is owed to it, loudly.

Administration is a first-class production surface:

- **Decommission** (`IPeerAdministration`): retire a departed peer deliberately — discard what is held for
  it, resolve handler ambiguities it left behind, report what was discarded.
- **Readiness**: awaitable "handlers for these types are available" — safe to start taking traffic
  ([readiness-and-waiting-sends.md](readiness-and-waiting-sends.md)).
- **Quiescence**: awaitable "no volatile work is in flight across the suite" — safe to stop
  (sketched in the evaluation document; designed in its own effort).

Readiness and quiescence are the two faces of production administration: safe to start, safe to stop.

## What is parameterized

Composition choices are parameters and strategies, not plugins:

- **The tier** — best-effort or exactly-once endpoint (or no endpoint at all: just the engine).
- **The transport protocol** — named pipes or ASP.NET Core; a strategy behind the one transport server and
  client.
- **The domain database an exactly-once endpoint joins** — which engine backs it; typed so the pairing is
  routed by the compiler.
- **The serializer** — one parameter, per endpoint.
- **Topology declarations** — `ParticipateIn(registry)` / `DiscoverEndpointsThrough` / `AnnounceAddressTo`;
  `RequirePeers`; `DoNotQueueTeventsFor`.
- **The DI container** — pluggable underneath everything, orthogonal to all of the above.

## Lifecycle and topology

Each endpoint drives its own phases: listen → announce → send on the way up, retract → stop sending → stop
listening on the way down. An announced address is always one that is actually listening, in every process
topology, because the ordering is per-endpoint.

**Addresses come in two deployment strategies; identity is the `EndpointId` in both:**

- **Generated** — the same-machine deployment: a fresh address per start (a new pipe name, a dynamic
  port), distributed by the interprocess registry, zero configuration. The address is the instance;
  restarts move it; discovery follows.
- **Configured** — the network deployment: the endpoint listens where it is told — a stable hostname and
  port, DNS, a load balancer — and discovery is a fixed address list or whatever registry the environment
  provides. No firewall rule ever chases a random port.

Topology is continuous convergence either way: every router continuously reconciles its connections
against what its registry says now, so endpoints appearing, disappearing, and restarting are followed — at
signal latency where the registry signals, at the reconcile interval where it cannot. A fixed address list
converges once and stays.

Startup order is nobody's problem: readiness fronts the wait where an application wants it paid, waiting
sends absorb the churn windows, queue-while-down and `RequirePeers` hold one-way tessages for peers not yet
met. An endpoint behaves identically whether its peers share its process, its machine, or neither.

## Testing

Tests host real endpoints with the real pipelines: the production announce/discover registry, real
transports, real (throwaway, pooled) databases — no mocks. Per-tier test wiring hands each endpoint its
test concerns (the tessages-in-flight tracker, the pooled database, the matrix-selected transport and
serializer) at construction. A test cannot pass while work is silently in flight: disposal awaits the
tracker's at-rest — which covers observation queues — and rethrows background exceptions no assertion
observed. The tracker is a testing device; the production-honest await is quiescence.

## Deliberately unsettled

- **The engine's final name.** `LocalTessagingEngine` is the working name (truthful; does not roll off the
  tongue).
- **The project/package layout** — the set of remaining projects and their names (options in the
  evaluation document, question 9).
- **The exact table-prefix convention** for the per-endpoint table-sets — an implementation-time decision
  inside the settled storage model.

## Related documents

- [style-substrate-and-hosting-evaluation.md](style-substrate-and-hosting-evaluation.md) — the rationale,
  the evidence, and how the codebase gets here.
- [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md) — readiness and waiting sends, design
  settled.
- [durable-peer-topology.md](durable-peer-topology.md) — peer memory, queue-while-down, shrink, and
  decommission, as built.
- [tessaging-WIP.md](tessaging-WIP.md) — the hub.
