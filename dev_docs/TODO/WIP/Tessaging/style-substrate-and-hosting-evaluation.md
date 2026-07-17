# The style substrate and the Host: an evaluation

**Status: evaluation for decision (2026-07-17). Every recommendation below is a proposal; the decisions are
Magnus's.** This document answers the nine questions settled as the charter for "the big job": whether the
typermedia<>tessaging split still earns its keep below the public models, whether the hosting layer's
style-ignorance is real, and what the Host actually is. Its verdicts gate the implementation of
[readiness-and-waiting-sends.md](readiness-and-waiting-sends.md) — building readiness into today's
duplication and migrating it later would be the backwards order.

Everything here is grounded in a code dig (file anchors throughout) plus a usage inventory of the two real
consumers: Deskmancer and the AccountManagement sample.

## The reframe the dig forces: three parties, not two

The question arrived as "was the typermedia<>tessaging split a good idea — what would merging them buy?"
The code says that is the wrong shape. There are **three** parties:

1. **A shared substrate that already exists**, half-extracted: the message-type hierarchy and hosting
   contracts in `Compze.Abstractions`, and the transport client/server, wire envelope, request-handler
   contribution seam, discovery round-trip, and address announcement in `Compze.Internals.Transport`.
2. **A private copy, per style, of the substrate's missing half**: each style separately implements the
   reconcile-against-registry router, the connection map, the endpoint-information discovery payload, and
   the distributed feature/component/composition scaffolding — near-verbatim twins.
3. **Genuine style semantics**: tevent/tommand delivery machinery (outbox, inbox, best-effort queues,
   delivery streams, peer memory) on the Tessaging side; navigators, handler executor, and the
   request/response shape on the Typermedia side.

So the choice is not "merge the styles" versus "keep the split". The styles' public models stay distinct —
a tuery is not a tevent, and nothing here proposes otherwise. The choice is whether party 2 keeps existing:
whether the substrate becomes **one thing with a name and a home**, with the styles as handler kinds plus
delivery semantics on top of it, or stays duplicated inside each style.

## Ground truth: what real applications use

| Dimension | Deskmancer (real product) | AccountManagement (sample) |
|---|---|---|
| Role of Compze messaging | External-observation/test channel only; **off on every production launch** | The application's whole spine |
| Production process IPC | Hand-rolled named kernel events + shared disk state — deliberately not Compze | n/a (single process) |
| Tessaging tier | `AddTransientTessaging` = today's `AddDistributedTessaging` (pre-rename packages) | `AddExactlyOnceTessaging` |
| Typermedia | `AddDistributedTypermedia` | `AddDistributedTypermedia` |
| Tessages on the wire | Best-effort tevents (`IAnalyticsEvent` family + probe/echo pair), 2 tueries; **zero tommands** | 4 `AtMostOnceTypermediaTommand`s, tueries; domain tevents stored, not remoted |
| Transport | Named pipes | In-memory testing transport — **even in the "production" MVC host** |
| Discovery | `InterprocessEndpointRegistry`, zero configuration | None (client connects to the known `TypermediaAddress`) |
| Persistence | None, explicitly | Full stack (TeventStore + DocumentDb + SQL, throwaway pooled DB) |
| Readiness | **Hand-rolled**: a probe/echo tessage pair + 30s poll loop for tessaging attach; a `while(true)` catching `NoHandlerForTypermediaTypeException` with `Thread.Sleep(100)` for typermedia (`ExternalObservationSubscriber.cs:90-132`) | None needed (single process; host-wide barrier covers it) |
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
  tevents + typermedia, tiny composition. Every substrate improvement serves it directly; multi-endpoint
  host machinery serves only tests.

## Findings

### What is already shared (the substrate seed)

| Substrate concern | Where it already lives |
|---|---|
| Endpoint identity | `EndpointId`, `EndpointConfiguration` (`Compze.Abstractions.Hosting.Public`) |
| Addressing | `EndpointAddress` + per-transport realizations (`NamedPipeAddress`, Kestrel URL) |
| Point-to-point send | `IEndpointTransportClient` (HTTP + named-pipe implementations) |
| The one server per endpoint | `IEndpointTransportServer`, `EndpointTransportServerFeature`/`Component` |
| Request-handler seam | `ITransportRequestHandlerContribution` + `TransportRequestHandlerMap` — styles contribute their request kinds to the one server |
| Wire envelope + framing | `TransportRequest`, `TransportRequestKind`, `NamedPipeFraming`, `HttpConstants` |
| Announcement (write side) | `EndpointTransportServerFeature.AnnounceAddressTo` → `IEndpointAddressAnnouncer` |
| Discovery round-trip | `IEndpointDiscoveryQueryTransport` + `EndpointDiscoveryQueryExecutor` + the fixed `EndpointDiscoverySerializer` |
| Registry contracts + implementation | `IEndpointRegistry`/`IEndpointRegistryAndAnnouncer` (Abstractions); `InterprocessEndpointRegistry` (`Compze.Hosting.SameMachine`) |
| The message-type hierarchy | One file: `Compze.Abstractions\Tessaging\Public\_TessageTypes..Interfaces.cs` — both styles' contracts, one root |
| The guarantee ladder's vocabulary | Same file: `IAtMostOnceTessage`, `IAtLeastOnceTessage`, `IExactlyOnceTessage` span both styles (see question 7) |

### What each style duplicates privately (party 2, measured)

| Tessaging | Typermedia | Notes |
|---|---|---|
| `TessagingRouter` (315 lines) | `TypermediaRouter` (229 lines) | Same reconcile loop, same `AwaitPossibleMembershipChange` wait, same connection map keyed by `EndpointId`, same drop/replace-on-restart logic — down to **word-for-word identical doc comments** on `ReconcileLivenessInterval` and `ReconcileLoop`. The Typermedia copy's own class comment admits it: "the same dynamic topology the Tessaging router runs on." |
| `TessagingEndpointInformationQuery`/`TessagingEndpointInformation` | `TypermediaEndpointInformationQuery`/`TypermediaEndpointInformation` | Structurally identical payloads: `Name` + `EndpointId Id` + a `HashSet<string>` of canonical type-id strings. Two questions asked over the same address, answered by the same server. |
| `DistributedTessagingEndpointFeature`/`Component` | `DistributedTypermediaEndpointFeature`/`Component` | Same scaffolding: assert foundation declared, map discovery types, compose the in-process core, `EndpointTransportServerFeature.GetOrAddTo`, register discovery handler on container-built, add the lifecycle component, `DiscoverEndpointsThrough`/`AnnounceAddressTo`/`ParticipateIn`. |
| `ExactlyOnceTessagingRequestHandlers` + `BestEffortTessagingRequestHandlers` | `TypermediaRequestHandlers` | Same `ITransportRequestHandlerContribution` shape, different request kinds. |
| `ITessagingSerializer` + `ITessagingSerializerSlot` | `ITypermediaSerializer` + `ITypermediaSerializerSlot` | Parallel slots in `Compze.Abstractions\Serialization\Internal`, both filled by the same generic `NewtonsoftSerializer()`. |
| `HandledRemoteTessageTypeIds()` | `HandledRemoteTypermediaTypeIds()` | The advertisement source, per style. |
| Testing host feature | Testing host feature | `ExactlyOnceTessagingTestingEndpointHostFeature` / `DistributedTypermediaTestingEndpointHostFeature`. |

The line count is modest (~450 duplicated substrate lines per style). The cost is not lines — it is that
**every substrate-level effort must be designed, built, spec'd, and maintained twice**, and the copies
drift: the Typermedia copy has already drifted into defects the Tessaging copy handles deliberately (see
"Defects" below).

### What only Tessaging has (the negative space)

Peer memory (`IPeerRegistry`, `DurablePeerRegistry`/`ProcessLifetimePeerRegistry`, `RememberedPeer`),
lifecycle observation (`IPeerLifecycleObserver`), administration (`IPeerAdministration.Decommission`,
`IPeerDecommissionParticipant`, `PeerDecommissionReport`), queue-while-down, and the self-connection.
Typermedia's `TypermediaConnection` is a 9-line address+advertisement holder; its router has 3 constructor
dependencies to the Tessaging router's 11 — the delta **is** the peer/durability layer.

This negative space is exactly the shopping list of
[readiness-and-waiting-sends.md](readiness-and-waiting-sends.md): typermedia peer knowledge (known-but-down
vs never-seen), lifecycle hygiene on shrink/decommission, waiting informed by remembered peers. On today's
architecture that means porting the peer layer into the Typermedia copy — building party 2's biggest
component a second time. On a shared substrate it means the knowledge already sits where readiness needs it.

### The style-ignorance of the shared seed is already a fiction

The split's rule — "the hosting machinery knows nothing of Tessaging, Typermedia, or any other capability" —
holds for assembly references. It does not hold for concepts:

- **`TransportRequestKind`** (`Compze.Internals.Transport`) is one enum enumerating both styles' message
  kinds: `ExactlyOnceTevent`, `ExactlyOnceTommand`, `TypermediaTuery`, `TypermediaTommandWithResult`,
  `TypermediaVoidTommand`, `EndpointDiscoveryQuery`, `BestEffortTevent`.
- **`HttpConstants.Routes`** carries both styles' route tables side by side — Typermedia's still under the
  pre-rename wire name `internal/rpc/...`. `HttpEndpointTransportClient.RouteFor` and the AspNet
  `TransportRequestController`'s seven actions hard-code the same per-style knowledge.
- **Tessaging's own discovery query is a tuery**: `TessagingEndpointInformationQuery :
  ITuery<TessagingEndpointInformation>`. The style that supposedly knows nothing of request/response rides
  the request/response message kind for its own discovery — as does the style-neutral discovery mechanism
  itself (`IEndpointDiscoveryQueryTransport.GetAsync<TResult>(ITuery<TResult>, EndpointAddress)`).
- **The whole shared hierarchy — including Typermedia's contracts — lives in a namespace named after one
  style**: `Compze.Abstractions.Tessaging.Public` declares `ITuery`, `ITypermediaTessage`,
  `IAtMostOnceTypermediaTommand`. (The old "tessage parent-domain vs Tessaging style-name overload" follow-up,
  still open.)

None of this is an accusation — it is evidence. The wire protocol, the message hierarchy, and discovery were
never separable by style, and the code has been saying so all along. `Compze.Internals.Transport`'s own
CHANGELOG documents the ongoing curation toward "the style-neutral discovery mechanism… each communication
style defines its own discovery query in its own assembly" — the substrate is being extracted piecemeal
already, without a name.

### Defects and debris found while digging

Defects (both in the Typermedia copy; the Tessaging copy handles the equivalents deliberately):

1. **Duplicate typermedia route = permanently failing reconciliation.**
   `TypermediaRouter.RebuildRouteTables` uses `Dictionary.Add`
   (`src/Compze.Typermedia.Client/TypermediaRouter.cs:157,160`), so two endpoints advertising the same
   typermedia type throw `ArgumentException` inside the reconcile pass. The loop survives (catch-log-retry)
   but every subsequent pass fails at the same point: route tables freeze while `_connections` keeps
   mutating. Tessaging treats the same situation as a deliberate, diagnosable send-time failure
   (`MultipleHandlersForTessageTypeException`, decommission as the remedy).
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

## Question 1 — Is there any point left to the split?

**What still earns its keep:** the styles' *public models*. A tuery is a synchronous question; a tevent is a
persistent fact told to the world. Distinct message kinds, distinct navigator/publisher surfaces, distinct
delivery semantics, separately composable (`AddDistributedTypermedia` without any Tessaging and vice versa),
separately testable. Nothing in the dig argues against any of that, and the June split's production-code
disentanglement made real structure visible that the old entanglement hid.

**What no longer earns its keep:** each style privately owning a copy of the distributed-endpoint substrate.
"Merge" is a spectrum; evaluated rung by rung:

| Rung | Merge? | Reasoning |
|---|---|---|
| **Message-type hierarchy** | Already merged | One file, one root, since the beginning. This was never split, and the styles interleave in it (`ITommand<TResult> : ITyperMediaTessage<TResult>`). |
| **Wire protocol / request kinds** | Already merged | One envelope, one kind enum, one route table, one server. Also never split. |
| **Discovery mechanism** | Already merged | One query transport, one executor, one fixed serializer. |
| **Advertisement (payload)** | **Merge** | Two structurally identical payloads answering two questions over one address. One `EndpointInformation` carrying name + id + handled-type sets serves both styles and halves the discovery chatter. The old "advertisement unification" question dissolves here. |
| **Router / connections / reconciliation** | **Merge** | The copies are near-verbatim twins; the only style-typed part is route derivation (tevent-assignability vs tommand-exact vs typermedia-exact). One router owning connections and reconciliation, with styles contributing route-derivation predicates and delivery streams, removes party 2 entirely. |
| **Peer memory / lifecycle / administration** | **Merge (move down)** | `RememberedPeer`'s type-partition and the two query methods are the only style-aware surface; make the partition style-supplied and the registry, observers, and `Decommission` cover every style at once. Readiness's typermedia knowledge then exists for free. |
| **Delivery machinery** | **Keep per style** | Outbox/inbox/queues/streams are genuinely Tessaging; the handler executor and navigators are genuinely Typermedia. This is party 3 — never merge it. |
| **Composition surface** | **Keep per style (thin)** | `AddDistributedTessaging`/`AddDistributedTypermedia` remain the user-facing words; each becomes thin over a shared distributed-endpoint feature, exactly the `GetOrAddFeature` pattern `EndpointTransportServerFeature` already proves one level down. |
| **Assemblies** | Follows the above | Not a goal in itself. The substrate needs *a* home (question 9); the style assemblies stay. |

**Does merging cost anything in practice?** The honest costs found:

- The substrate must expose a style-contribution seam (route predicates, delivery-stream factories,
  request-kind handlers) instead of hard-coding two known styles. That seam mostly exists already
  (`ITransportRequestHandlerContribution`, the component-set idiom); routing needs the same treatment.
- A shared advertisement couples the styles' wire evolution to one payload — greenfield, and the payload is
  three fields.
- The "each style is a complete vertical shippable alone" property weakens to "each style is a complete
  vertical *on the substrate*" — the same relationship both already have to `Compze.Internals.Transport`
  and `Compze.Abstractions` today. Nothing real is lost.

**Recommendation:** keep the split of public models and delivery semantics; collapse party 2 into one
distributed-endpoint substrate. The split was not a mistake — it removed real entanglement and made the
substrate *visible*. Its follow-through is extraction of the now-visible common half, not re-entanglement.

## Question 2 — What duplication and future tax would disappear?

Disappears immediately (the party-2 table above): the twin routers, twin discovery payloads, twin
feature/component scaffolding, twin advertisement sources. The twin serializer slots can stay per-style
(they serialize different payload families) — but they collapse into one *slot pattern* on the substrate if
desired; low value, low cost either way.

The future tax is the larger number — per pending roadmap item, on today's architecture versus the substrate:

| Pending effort | Today | On the substrate |
|---|---|---|
| Waiting sends | Built twice: once against the Tessaging router/peer registry, once against the Typermedia router with no peer memory to consult | Built once where the router and peer memory live |
| Readiness | Same twice-over, plus porting peer knowledge to Typermedia first | One awaitable over one peer registry |
| Typermedia peer knowledge (retired "increment 7") | A Typermedia-private peer-memory port | Free — the registry already remembers every peer's one advertisement |
| Decommission for typermedia routes | Unbuilt; today's `Decommission` covers Tessaging only | Covered by the same act — participants span styles |
| Distributed quiescence (question 6) | Two report sources, two aggregations | One administration surface |
| A third style (hosting-model.md's own recipe) | Step 2 of the recipe is "write your own distribution pipeline" — i.e. copy the router again | Contribute route predicates + request handlers + delivery semantics |
| A new transport | Already cheap (transport is shared) — unchanged | Unchanged |

The dynamic-topology effort already paid this tax once (the "Typermedia parity" batch of 2026-07-15 was
precisely re-implementing reconciliation for the second router); shrink/decommission is paying it now (built
for Tessaging, unbuilt for Typermedia); readiness would pay it a third time. That recurrence — every
cross-cutting effort ending in "now do it again for the other style" — is the architecture telling us the
seam is in the wrong place.

## Question 3 — Local, in-process, distributed

Settled in the design conversation and confirmed by the dig: **"in-process" and "local" are one concept —
same-endpoint scope — wearing two words, and "in-process" lies at its only distinguishing edge.** Two
co-located endpoints in one multi-endpoint host converse through the full distributed machinery (real
transport connections, discovery, the router — there is no same-host fast path anywhere in the code, verified),
never through the in-process tier. The tier's boundary is always the **endpoint**, never the process.

The structure underneath is right and already built the way the question hopes: each style's synchronous
core is its own feature (`InProcessTessagingEndpointFeature`, `InProcessTypermediaEndpointFeature`), and
distribution *composes on top of it*. [hosting-model.md](../../../../src/Compze.Hosting/dev_docs/hosting-model.md)
already says it out loud: "in-process Compze is container wiring, not hosting." So yes — **local is the core
programming model** (handlers, publishers, navigators, all in the caller's transaction against the
endpoint's own handlers), and distribution is the optional layer. Only the name lies.

Options:

- **(a) Rename the tier to local** — `AddLocalTessaging()`/`LocalTessaging()`,
  `AddLocalTypermedia()`/`LocalTypermedia()`, `LocalTessagingEndpointFeature`, etc. "In-process" dies
  everywhere (code, docs, prose). The word then agrees with `ILocalTypermediaNavigatorSession` and
  `IStrictlyLocalTessage`, which already use "local" for exactly this scope — one word, one concept, three
  surfaces of it.
- **(b) Dissolve the tier as a named thing** — fold the local core into the style's base registration with
  no `Add*` verb of its own. Rejected on the evidence: the tier is real (a container-only composition with
  no endpoint is a supported, documented use), it is the composition rung distribution builds on, and
  dissolving it removes a truthful concept to fix a wrong name.

**Recommendation: (a).** Mechanical, wholly beneficial, and independent of every other decision here — it
can ship first.

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
| Direct object handoff / loopback shortcuts between co-hosted endpoints | — | **None exist — honest.** Self-send rides the endpoint's own transport server; co-hosted endpoints discover each other through the real registry and converse over real connections. This is the strongest part of the design and the reason the consolidation is tractable |
| At-rest quiescence + background-exception rethrow | `TestingEndpointHost` dispose; the shared-by-reference `TessagesInFlightTracker` one host feature instance registers into every endpoint | **Test-only** — production resolves `NullOpTessagesInFlightTracker` everywhere; and the mechanism (one tracker object shared across endpoints' heaps) is *unbuildable* cross-process. Legitimate as a testing device; a production quiescence story must be built differently (question 6) |

**"Why must a Host create an endpoint at all?"** It mustn't. Endpoint creation needs the container factory
and the baseline registrations; the host's only irreducible contribution is driving the host-wide barrier —
and that barrier's cross-endpoint half is the pretense. For a single endpoint (Deskmancer's actual usage,
twice over) the host contributes nothing an endpoint could not do itself.

Options:

- **(a) Endpoint becomes the first-class unit; Host demotes to a convenience.** A free-standing composition
  entry point creates and runs one endpoint (what `RegisterEndpoint`'s four lines do today); a host is an
  optional aggregator that owns several endpoints' lifecycles in one process — a convenience, not
  infrastructure, and documented as such. The host-wide barrier weakens to what is honest everywhere:
  per-endpoint phase ordering plus announced-means-listening. Startup completeness — today the barrier's
  guarantee 3 — is carried instead by the mechanisms that already carry it cross-process: queue-while-down
  and `RequirePeers` for tessaging, readiness/waiting-sends for request/response.
- **(b) Keep the Host as the sole entry, keep the barrier, document it as in-process-only comfort.** Cheaper
  now; but it leaves two worlds (in-host strong ordering, cross-host convergence), keeps specs passing
  against guarantees production topologies don't have, and keeps the "why must a Host exist" question
  unanswered.

**Recommendation: (a) — with sequencing teeth.** The barrier cannot be honestly weakened until
readiness/waiting-sends exist, because in-host compositions and specs currently lean on guarantee 3 to avoid
the startup race that readiness is designed to absorb. That inverts nothing in the settled plan — it
*confirms* the order: substrate first, readiness/waiting-sends built once on it, then the Host demotion.
The Host is the last piece to move, not the first.

## Question 5 — The testing story

Not on trial: the testing host stays (settled). What the dig adds:

- The testing host's claim to honesty is largely real: every test runs the production announce/discover
  pipeline on a real `InterprocessEndpointRegistry`, real transports, no mocks. Its two in-process-only
  shortcuts are the cloned root container (shared DbPool/serializers — pure test economics, fine) and the
  shared-by-reference in-flight tracker.
- The at-rest wait ("a test cannot pass while work is in flight") is a *testing device* and can stay one —
  a shared heap is a legitimate testing convenience. When distributed quiescence (question 6) exists, the
  testing host gains a production-honest alternative: await the same quiescence report production
  administrators read, and keep the tracker only for what quiescence cannot see (background exceptions
  no assertion observed — which is a separate concern and could become one under its own name).
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
  two faces of production administration, siblings on the same administration surface, both properties of
  the substrate (they span styles by construction: a tuery in flight and a tevent in a queue both block
  quiescence).

This is a sketch to be designed properly in its own effort; it is recorded here because it is substrate
evidence: quiescence is unbuildable per-style (it must see *all* in-flight conversation) and unbuildable on
the shared-heap tracker (cross-process). It only has one possible home — the substrate.

## Question 7 — The guarantee vertical is substrate-level

Confirmed by the type hierarchy itself: the delivery-guarantee ladder is declared in the shared root —
`IAtMostOnceTessage` (which `IAtMostOnceTypermediaTommand` and `IExactlyOnceTessage` both extend),
`IAtLeastOnceTessage`, `IExactlyOnceTessage` — and spans the styles: at-most-once is a *typermedia* tommand
rung and an *exactly-once* building block at once. The transactionality markers
(`IMustBeSentTransactionally`, `ICannotBeSentRemotelyFromWithinTransaction`) are the same shape: substrate
vocabulary, honored by whichever style's machinery carries the tessage. The substrate owns the ladder and
its enforcement points (`TessageValidator`); the styles choose which rungs their message kinds occupy. The
consolidation should make this explicit rather than incidental.

## Question 8 — Ground truth

Hoisted to the top of the document (it grounds everything). One correction to keep on record: Deskmancer's
`AddTransientTessaging` is today's `AddDistributedTessaging` under its pre-rename package name — the
distributed (best-effort) tier has a real consumer; the surfaces with **zero** real consumers are the
in-process tier under its current name, `RequirePeers`, and multi-endpoint production hosting.

## Question 9 — Naming and migration

**The substrate's name.** `Compze.Internals.Transport` is the seed under a homeless name: it already owns
endpoint server lifecycle, address announcement, discovery, the request-handler seam, and the wire protocol —
"transport" undersells all of it, and "Internals" will become false the moment the substrate grows public
surface (`IPeerAdministration` is already public; readiness and quiescence will be). Candidates:

- **(a) `Compze.DistributedEndpoints`** — truthful: the substrate is exactly what a *distributed* endpoint
  is (an endpoint composing only local features never touches it). Public surface lives here; the wire-level
  internals can stay in `Compze.Internals.Transport` beneath it or fold in.
- **(b) `Compze.Endpoints`** — shorter, but overclaims: endpoints exist without distribution.
- **(c) Grow `Compze.Internals.Transport` in place, rename later** — defers the naming decision but keeps
  accreting public concepts under an `Internals` name; against the grain of names-first.

Recommendation: **(a)**, with the name explicitly Magnus's call. Two adjacent naming decisions ride along:
the `Compze.Abstractions.Tessaging.Public` namespace holding both styles' contracts (the parent-domain
"tessage" vs style-name "Tessaging" overload — long-flagged, this effort is the natural moment), and the
`internal/rpc/...` wire routes (greenfield; rename with the advertisement unification).

**Migration order — rehome concepts before restructuring, names and homes first, wiring second:**

1. **Names + deletions (no design risk, ship immediately):** "in-process" → "local" everywhere (question 3);
   delete the husk directories and the vestigial `Compze.Core` references; fix the two Typermedia defects
   (they are bugs today, independent of everything here); fix the stale hosting-model.md prose.
2. **The substrate gets its home:** create the package; move the advertisement to **one**
   `EndpointInformation` + one discovery query (the smallest cut with the highest coupling payoff); then the
   router — one reconcile/connection core with style-contributed route derivation and delivery-stream
   attachment; then peer memory, lifecycle observation, and `Decommission` down into it with style-supplied
   type-partition predicates (decommission thereby covers typermedia routes — closing that hole is the
   acceptance test that the move is real).
3. **Readiness + waiting sends, built once, on the substrate** — the increments already sketched in
   [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md), now with an unambiguous *where*.
4. **The Host demotion (question 4a), last:** endpoint first-class, host a convenience, barrier weakened to
   per-endpoint ordering — only after readiness exists to carry what the barrier pretended to guarantee.

**How long does this gate readiness/waiting-sends?** Steps 1–2 are the gate. Step 1 is mechanical. Step 2
is real design-and-build work, but each cut (advertisement, router, peers) ships green independently, and
the router consolidation *deletes* the second copy rather than adding a third thing. The alternative —
building readiness twice into the duplication now and consolidating later — pays the substrate cost anyway
*plus* a double readiness implementation *plus* the migration of both. The gate is worth its price.

## Recommendation summary (all decisions Magnus's)

1. Keep the styles' public models split; collapse the private substrate copies into one
   distributed-endpoint substrate (advertisement → router → peer memory, in that order).
2. Rename the in-process tier to **local**; "in-process" dies. Local is the core programming model;
   distribution is the optional layer — the code already says so, the names now agree.
3. Demote the Host to a multi-endpoint convenience over first-class endpoints, and weaken the host-wide
   barrier to per-endpoint ordering — **after** readiness/waiting-sends land.
4. Keep the testing host; let distributed quiescence eventually give specs (and production operators) an
   honest await; keep the in-heap tracker only as a testing device until then.
5. Sketch accepted for distributed quiescence as `IPeerAdministration`'s sibling: readiness = safe to start,
   quiescence = safe to stop; both substrate-level.
6. Name the substrate (`Compze.DistributedEndpoints` proposed); tease apart the `Abstractions.Tessaging`
   parent-domain namespace overload while at it.
7. Fix the two Typermedia defects and delete the debris now, independent of every decision above.

## Relation to the other effort documents

- [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md): its "gating open question" is answered
  by this document's recommendations; its increments acquire a *where* (the substrate) and two prerequisite
  steps (migration steps 1–2 above).
- [durable-peer-topology.md](durable-peer-topology.md): complete and unchanged; its peer memory,
  lifecycle observation, and decommission machinery are the pieces migration step 2 moves down into the
  substrate and generalizes across styles.
- [tessaging-WIP.md](tessaging-WIP.md): the hub; links this document.
