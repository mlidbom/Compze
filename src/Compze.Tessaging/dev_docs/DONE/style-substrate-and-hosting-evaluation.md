# The style substrate and the Host: an evaluation

**Status: evaluation, revised 2026-07-17 (twice: the feature-machinery challenge, then the naming
settlement). Open recommendations are proposals; the decisions are Magnus's.** This document answers the
nine questions chartered as "the big job" — whether the typermedia<>tessaging split still earns its keep
below the public models, whether the hosting layer's style-ignorance is real, and what the Host actually
is — and, since the first revision, a tenth: whether the endpoint-feature machinery itself survives. Its
verdicts gate the implementation of [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md) —
building readiness into today's duplication and migrating it later would be the backwards order.

Decided so far (⚖ Magnus, 2026-07-17):

- ⚖ **Tessaging is the common paradigm** — one harmonized concept whose message kinds are tueries,
  typermedia tommands, tevents, and tommands.
- ⚖ **Typermedia and TessageBus are its two siblings**, built on the common paradigm: **Typermedia** (the
  request/response style — hypermedia with types) and **TessageBus** (the asynchronous style — the message
  bus with tessages). The names follow the family's generative rule: take the established industry concept
  and make the t-substitution — event → tevent, command → tommand, query → tuery, message → tessage,
  aggregate → taggregate, event store → `TeventStore`, hypermedia → Typermedia, **message bus →
  TessageBus**. (The earlier candidates Typercast/Typerpost/Typergram are retracted — they mistook the
  shared `Typer-` stem for the rule.) A symmetry worth putting in the public docs: the two siblings are the
  two canonical integration styles of the literature, each re-founded on .NET types — ask now and follow
  typed links; or tell the world and trust it arrives.
- ⚖ **"Ship another communication style" is not on the roadmap** and has no imaginable meaning — the
  open-ended capability seam has no prospective consumer, ever.
- ⚖ **The project homes are settled (2026-07-17): question 9's option (a) — one paradigm project — with
  the transport-weight exception.** `Compze.Tessaging` is the paradigm project (both siblings' machinery as
  namespaces within it; the named-pipe transport folds in); the ASP.NET Core transport stays its own
  project; the four SQL backend projects keep their shape. Recorded in
  [tessaging-migration-plan.md](tessaging-migration-plan.md).

Everything here is grounded in a code dig (file anchors throughout) plus a usage inventory of the two real
consumers: Deskmancer and the AccountManagement sample.

## The reframe the dig forces: three parties, not two

The question arrived as "was the typermedia<>tessaging split a good idea — what would merging them buy?"
The code says that is the wrong shape. There are **three** parties:

1. **A shared substrate that already exists**, half-extracted: the message-type hierarchy and hosting
   contracts in `Compze.Abstractions`, and the transport client/server, wire envelope, request-handler
   contribution seam, discovery round-trip, and address announcement in `Compze.Internals.Transport`.
2. **A private copy, per sibling, of the substrate's missing half**: each separately implements the
   reconcile-against-registry router, the connection map, the endpoint-information discovery payload, and
   the distributed feature/component/composition scaffolding — near-verbatim twins.
3. **Genuine sibling semantics**: tevent/tommand delivery machinery (outbox, inbox, best-effort queues,
   delivery streams, peer memory) on the TessageBus side; navigators, handler executor, and the
   request/response shape on the Typermedia side.

Under the harmonized paradigm the fate of party 2 is sharper than the first revision of this document
proposed. The first proposal was to *extract* it into a pluggable substrate with style-contributed seams
(route predicates, delivery-stream factories) — pluggability whose only purpose was preserving style
ignorance. ⚖ Harmonization abolishes style ignorance as a goal, so party 2 is not extracted — **it is
deleted**: one router that simply knows Tessaging's four message kinds, one advertisement, one discovery
query, wired concretely (see "The feature machinery on trial" below).

The same decision reverses the polarity of a whole class of findings. The dig catalogued "style leaks" in
the shared seed — `TransportRequestKind` enumerating both siblings' message kinds, `HttpConstants` carrying
both route tables, TessageBus's discovery query being an `ITuery`, Typermedia's contracts living in the
`Compze.Abstractions.Tessaging.Public` namespace. Under the split's rules those were fictions undermining
the design. Under harmonization they are the opposite: **evidence that the code has wanted to be one thing
all along**. A wire enum naming the four message kinds of one paradigm is not a leak; it is the paradigm.

## Ground truth: what real applications use

| Dimension | Deskmancer (real product) | AccountManagement (sample) |
|---|---|---|
| Role of Compze messaging | External-observation/test channel only; **off on every production launch** | The application's whole spine |
| TessageBus tier | `AddTransientTessaging` = today's `AddDistributedTessaging` (pre-rename packages) | `AddExactlyOnceTessaging` |
| Typermedia | `AddDistributedTypermedia` | `AddDistributedTypermedia` |
| Production process IPC | Hand-rolled named kernel events + shared disk state — deliberately not Compze | n/a (single process) |
| Tessages on the wire | Best-effort tevents (`IAnalyticsEvent` family + probe/echo pair), 2 tueries; **zero tommands** | 4 `AtMostOnceTypermediaTommand`s, tueries; domain tevents stored, not remoted |
| Transport | Named pipes | In-memory testing transport — **even in the "production" MVC host** |
| Discovery | `InterprocessEndpointRegistry`, zero configuration | None (client connects to the known `TypermediaAddress`) |
| Persistence | None, explicitly | Full stack (TeventStore + DocumentDb + SQL, throwaway pooled DB) |
| Readiness | **Hand-rolled**: a probe/echo tessage pair + 30s poll loop for the TessageBus attach; a `while(true)` catching `NoHandlerForTypermediaTypeException` with `Thread.Sleep(100)` for typermedia (`ExternalObservationSubscriber.cs:90-132`) | None needed (single process; host-wide barrier covers it) |
| Hosts × endpoints × processes | 2 hosts × 1 endpoint each × 2 processes | 1 host × 2 endpoints × 1 process |

Three conclusions this table forces:

- **The hand-rolled readiness probes are real and quoted.** Deskmancer invented an entire tessage pair
  (`ObservationChannelProbe`/`ObservationChannelProbeEcho`) whose only purpose is to synthesize the attach
  signal the framework doesn't provide, plus a sleep-retry loop around `NoHandlerForTypermediaTypeException`.
  This is the trigger evidence for readiness/waiting-sends, in consumer code, today.
- **Nobody real hosts multiple endpoints in one production host.** Deskmancer runs one endpoint per host per
  process. Only the sample and the test suite put two endpoints in one host — and the sample's "production"
  MVC host runs on the testing transport and a `TypermediaTestClient`. Production hosting's total real
  exercise is the multi-process specification (which
  [same-machine-hosting.md](../../../../src/Compze.Hosting/dev_docs/wip/same-machine-hosting.md) itself calls
  "the first production-hosting composition exercised end to end", 2026-07-15) plus Deskmancer's optional
  side channel.
- **The consumer that matters wants the light stack**: named pipes, no database, no web stack, best-effort
  tevents + typermedia, tiny composition. Every consolidation here serves it directly; multi-endpoint
  host machinery serves only tests. And both real consumers already speak **both siblings on every
  endpoint** — nobody composes one without the other.

## Findings

### What is already shared (the substrate seed)

| Substrate concern | Where it already lives |
|---|---|
| Endpoint identity | `EndpointId`, `EndpointConfiguration` (`Compze.Abstractions.Hosting.Public`) |
| Addressing | `EndpointAddress` + per-transport realizations (`NamedPipeAddress`, Kestrel URL) |
| Point-to-point send | `IEndpointTransportClient` (HTTP + named-pipe implementations) |
| The one server per endpoint | `IEndpointTransportServer`, `EndpointTransportServerFeature`/`Component` |
| Request-handler seam | `ITransportRequestHandlerContribution` + `TransportRequestHandlerMap` — the siblings contribute their request kinds to the one server |
| Wire envelope + framing | `TransportRequest`, `TransportRequestKind`, `NamedPipeFraming`, `HttpConstants` |
| Announcement (write side) | `EndpointTransportServerFeature.AnnounceAddressTo` → `IEndpointAddressAnnouncer` |
| Discovery round-trip | `IEndpointDiscoveryQueryTransport` + `EndpointDiscoveryQueryExecutor` + the fixed `EndpointDiscoverySerializer` |
| Registry contracts + implementation | `IEndpointRegistry`/`IEndpointRegistryAndAnnouncer` (Abstractions); `InterprocessEndpointRegistry` (`Compze.Hosting.SameMachine`) |
| The message-type hierarchy | One file: `Compze.Abstractions\Tessaging\Public\_TessageTypes..Interfaces.cs` — both siblings' contracts, one root |
| The guarantee ladder's vocabulary | Same file: `IAtMostOnceTessage`, `IAtLeastOnceTessage`, `IExactlyOnceTessage` span both siblings (see question 7) |

### What each sibling duplicates privately (party 2, measured)

| TessageBus (today's `Compze.Tessaging` code) | Typermedia | Notes |
|---|---|---|
| `TessagingRouter` (315 lines) | `TypermediaRouter` (229 lines) | Same reconcile loop, same `AwaitPossibleMembershipChange` wait, same connection map keyed by `EndpointId`, same drop/replace-on-restart logic — down to **word-for-word identical doc comments** on `ReconcileLivenessInterval` and `ReconcileLoop`. The Typermedia copy's own class comment admits it: "the same dynamic topology the Tessaging router runs on." |
| `TessagingEndpointInformationQuery`/`TessagingEndpointInformation` | `TypermediaEndpointInformationQuery`/`TypermediaEndpointInformation` | Structurally identical payloads: `Name` + `EndpointId Id` + a `HashSet<string>` of canonical type-id strings. Two questions asked over the same address, answered by the same server. |
| `DistributedTessagingEndpointFeature`/`Component` | `DistributedTypermediaEndpointFeature`/`Component` | Same scaffolding: assert foundation declared, map discovery types, compose the in-process core, `EndpointTransportServerFeature.GetOrAddTo`, register discovery handler on container-built, add the lifecycle component, `DiscoverEndpointsThrough`/`AnnounceAddressTo`/`ParticipateIn`. |
| `ExactlyOnceTessagingRequestHandlers` + `BestEffortTessagingRequestHandlers` | `TypermediaRequestHandlers` | Same `ITransportRequestHandlerContribution` shape, different request kinds. |
| `ITessagingSerializer` + `ITessagingSerializerSlot` | `ITypermediaSerializer` + `ITypermediaSerializerSlot` | Parallel slots in `Compze.Abstractions\Serialization\Internal`, both filled by the same generic `NewtonsoftSerializer()`. |
| `HandledRemoteTessageTypeIds()` | `HandledRemoteTypermediaTypeIds()` | The advertisement source, per sibling. |
| `ITessageHandlerRegistrar` (`ForTevent`/`ForTommand`) | `ITypermediaHandlerRegistrar` (`ForTuery`/`ForTommand`×3) | Two handler-registration surfaces for one paradigm's four message kinds. |
| Testing host feature | Testing host feature | `ExactlyOnceTessagingTestingEndpointHostFeature` / `DistributedTypermediaTestingEndpointHostFeature`. |

The line count is modest (~450 duplicated substrate lines per sibling). The cost is not lines — it is that
**every substrate-level effort must be designed, built, spec'd, and maintained twice**, and the copies
drift: the Typermedia copy has already drifted into defects the TessageBus copy handles deliberately (see
"Defects" below).

### What only TessageBus has (the negative space)

Peer memory (`IPeerRegistry`, `DurablePeerRegistry`/`ProcessLifetimePeerRegistry`, `RememberedPeer`),
lifecycle observation (`IPeerLifecycleObserver`), administration (`IPeerAdministration.Decommission`,
`IPeerDecommissionParticipant`, `PeerDecommissionReport`), queue-while-down, and the self-connection.
Typermedia's `TypermediaConnection` is a 9-line address+advertisement holder; its router has 3 constructor
dependencies to the `TessagingRouter`'s 11 — the delta **is** the peer/durability layer.

This negative space is exactly the shopping list of
[readiness-and-waiting-sends.md](readiness-and-waiting-sends.md): typermedia peer knowledge (known-but-down
vs never-seen), lifecycle hygiene on shrink/decommission, waiting informed by remembered peers. On today's
architecture that means porting the peer layer into the Typermedia copy — building party 2's biggest
component a second time. Harmonized, the knowledge already sits where readiness needs it: one peer registry
remembering one advertisement per peer.

### Defects and debris found while digging

Defects (both in the Typermedia copy; the TessageBus copy handles the equivalents deliberately):

1. **Duplicate typermedia route = permanently failing reconciliation.**
   `TypermediaRouter.RebuildRouteTables` uses `Dictionary.Add`
   (`src/Compze.Typermedia.Client/TypermediaRouter.cs:157,160`), so two endpoints advertising the same
   typermedia type throw `ArgumentException` inside the reconcile pass. The loop survives (catch-log-retry)
   but every subsequent pass fails at the same point: route tables freeze while `_connections` keeps
   mutating. TessageBus treats the same situation as a deliberate, diagnosable send-time failure
   (`MultipleHandlersForTessageTypeException`, decommission as the remedy). One harmonized router makes one
   policy structural.
2. **Server-side no-handler is a bare `KeyNotFoundException` — and gets retried.**
   `TypermediaHandlerRegistry.GetTommandHandlerWithReturnValue`/`GetTueryHandler(Type)` use raw dictionary
   indexers (`src/Compze.Typermedia/HandlerRegistration/TypermediaHandlerRegistry.cs:55,57`) — exactly the
   two methods the remote `TypermediaHandlerExecutor` calls — while the sibling lookup methods throw
   `NoHandlerException`. Worse, the tommand path runs inside `ExecuteWithRetry`'s catch-all (5 attempts, no
   backoff, `TypermediaHandlerExecutor.cs:48-66`), so a no-handler programming error is retried five times
   server-side. Both collide head-on with the settled readiness decision that the no-handler exception family
   becomes exclusively the patience-exhausted failure; fixing them belongs to that effort's increment 2.

Debris (deletable or trivially fixable, no design decisions needed):

- **Husk directories** with only stale `bin`/`obj`: `src/Compze.ServiceBus*` (7 directories),
  `src/Compze.Tessaging.Hosting.AspNetCore`, `src/Compze.Typermedia.Hosting.AspNetCore`, and the dead
  `src/Compze.Tessaging/Typermedia/` folder.
- **Vestigial `Compze.Core` references**: the project was dissolved, yet six csprojs (`Compze.Tessaging` +
  its 4 SQL backends + `Compze.Internals.Transport`) carry a one-sided FlexRef block that resolves to the
  frozen legacy package `0.1.0-alpha.3` in package mode — loading a stale assembly with duplicate old types
  is exactly the stale-nupkgs failure class. Zero `using Compze.Core` exists; two stale `// Compze.Core`
  comments point at types that now live in `Compze.Teventive`.
- **`EndpointAddress` is a `record`** — against the no-records convention.
- **Stale prose in [hosting-model.md](../../../../src/Compze.Hosting/dev_docs/hosting-model.md)**: two
  mentions of a tommand "scheduler" (no scheduler exists in the code), and the pre-rename
  `ISessionLocalTypermediaNavigator` (now `ILocalTypermediaNavigatorSession`).
- **`internal/rpc/...`** as Typermedia's wire route prefix — greenfield, renameable at will.

---

## The feature machinery on trial

The abstract plugin machinery — `GetOrAddFeature`, endpoint features, contribution sets, guarded
registrars, serializer slots, `OnContainerBuilt` hooks, testing-host features — is not incidental
complexity. It is the split's **exoskeleton**: every piece of it exists to let two independent verticals
share one endpoint without knowing each other. Its three documented justifications (hosting-model.md) all
presuppose separate style deliverables — "each style developed, shipped, tested alone" (no strangers to keep
apart after harmonization), "capabilities declared in one visible place" (a constructor does that better),
"a new capability without touching hosting" (⚖ no third style, ever). Merge the organism and the
exoskeleton is dead weight. Piece by piece:

| Machinery | Why it exists | Fate under harmonization |
|---|---|---|
| `GetOrAddFeature` idempotency | Shared demands arrive from multiple independent directions: both siblings demand `EndpointTransportServerFeature`; both `Add*` verbs and the `RegisterXHandlers` properties demand the in-process core | **Dies.** One concrete composition per tier = one arrival path = nothing to reconcile |
| `ITransportRequestHandlerContribution` + `TransportRequestHandlerMap` union/assert | The server must serve request kinds it is forbidden to know | **Dies.** The server takes its closed handler map for Tessaging's four kinds directly |
| Every `IfNotRegistered` guard (`EndpointDiscoveryQueryTransportIfNotRegistered`, HTTP client factory, transport server, the tracker default, `CurrentTestsInfrastructureTransportIfNotRegistered`) | Whichever of two mutually-ignorant features arrives first must win | **Dies.** One wiring path registers everything exactly once |
| Twin serializer slots + compose-lambdas (`AddExactlyOnceTessaging(t => t.NewtonsoftSerializer())`) | Each sibling fills its own slot without seeing the other's | **Dies.** One serializer, one constructor parameter |
| `OnContainerBuilt` hooks | Features schedule work against a container whose building they don't control | **Dies.** A concrete composition root does the work in order |
| `IEndpointComponent` + `AddComponent` contribution sets | Unknown capabilities join the lifecycle phases | **Dies as a contribution seam.** A concrete endpoint type implements `IEndpoint`'s six phase methods itself, starting its own known parts in order — "what starts when" becomes one readable method per phase instead of a traced component set |
| `ITestingEndpointHostFeature` | The same rule repeated one level up | **Dies.** Concrete per-tier test wiring |
| Per-sibling `Add*` verbs + `EndpointFoundation` slot-filling choreography | Features added later must find foundation registrations already in place | **Shrinks.** The foundation's compiler-routed database pairing survives as ordinary constructor typing on the concrete tier types |
| The style-contribution seams this document's first revision proposed (route predicates, stream factories) | Preserving style ignorance inside a shared substrate | **Withdrawn.** The one router knows the four kinds: tevents route by assignability, everything else by exact type — which the dig showed is the *only* style-typed part of either router today |

The dig had already documented the machinery creaking: feature-arrival **order** sensitivity (the
`RequirePeers` list captured by reference and read at container build; the pens-read-before-registry
ordering the best-effort leg pins with a comment), the `IsRegistered` guard dances, and Typermedia's
server-side seam living in the *Client* project because "features live where their internal access is
natural" — the abstraction dictating project layout.

**Is there any real reason not to wire everything unconditionally within a tier?** None found. An endpoint
with the tuery executor wired but no tuery handlers registered costs a few idle singletons, an empty
advertisement set, and requests that fail loud. Ground truth agrees: both real consumers already speak both
siblings on every endpoint. Unconditional wiring actively closes holes — decommission-covers-only-one-sibling
and the twin-discovery chatter become structurally impossible. The one conditionality that is **real** is
the tier, because the guarantee ladder is real and durability follows the foundation: an outbox cannot
exist without storage.

What survives, as ordinary parameters and strategies rather than plugin machinery: the transport protocol
(a strategy interface with two implementations — already is), the sql backend (foundation-typed — already
is), the serializer (a parameter), handler registration, topology options (`ParticipateIn`/registry,
`RequirePeers`, `DoNotQueueTeventsFor`), and the DI container pluggability (orthogonal, untouched).

## The shape after the collapse: concrete endpoint types

The tier ladder — local ⊂ best-effort ⊂ exactly-once — collapses from a feature lattice into **plain code a
human can read top to bottom**:

1. **Local Tessaging — no endpoint at all.** A container registrar wiring the handler registry, synchronous
   dispatch, publishers, and the local navigator. hosting-model.md already says it: "in-process Compze is
   container wiring, not hosting." A local-only *endpoint* stops being a thing — it never was one; the
   `AddInProcess*` endpoint features existed only so the lattice could compose the core.
2. **The best-effort endpoint** — a concrete type: transport server, discovery, router, process-lifetime
   peer registry, best-effort tevent queues, *all four message kinds served*, navigators and publishers.
   No database. (Deskmancer's exact composition.)
3. **The exactly-once endpoint** — everything above plus the durable vertical: inbox, outbox, durable peer
   registry, tommand senders; parameterized by the database foundation. (AccountManagement's exact
   composition.)

The two endpoint tiers differ only in their **TessageBus rung** — which delivery guarantees the endpoint can
give; Typermedia rides both tiers identically (request/response neither queues nor persists). Plus the one
small extra shape that is real: a **pure client** — navigator + transport client, no server — today's
`TypermediaTestClient`.

Each type is a composition root that openly lists its parts and drives its own lifecycle phases. The parts
that do the actual work — router, outbox, queues, streams, executor, registries — survive untouched; what
melts is the composition plumbing between them. Handler registration plausibly unifies too: one registrar
with `ForTuery`/`ForTommand`/`ForTevent` instead of two per-sibling surfaces. Consciously given up:
the ship-a-sibling-alone property (dies with harmonization by definition), the "no Tessaging assembly
loaded" proof specs (same), and the open capability seam (⚖ no consumer, ever; a seam can be re-extracted
from working concrete code if the inconceivable happens).

---

## Question 1 — Is there any point left to the split?

**What still earns its keep:** the siblings' *public models*. A tuery is a synchronous question; a tevent is
a persistent fact told to the world. Distinct message kinds, distinct navigator/publisher surfaces, distinct
delivery semantics. Nothing in the dig argues against any of that, and the June split's production-code
disentanglement made real structure visible that the old entanglement hid — including the twin routers
themselves, whose near-identity is the central evidence here.

**What no longer earns its keep:** the boundary as an architectural wall. Evaluated rung by rung under the
harmonized paradigm:

| Rung | Verdict | Reasoning |
|---|---|---|
| **Message-type hierarchy** | Already one | One file, one root, since the beginning; the siblings interleave in it (`ITommand<TResult> : ITyperMediaTessage<TResult>`) |
| **Wire protocol / request kinds** | Already one | One envelope, one kind enum, one route table, one server. Under harmonization the kind enum stops being a leak and becomes the paradigm |
| **Discovery mechanism** | Already one | One query transport, one executor, one fixed serializer |
| **Advertisement (payload)** | **Unify** | Two structurally identical payloads answering two questions over one address become one `EndpointInformation` carrying name + id + handled types. The old "advertisement unification" question dissolves here |
| **Router / connections / reconciliation** | **Unify — concretely** | One router knowing the four kinds (tevent-assignability, exact-match for the rest). No contribution seams: the seam-based merge proposed in this document's first revision is withdrawn |
| **Peer memory / lifecycle / administration** | **Unify** | One registry remembering one advertisement per peer; `Decommission` covers every message kind at once; readiness's typermedia knowledge exists for free |
| **Delivery machinery** | **Keep per sibling** | Outbox/inbox/queues/streams are TessageBus; the handler executor and navigators are Typermedia. This is party 3 — never merge it |
| **Composition surface** | **Concrete endpoint types** | The per-sibling `Add*` feature verbs die with the lattice (see "The feature machinery on trial"); the tier is the composition |
| **Assemblies** | **Unsettled** | The exact set of remaining projects and their names is an open decision; question 9 |

**Recommendation:** keep the siblings' public models and delivery semantics distinct *inside* one harmonized
Tessaging; delete party 2 into the concrete shapes above. The split was not a mistake — it removed real
entanglement and made the substrate visible. Its follow-through is harmonization, not re-entanglement: the
siblings stop being strangers sharing a flat, and become two children of one paradigm.

## Question 2 — What duplication and complexity would disappear?

Immediately, party 2 (the twin table above): routers, discovery payloads, feature/component scaffolding,
advertisement sources, serializer slots, handler-registrar pair. With the feature machinery's collapse, the
abstraction layer itself joins the list: `GetOrAddFeature` and the feature classes, the
request-handler-contribution union machinery, every `IfNotRegistered` guard, the `OnContainerBuilt` hook
choreography, the `IEndpointComponent` contribution seam, the testing-host feature seam, the per-sibling
`Add*` verbs and slot-filling lambdas. What remains is Magnus's list: **choose the tier, the transport, the
sql layer, and the serializer; register your handlers.**

The future tax is the larger number — per pending roadmap item, on today's architecture versus harmonized:

| Pending effort | Today | Harmonized |
|---|---|---|
| Waiting sends | Built twice: once against the `TessagingRouter`/peer registry, once against the `TypermediaRouter` with no peer memory to consult | Built once where the one router and peer memory live |
| Readiness | Same twice-over, plus porting peer knowledge to Typermedia first | One awaitable over one peer registry |
| Typermedia peer knowledge (retired "increment 7") | A Typermedia-private peer-memory port | Free — the registry already remembers every peer's one advertisement |
| Decommission for typermedia routes | Unbuilt; today's `Decommission` covers TessageBus only | Covered by the same act — one router, one registry |
| Distributed quiescence (question 6) | Two report sources, two aggregations | One administration surface |
| A new transport | Already cheap (transport is shared) — unchanged | Unchanged |

The dynamic-topology effort already paid this tax once (the "Typermedia parity" batch of 2026-07-15 was
precisely re-implementing reconciliation for the second router); shrink/decommission is paying it now;
readiness would pay it a third time. That recurrence — every cross-cutting effort ending in "now do it
again for the other style" — is the architecture telling us the seam is in the wrong place.

## Question 3 — Local, in-process, distributed

Settled in the design conversation and confirmed by the dig: **"in-process" and "local" are one concept —
same-endpoint scope — wearing two words, and "in-process" lies at its only distinguishing edge.** Two
co-located endpoints in one multi-endpoint host converse through the full distributed machinery (real
transport connections, discovery, the router — there is no same-host fast path anywhere in the code,
verified), never through the in-process tier. The tier's boundary is always the **endpoint**, never the
process.

Under the feature-machinery collapse the resolution sharpens further: **local is the core programming
model** — handlers, publishers, navigators, all in the caller's transaction against the endpoint's own
handlers — and it is *container wiring, not an endpoint tier*. The word "in-process" dies everywhere; the
local container composition keeps the word **local**, agreeing with `ILocalTypermediaNavigatorSession` and
`IStrictlyLocalTessage`, which already use it for exactly this scope. The former "in-process endpoint
feature" shape disappears entirely: an endpoint that wants local navigation alongside its remote conversing
has it unconditionally — it is part of the core every tier wires.

## Question 4 — The Host on trial

The production `EndpointHost` (`src/Compze.Hosting/EndpointHost.cs`) is four fields — a container-builder
factory, a `List<IEndpoint>`, two flags — and three `Task.WhenAll` barriers. The full responsibility
inventory, each tagged against the bar set for this evaluation (*multi-endpoint-in-one-process is a
legitimate deployment option, but it must be built only from abstractions behaving identically when the
endpoints migrate to their own processes*):

| Responsibility | Where | Verdict |
|---|---|---|
| Fresh container per endpoint + baseline registrations (type-mapper pre-map, identity, default config provider, discovery-query executor) | `ServerEndpointBuilder.SetupContainer` | **Production-real** — but it is endpoint *composition*, ~10 lines an endpoint could own itself; nothing about it needs a multi-endpoint coordinator |
| Per-endpoint phase ordering (listen before announce before send; retract before stop) | `Endpoint`'s six phase methods | **Production-real** — and already owned by the endpoint, not the host. This ordering is what makes "an announced address is always listening" true, in any process topology |
| **Host-wide** phase barrier (every endpoint's phase N before any endpoint's phase N+1) | `EndpointHost.StartAsync:53-55` | **Pretense at its headline guarantee.** Guarantees 1–2 ("nothing sends to a non-listening endpoint", "announced means listening") come from per-endpoint ordering + announce-after-listen and hold cross-process. Guarantee 3 — "a router's first look at the registry sees every endpoint the host announced" — holds only inside one process's `StartAsync` and silently degrades to reconciliation-based convergence the moment a second host exists. The design knows this (the routers reconcile continuously; same-machine-hosting.md's whole model is "senders converge, nothing is wired once") — but in-host code and specs get a stronger world than cross-process code, which is precisely the observational-equivalence violation the bar forbids |
| Direct object handoff / loopback shortcuts between co-hosted endpoints | — | **None exist — honest.** Self-send rides the endpoint's own transport server; co-hosted endpoints discover each other through the real registry and converse over real connections. This is the strongest part of the design and the reason the collapse is tractable |
| At-rest quiescence + background-exception rethrow | `TestingEndpointHost` dispose; the shared-by-reference `TessagesInFlightTracker` one host feature instance registers into every endpoint | **Test-only** — production resolves `NullOpTessagesInFlightTracker` everywhere; and the mechanism (one tracker object shared across endpoints' heaps) is *unbuildable* cross-process. Legitimate as a testing device; a production quiescence story must be built differently (question 6) |

**"Why must a Host create an endpoint at all?"** It mustn't. With concrete endpoint types the answer
becomes literal: an endpoint is a type you construct and start; a host is an optional convenience that owns
several endpoints' lifecycles in one process — a `WhenAll` runner, not infrastructure.

Options:

- **(a) Endpoint becomes the first-class unit; Host demotes to a convenience.** The concrete tier types
  (above) each own their composition and lifecycle; the host-wide barrier weakens to what is honest
  everywhere: per-endpoint phase ordering plus announced-means-listening. Startup completeness — today the
  barrier's guarantee 3 — is carried instead by the mechanisms that already carry it cross-process:
  queue-while-down and `RequirePeers` for TessageBus, readiness/waiting-sends for request/response.
- **(b) Keep the Host as the sole entry, keep the barrier, document it as in-process-only comfort.** Cheaper
  now; but it leaves two worlds (in-host strong ordering, cross-host convergence), keeps specs passing
  against guarantees production topologies don't have, and keeps the "why must a Host exist" question
  unanswered.

**Recommendation: (a) — with sequencing teeth.** The barrier cannot be honestly weakened until
readiness/waiting-sends exist, because in-host compositions and specs currently lean on guarantee 3 to avoid
the startup race that readiness is designed to absorb. The Host is the last piece to move, not the first.

## Question 5 — The testing story

Not on trial: the testing host stays (settled). What changes and what the dig adds:

- The testing host's claim to honesty is largely real: every test runs the production announce/discover
  pipeline on a real `InterprocessEndpointRegistry`, real transports, no mocks. Its two in-process-only
  shortcuts are the cloned root container (shared DbPool/serializers — pure test economics, fine) and the
  shared-by-reference in-flight tracker.
- The `ITestingEndpointHostFeature` seam dies with the feature machinery; its content survives as concrete
  per-tier test wiring (the tracker handed to the endpoint at construction — cleaner than today's
  `IsRegistered` guard dance).
- The at-rest wait ("a test cannot pass while work is in flight") is a *testing device* and can stay one —
  a shared heap is a legitimate testing convenience. When distributed quiescence (question 6) exists, the
  testing host gains a production-honest alternative: await the same quiescence report production
  administrators read, and keep the tracker only for what quiescence cannot see (background exceptions no
  assertion observed — a separate concern that could earn its own name).
- What specs await when the host-wide barrier weakens (question 4a): the deterministic-topology idioms
  already invented for the distributed-tier specs — `RequirePeers` pens, the read-counting registry double
  (`AwaitTwoReadsCompletingAfterNow`), and, once built, the readiness awaitable itself. Specs that today
  implicitly ride guarantee 3 would migrate to explicit awaits — which is also what makes them honest about
  what production code must do.

## Question 6 — Distributed quiescence: the tracker recast as a production tool

The idea (Magnus's): "all queues empty across the application's communicating components → safe
update/shutdown." The dig confirms the per-endpoint facts already exist, durably or in-memory:

- Outbox: undelivered exactly-once rows per receiving peer (already queryable — the recovery backlog).
- Best-effort: `BestEffortTeventQueues` depths + in-transaction reservations per peer.
- Inbox: received-but-not-yet-successfully-handled tessages.
- Transport: the in-flight-on-connection set (today only the testing tracker sees it; each connection knows).

Sketch — deliberately shaped like the machinery that already works:

- **A `QuiescenceReport` infrastructure tuery** every distributed endpoint serves over the one transport
  server, exactly as it serves the endpoint-information query today: per-category counts, each tagged by
  **volatility** — *durable* (outbox rows waiting for a down peer: safe to stop, the rows survive and wait)
  vs *volatile* (best-effort queue contents, in-flight handling: stopping now loses them).
- **An aggregation act on `IPeerAdministration`'s sibling** (or the same surface): ask every peer the
  registry lists plus every remembered peer; quiescent = every volatile count is zero across the suite, held
  across two consecutive sweeps (the double-read idiom the registry specs already use — a single snapshot
  can be invalidated mid-read by a tessage in flight between two peers).
- **Two-phase use in operations**: (1) stop producing new work — an application-level act, possibly assisted
  by a future administrative "pause sends" — then (2) await suite quiescence, then update/stop. Readiness is
  the mirror on the way back up: **readiness = safe to start taking traffic; quiescence = safe to stop** —
  two faces of production administration, siblings on the same administration surface. Both span every
  message kind by construction: a tuery in flight and a tevent in a queue both block quiescence — which is
  also why quiescence is unbuildable per-sibling and unbuildable on the shared-heap tracker. It has one
  possible home: the harmonized Tessaging core.

This is a sketch to be designed properly in its own effort.

## Question 7 — The guarantee vertical is the tier ladder, and it spans the siblings

Confirmed by the type hierarchy itself: the delivery-guarantee ladder is declared in the shared root —
`IAtMostOnceTessage` (which `IAtMostOnceTypermediaTommand` and `IExactlyOnceTessage` both extend),
`IAtLeastOnceTessage`, `IExactlyOnceTessage` — and spans the siblings: at-most-once is a *typermedia*
tommand rung and an *exactly-once* building block at once. The transactionality markers
(`IMustBeSentTransactionally`, `ICannotBeSentRemotelyFromWithinTransaction`) are the same shape. Under the
collapse this stops being an observation and becomes the design: **the concrete endpoint tiers ARE the
ladder's rungs** — the best-effort endpoint serves the guarantee-free kinds, the exactly-once endpoint adds
the durable ones, and a tessage's type decides which machinery carries it, exactly as the tevent delivery
model already works.

## Question 8 — Ground truth

Hoisted to the top of the document (it grounds everything). One correction to keep on record: Deskmancer's
`AddTransientTessaging` is today's `AddDistributedTessaging` under its pre-rename package name — the
best-effort tier has a real consumer; the surfaces with **zero** real consumers are the in-process tier as
an endpoint feature, `RequirePeers`, and multi-endpoint production hosting.

## Question 9 — Naming and migration

**The names — ⚖ settled.** **Tessaging** is the common paradigm; **Typermedia** and **TessageBus** are its
two siblings. The generative rule (established concept + t-substitution, `TeventStore` as the compound
precedent) is recorded in the decisions block at the top. Two knock-on resolutions come free:

- The long-flagged namespace overload dissolves: `Compze.Abstractions.Tessaging.Public` holding tueries
  stops being one style's name annexing the parent domain and becomes simply the paradigm's domain.
- The parked ServiceBus remnants got their renames (executed in migration phase 1): `IServiceBusSqlLayer`
  is `ITessagingSqlLayer` — the paradigm's word, not TessageBus's, because the layer holds the peer
  registry (paradigm-level after harmonization) beside the inbox and outbox, and the target design names
  what it stores "tessaging storage" — and the `ServiceBusSpecification` test folders dissolved into their
  already-truthful `Tessaging/` parents, since their specs span both siblings (tueries and the navigator
  beside outbox, inbox, and peers). "ServiceBus" died as borrowed jargon on a lying type.

One naming caveat noticed and accepted: "bus" can suggest a broker in the middle, and Compze's tessage bus
is brokerless peer-to-peer — but the term's actual industry usage (NServiceBus, MassTransit) covers
brokerless topologies, so it informs rather than lies.

**The homes — ⚖ settled 2026-07-17: option (a) with the transport-weight exception (see the decisions
block at the top; the option space below is kept as the decision's record).** The tension the naming
settlement fixed: today's
`Compze.Tessaging` project carries the TessageBus sibling under the paradigm's name, and the paradigm's
core (endpoint types, router, peers, discovery, wire) needs a home that both siblings' machinery lives
under or beside. The option space, laid out for that decision:

- **(a) One project:** `Compze.Tessaging` = the paradigm — core plus both siblings' machinery in one
  assembly (Typermedia's projects and `Compze.Internals.Transport` fold in; siblings live as namespaces:
  `Compze.Tessaging.Typermedia`, `Compze.Tessaging.TessageBus`, or similar). Simplest dependency graph;
  matches "both real consumers speak both siblings"; the assembly is not small.
- **(b) Paradigm core + two sibling projects:** `Compze.Tessaging` (core: hierarchy, endpoint types, router,
  peers, discovery, transports) with `Compze.TessageBus` (outbox/inbox/queues/streams/senders) and
  `Compze.Typermedia` (navigators/executor) atop it. Truthful names per project; but the concrete endpoint
  types wire both siblings unconditionally, so either the endpoint types move *up* into a composition
  project referencing both siblings, or the core references the siblings — the layering needs care to avoid
  re-creating a composition layer the collapse just deleted.
- **(c) Minimal movement:** keep today's project set, rename in place (`Compze.Tessaging` →
  `Compze.TessageBus`; a new home for the paradigm core grown out of `Compze.Internals.Transport`). Cheapest
  mechanically; leaves the most seams.

The per-backend SQL projects (`Compze.Tessaging.Sqlite` etc.) keep their shape in every option; whether
they follow the paradigm's name or the TessageBus name depends on where the durable vertical lands (they
hold inbox/outbox — TessageBus — but also the peer-registry layer, which is paradigm-level after
harmonization; not clean either way, decide with the homes). `Compze.Hosting` shrinks toward its two real
contents — the `InterprocessEndpointRegistry` and the Host convenience — whose ultimate home is decided
with the Host demotion, last.

**Migration order — rehome concepts before restructuring, names and homes first, wiring second:**

1. **Names + deletions + defect fixes (no design risk, ship immediately):** "in-process" → "local" in
   vocabulary and docs; the ServiceBus-remnant renames into the TessageBus vocabulary; delete the husk
   directories and the vestigial `Compze.Core` references; fix the two Typermedia defects (bugs today,
   independent of everything here); fix the stale hosting-model.md prose.
2. **Settle the homes (the open decision above), then build the concrete tier types on the harmonized
   paradigm, deleting the lattice as they land:** one advertisement + one discovery query; one router
   knowing the four kinds; peer memory, lifecycle observation, and `Decommission` under it (decommission
   thereby covering typermedia routes is the acceptance test that the harmonization is real); one handler
   registrar; the best-effort and exactly-once endpoint types replacing the feature compositions; the local
   container registrar replacing the `AddInProcess*` features. This goes **straight to concrete** — the
   first revision's pluggable-substrate waypoint is withdrawn as scaffolding for a style boundary that no
   longer exists.
3. **Readiness + waiting sends, built once, into the concrete types** — the increments already sketched in
   [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md), now with an unambiguous *where*.
4. **The Host demotion (question 4a), last:** endpoint types first-class, host a convenience, barrier
   weakened to per-endpoint ordering — only after readiness exists to carry what the barrier pretended to
   guarantee.

**How long does this gate readiness/waiting-sends?** Steps 1–2 are the gate. Step 1 is mechanical. Step 2
is real design-and-build work, but it *deletes* more than it adds — each cut (advertisement, router, peers,
endpoint types) ships green independently, and skipping the pluggable-substrate waypoint makes it cheaper
than the first revision's plan. The alternative — building readiness twice into the duplication now —
pays the harmonization cost anyway later, *plus* a double readiness implementation, *plus* the migration of
both.

## Recommendation summary

⚖ already decided: Tessaging is the common paradigm; **Typermedia** and **TessageBus** are its two
siblings; no third style, ever; the exact project set and names remain unsettled.

Proposed, awaiting decision:

1. Delete the feature machinery into concrete compositions: a local container registrar (no endpoint), a
   best-effort endpoint type, an exactly-once endpoint type, a pure-client composition. Unconditional wiring
   of all four message kinds within a tier; the tier is the only real conditionality, because durability
   follows the foundation.
2. One advertisement, one router, one peer registry, one handler registrar — built concretely inside
   migration step 2, no contribution seams.
3. "In-process" dies; **local** is the word for same-endpoint scope, and local Tessaging is container
   wiring, not hosting.
4. Settle the project homes (options (a)/(b)/(c) in question 9) in a design conversation before migration
   step 2.
5. Demote the Host to a multi-endpoint convenience over first-class endpoint types — after
   readiness/waiting-sends land.
6. Keep the testing host; its feature seam dies into concrete per-tier test wiring; distributed quiescence
   eventually gives specs and operators an honest await.
7. Fix the two Typermedia defects and delete the debris now, independent of every decision above.

## Relation to the other effort documents

- [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md): its "gating open question" is answered
  by this document; its increments acquire a *where* (the harmonized Tessaging core) and two prerequisite
  steps (migration steps 1–2 above).
- [durable-peer-topology.md](durable-peer-topology.md): complete and unchanged; its peer memory, lifecycle
  observation, and decommission machinery are the pieces migration step 2 moves under the one router and
  generalizes across the siblings.
- [tessaging-WIP.md](../WIP/tessaging-WIP.md): the hub; links this document.
