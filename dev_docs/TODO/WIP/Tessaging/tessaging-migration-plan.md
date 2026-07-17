# The Tessaging migration plan: from today's code to the target design

**Status: plan, recorded 2026-07-17. Phases 1 and 2 executed 2026-07-17** (phase 1: debris, both Typermedia
defect fixes, the record→class conversion, the stale-prose sweep, the ServiceBus vocabulary's death;
phase 2: `Compze.Tessaging.Abstractions`, the Typermedia trio + its testing project, and
`Compze.Internals.Transport` folded into the paradigm assemblies; `Compze.Internals.Transport.AspNet`
renamed `Compze.Tessaging.AspNetCore`; the three typermedia spec projects merged into
`Compze.Tessaging.Specifications`). **Phase 3 executed 2026-07-17**: one `EndpointInformation` +
`EndpointInformationQuery` (advertisement unioned from per-style `IEndpointAdvertisementContributor`
contributions — an interim seam the roster replaces); the `TessagingRouter` routes all four kinds and the
endpoint's `ITypermediaRouting` rides it; the reconcile-loop twin died — `TypermediaClientRouter` keeps only
the explicit-connect client shape; `DistributedTypermediaEndpointFeature` composes the distributed Tessaging
core; peer memory remembers the one advertisement, typermedia types included, so decommission covers them by
construction (pinned in `Peer_registry_tests`). The destination is
[tessaging-target-design.md](tessaging-target-design.md); the rationale and evidence are in
[style-substrate-and-hosting-evaluation.md](style-substrate-and-hosting-evaluation.md). This document is the
path: the ordered phases, what each contains, and what gates what. Every phase is a run of increments that
each build clean, pass the full test suite, and are committed with a message recording the why.

## The ordering principle

Re-home concepts before restructuring — truthful names and homes first, wiring second. Applied here:

1. **Debris, defects, and truthful names first** (no design risk).
2. **Then the homes** (the project moves), so everything after is born where it belongs.
3. **Then unify the workers** — router, advertisement, peer memory — which survive unchanged into the
   target, so nothing is built twice.
4. **Then replace the plumbing** — the LocalTessagingEngine and the concrete endpoint types, deleting the
   feature machinery as they land.
5. **Only then the behavior changes that need the new structure** — inline in-roster tommands, the
   observation redesign, synchrony-follows-the-type, the storage model.
6. **Readiness before storage** (it carries the ⚖ before-the-next-release commitment inherited from
   "Typermedia parity"); **the Host demotion last** (only after readiness exists to carry what the host-wide
   barrier pretended to guarantee).

## ⚖ The project homes (evaluation question 9 — settled 2026-07-17)

**Option (a) — one paradigm project — with the transport-weight exception:**

- **`Compze.Tessaging` is the paradigm project**: the LocalTessagingEngine, the concrete endpoint types, and
  both siblings' machinery as namespaces within it. The named-pipe transport folds in — `System.IO.Pipes`
  is base runtime, no dependency weight. `Compze.Typermedia`, `Compze.Typermedia.Client`,
  `Compze.Typermedia.Hosting`, `Compze.Tessaging.Abstractions`, and `Compze.Internals.Transport` fold into
  it.
- **The ASP.NET Core transport stays its own project** (working name `Compze.Tessaging.AspNetCore`):
  folding it in would drag the web stack into every consumer, and the consumer that matters wants the light
  stack.
- **The four SQL backend projects keep their shape** (`Compze.Tessaging.Sqlite` and kin).
- Testing projects fold correspondingly. Exact final names for the folded projects are decided at move time,
  inside the settled shape.

## What the plan rests on (verified in the code, 2026-07-17)

- **Tevents already obey the consistency law.** `Outbox.PublishTransactionally` excludes self from fan-out;
  an endpoint's own subscription to its own exactly-once tevent is served by participation only. The only
  self-addressed wire traffic is the exactly-once tommand path (`Outbox.SendTransactionally` →
  self-connection → own transport server → own inbox), so deleting the self-connection is a tommand-path
  change plus one spec, not a broad rewiring.
- **Every handler surface today is synchronous** — `Action`/`Func` only, and the doors are `void Publish` /
  `void Send`. Synchrony-follows-the-type is new machinery, not a rename sweep.
- **Observation today is inline on the caller's thread and spec-pinned to fire even when the publishing
  transaction rolls back.** The target's committed-facts-only, off-thread contract deliberately inverts that
  pin.
- **The endpoint catalog is a wholly new table** — nothing anywhere persists an endpoint's own identity
  today.
- **The one executor has exactly five donors**: `InProcessTeventPublisher`, the `Inbox` handler-execution
  engine, `BestEffortTeventDirectDispatcher`, `TypermediaHandlerExecutor`, `TeventObservationDispatcher`.

## Phase 1 — Debris, defects, stale prose, safe renames *(mechanical, no design risk)*

1. Delete the nine husk directories (seven `Compze.ServiceBus*`, `Compze.Tessaging.Hosting.AspNetCore`,
   `Compze.Typermedia.Hosting.AspNetCore`) and the vestigial `Compze.Core` FlexRef blocks in the six csprojs
   (a stale-nupkg hazard), plus the two stale `// Compze.Core` comments.
2. Fix the two Typermedia defects: `TypermediaRouter.RebuildRouteTables`'s `Dictionary.Add` crash-loop
   becomes the TessageBus policy (a duplicate route is a diagnosable send-time condition, decommission the
   remedy); `TypermediaHandlerRegistry`'s raw indexers become a proper no-handler exception, and
   `ExecuteWithRetry` stops retrying programming-error-shaped failures.
3. `EndpointAddress` record → class.
4. Stale-prose sweep: `hosting-model.md` (the nonexistent "scheduler", the pre-rename navigator name),
   `unit-of-work-model.md` ("transient tevent dispatch"), and every file still pointing at the old
   `dev_docs/TODO/durable-peer-topology.md` path.
5. The ServiceBus-remnant renames out of the borrowed jargon: `IServiceBusSqlLayer` and the
   `ServiceBusSpecification` test folders.
6. "In-process" → "local" as vocabulary — applied only where the word is conceptual; prose that truthfully
   describes the current `InProcess*` classes keeps their names, and the doomed classes themselves die under
   their old names in phase 5 (renaming corpses is churn).

## Phase 2 — Move the projects into the settled homes

The folds and renames from the ⚖ homes decision above, as their own mechanical green increment
(`C-Merge-Project`/`C-Rename-Project` plus the Rider refactoring engine), so everything after is born in its
home.

## Phase 3 — One advertisement, one discovery query, one router, peers across all kinds

Unify the workers in place, under the existing feature wiring — they survive unchanged into the endpoint
types, so nothing here is done twice: merge the two structurally identical endpoint-information payloads and
discovery queries into one; `TessagingRouter` absorbs the typermedia routes (exact-type single-handler kinds
beside the tevent-assignability routes — the only style-typed difference that exists); `TypermediaRouter` is
deleted; the peer registry remembers the one advertisement per peer. **The acceptance test that the
harmonization is real: `Decommission` covers typermedia routes.** This also dissolves the "Typermedia
parity" debt permanently and puts peer knowledge exactly where readiness will need it.

## Phase 4 — The LocalTessagingEngine

The heart of the migration, several increments:

1. **The `TessageHandlerRoster`** — one immutable map for all four kinds, merging `TessageHandlerRegistry`
   and `TypermediaHandlerRegistry`; the advertisement becomes its pure projection, computed once; a second
   handler for a single-handler type explodes at build.
2. **The one executor** — replacing the five donors' choreography: a tevent's response is all compatible
   handlers in one unit of work, a tommand's its single handler in a unit of work, a tuery's its single
   handler in a scope; one no-handler policy. Written **async-capable from birth** — it is a new class,
   written right once — with the sync strictly-local path staying genuinely synchronous.
3. **The builder** — `LocalTessagingEngineBuilder` with the settled registrar idiom
   (`Builder RegisterTessageHandlers(Action<TessageHandlerRegistrar> register)`, `MapTypes`,
   `ObserveTevents`), replacing the `InProcessTessaging()`/`InProcessTypermedia()` container extensions and
   the `RegisterTessagingHandlers`/`RegisterTypermediaHandlers` extension properties; one engine per
   container, a second explodes. **The registrar is born with the target handler signatures** — async-only
   verbs for exactly-once kinds, sync first-class for strictly-local — so the one big spec migration (every
   registration site moves to the declaration block anyway) lands on the final shapes and never moves again.
4. The old endpoint features are adapted to compose the engine (a thin shim, deleted in phase 5), so the
   suite stays green throughout. Observation semantics are carried over unchanged in this phase — the
   redesign is its own phase, so each behavior change is its own commit.

## Phase 5 — Concrete endpoint types; the feature machinery dies

The best-effort endpoint and the exactly-once endpoint as plain composition roots wrapping an engine plus
identity and wire, serving all four kinds unconditionally, each driving its own lifecycle phases in readable
methods; the pure client as the third shape. `GetOrAddFeature`, the contribution seams, the
`IfNotRegistered` guards, the twin serializer slots, `OnContainerBuilt`, the `IEndpointComponent` seam, and
the `ITestingEndpointHostFeature` seam all die; testing becomes concrete per-tier wiring (the tracker handed
in at construction).

**Into this phase lands the consistency law's behavior change:** the tommand-sender door consults the
roster — in-roster executes inline through the engine's executor, in the sender's execution; the
self-connection is deleted from the router. The exactly-once-in-order walk is short and by construction: an
inline tommand never touches the wire, so there is no stream to order and no dedup to need. The dedicated
self-send spec changes meaning deliberately — from "delivered through its own inbox" to "executes inline in
the sender's unit of work".

## Phase 6 — Observation redesigned: committed facts, off-thread

Queue observed tevents at commit (`IUnitOfWorkResolver.OnCommittedSuccessfully` is the hook), dispatch
off-thread with per-observer FIFO, failures to the background-exception reporter, and the testing at-rest
wait extended to cover the observation queues (today the in-flight tracker does not see observation at all —
it is only transitively covered by inline dispatch, which stops being true the moment dispatch goes
off-thread). The fires-even-on-rollback spec inverts — that is the point.

## Phase 7 — Synchrony follows the type: the doors

The surface sweep the async-native executor makes cheap: the exactly-once doors go async
(`IUnitOfWorkTeventPublisher`/`IIndependentTeventPublisher`, the tommand senders — outbox writes are
database I/O), remote typermedia async-first, administration async, strictly-local doors keeping sync
first-class. One design detail settled at build time: whether the shared tevent-publisher door becomes one
async method or a sync/async pair (it serves both strictly-local and exactly-once tevents through one
interface).

## Phase 8 — Readiness and waiting sends, built once

The four increments already sketched in [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md),
now with their unambiguous *where* (the one router and one peer registry from phase 3). Before the storage
phase because it carries the ⚖ before-the-next-release commitment, and it is what triggered this whole arc.

## Phase 9 — Storage: the database belongs to the domain

Per-endpoint prefixed table-sets across all four backends (the shared table-name constants and per-backend
`Schema.cs` DDL make this well-localized); the endpoint catalog table (name uniqueness asserted loud,
`EndpointId`, the process lease — one process per endpoint, a conflict fails loud at startup); the type-id
interner shared per domain database (on SQLite this also retires the separate per-endpoint interner database
file); decommissioning an endpoint's storage = dropping its set; the foundation declaration reworded from
"the endpoint's database" to "the domain database this endpoint joins". New specs: several endpoints
conversing exactly-once inside one domain database; the lease conflict. The exact prefix convention is the
deliberately-unsettled implementation-time decision — settled here.

## Phase 10 — The Host demotion *(last)*

Only after readiness exists to carry what the host-wide barrier pretended to guarantee: endpoint types
first-class, `EndpointHost` a `WhenAll` convenience, the barrier weakened to per-endpoint phase ordering,
and the specs that implicitly rode first-look-sees-all migrated to explicit awaits (readiness,
`RequirePeers` pens, the read-counting registry idiom).

## Rolling throughout

[hosting-model.md](../../../../src/Compze.Hosting/dev_docs/hosting-model.md),
[tevent-delivery-model.md](../../../../src/Compze.Tessaging/dev_docs/tevent-delivery-model.md), and
[same-machine-hosting.md](../../../../src/Compze.Hosting/dev_docs/wip/same-machine-hosting.md) are rewritten
as the machinery they describe changes — never left lying — with a final coherence sweep at the end.

## Size and risk, honestly

Phases 1–3 are days-scale and low-risk. Phases 4–5 are the big ones — every handler-registration site in
the suite migrates in phase 4, and the endpoint types in phase 5 touch every hosting spec. Phase 7's async
depth in the inbox, and the SQLite write-gate-across-awaits interaction, is the least sizable piece from
here. Everything else is design already ⚖-settled in the target document.

## Related documents

- [tessaging-target-design.md](tessaging-target-design.md) — the destination.
- [style-substrate-and-hosting-evaluation.md](style-substrate-and-hosting-evaluation.md) — the rationale
  and evidence; question 9's option space, now settled above.
- [readiness-and-waiting-sends.md](readiness-and-waiting-sends.md) — phase 8's design, settled.
- [tessaging-WIP.md](tessaging-WIP.md) — the hub.
