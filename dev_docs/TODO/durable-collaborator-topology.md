# Durable collaborator topology: endpoints remember whom they work with

**Status: design proposal, 2026-07-16. Nothing here is built.**

## The trigger

Investigating the "readiness probes" a Compze-consuming application was forced to hand-roll exposed two
holes, one much worse than the other:

1. **A silent data-loss hole inside the exactly-once claim.** Exactly-once tevent fan-out is decided by the
   live route tables at publish time (`Outbox.PublishTransactionally` →
   `ITessagingRouter.SubscriberConnectionsFor`). A subscriber that is down when the tevent is published —
   a routine rolling restart is enough — is simply absent from the persisted delivery set, and never
   receives the tevent. The durable *delivery* half is built right (undelivered rows wait in the outbox's
   storage and the recovery stream delivers them in send order when the endpoint returns), but the durable
   *membership* half does not exist: **nothing anywhere persists who the subscribers are.** The system
   remembers what it owes whom, but forgets who exists.
2. **A disastrous first-contact experience.** Every remote-facing send races discovery at startup: a
   typermedia tuery/tommand or an exactly-once tommand sent before the peer's advertisement is discovered
   explodes instantly (`NoHandlerForTypermediaTypeException` / `NoHandlerForTessageTypeException`), and a
   tevent published before the subscriber connects silently goes nowhere. (The readiness/waiting-sends
   proposal addresses the ergonomics of this; this document addresses the missing knowledge underneath.)

## The design error: two axes were conflated

- **Delivery guarantee** is a property of a *tessage in flight*: exactly-once (outbox → inbox, durable,
  deduped, retried) or best-effort (the transient delivery leg — no store, no dedup, no retry).
- **Relationship durability** is a property of an *endpoint pair*: what an endpoint knows about the
  collaborators it works with — that they exist, what they handle, what they subscribe to.

Today the code couples them: only the exactly-once composition has any persistence at all, and even it
persists nothing about relationships — all topology knowledge lives in in-memory route tables rebuilt from
live connections, erased by every restart and every membership change.

"Transient" is the name of a **delivery mechanism**. It leaked into the **relationship model**, where it is
wrong: two endpoints that communicate without exactly-once guarantees are not "transient" in their
relationship to each other — they depend on each other and typically know each other intimately.
Applications *care* which endpoints they communicate with and whether those are up, regardless of the
delivery tier. Endpoints whose relationships are genuinely disposable are the extreme edge case — bus
management/statistics infrastructure (current load, tessages in flight, …), not applications.

The Compze story for non-exactly-once endpoints is therefore **not** "forget all collaborators on every
restart and dump tevents into the void while they are down". Forgetting must be a visible, deliberate,
opt-in choice — never a default that falls out of which delivery legs happen to be wired.

## The concept: the collaborator registry

> An endpoint's **collaborators** are the endpoints it works with. The **collaborator registry** is the
> endpoint's own durable memory of them: each collaborator's identity (`EndpointId`) and its last-known
> advertisements — which tessage types it handles and subscribes to (Tessaging), and which typermedia types
> it serves (Typermedia). It survives restarts on both sides, and a collaborator leaves it only through
> explicit decommissioning — never through absence.

Sharply distinct from `IEndpointRegistry`, which stays exactly what it is: **live discovery** — who is
reachable *right now*, at which per-instance address. The two answer different questions:

| | `IEndpointRegistry` (discovery) | Collaborator registry |
|---|---|---|
| Question | Who is reachable now, where? | Whom do I work with, and what do they advertise? |
| Lifetime | Live; entries appear/disappear with announce/retract/liveness | Durable; entries persist until decommissioned |
| Scope | Shared across endpoints (e.g. interprocess) | Private to one endpoint |
| Content | Addresses (per-instance) | Identity + advertisements (never addresses — those are discovery's) |

The registry is written from the same flow that builds routes today: whenever a router connects to a
discovered endpoint and fetches its advertisement, it also upserts the collaborator's entry (first contact
creates it; every later fetch replaces the stored advertisement wholesale, so redeploys that add or remove
types are absorbed).

## Who reads it

1. **Exactly-once tevent fan-out — the data-loss fix.** `Outbox.PublishTransactionally` fans out to the
   *durable* subscriber set (every non-decommissioned collaborator whose stored advertisement matches the
   tevent's wrapper type), not to the live connections. Live connections decide only *when* delivery
   happens; the registry decides *who* gets a persisted delivery row. A subscriber that is down at publish
   time accumulates rows, and the existing recovery machinery — which was built for exactly this — delivers
   them on its return. The read happens inside the publish's transaction against the same database, so
   fan-out membership commits or rolls back with the tevent.
2. **Exactly-once tommand routing.** `Outbox.SendTransactionally` routes against the durable handled-types
   map: a tommand to a known-but-down handler endpoint persists and is delivered on its return, instead of
   exploding inside the caller's unit of work. (This composes with — or is subsumed by — the
   route-at-delivery-time outbox redesign sketched in the readiness discussion; see Open questions.)
3. **Startup.** On start the endpoint loads its collaborators and *knows what convergence means*: it can
   gate its readiness on reconnecting to the remembered set (bounded by a patience window), instead of
   every application hand-enumerating who it needs. This is what turns the startup story deterministic
   without probes.
4. **Non-exactly-once sends and publishes — informed behavior instead of silent racing.** With knowledge,
   the failure modes split into the two cases that deserve different treatment:
   - **Known but down**: a remembered collaborator serves this type but is not currently connected. A
     typermedia send waits for reconnection within its patience window. A best-effort tevent publish
     proceeds (that *is* the tier's delivery contract) but visibly — the miss is observable, never silent.
   - **Never seen**: no collaborator has ever advertised this type. Waiting is bounded by the first-contact
     patience; then the existing loud failure, whose message can now honestly say which case occurred.

## Where it persists

The registry's storage rides the pluggable SQL layer, like every other Compze persistence concern:

- **Exactly-once endpoints**: in the endpoint database, alongside the outbox/inbox tables. No new
  declaration — the fan-out read requires same-database transactional consistency anyway.
- **Non-exactly-once application endpoints**: also a database-backed registry — the natural default being a
  local SQLite file, which the pluggable SQL machinery already fully supports and which costs an endpoint
  nothing operationally. The composition surface decides the exact declaration shape (see Open questions).
- **Deliberately ephemeral endpoints** — the management/statistics edge case: an explicit opt-out
  declaration, named so the choice is visible in the composition (candidate words: ephemeral, forgetful —
  naming open). An endpoint that declares no storage and does not declare itself ephemeral fails loud at
  composition: forgetting is never an accident.

Registry writes happen on connection/advertisement events — outside any caller's unit of work — in their
own small transactions.

## Advertisement lifecycle

- **First contact**: connecting to a newly discovered endpoint creates its collaborator entry. Everything
  published *before* first contact is out of scope by definition — a subscription exists from when it is
  first known. This is standard at-publish pub/sub semantics; the readiness primitives cover the
  choreography where an application needs a collaborator before proceeding.
- **Update**: every advertisement fetch replaces the stored advertisement wholesale.
- **Shrink**: when a collaborator returns advertising *fewer* types, undelivered rows of the dropped types
  can no longer be delivered (the peer's inbox would refuse them loud). They must be surfaced loudly —
  never silently dropped. Exact surface open (see Open questions).
- **Absence is not a lifecycle event.** Crash, liveness pruning, clean stop with retraction: none of them
  touch the registry. They affect connections and delivery timing only.

## Decommissioning

The one way a collaborator leaves the registry, and it is an administrative act, not an inference:

- An explicit operation — working name `Decommission(EndpointId)` — on an endpoint management surface (this
  is precisely the kind of operation the future bus-management endpoints exist to expose).
- Decommissioning a collaborator with undelivered rows must be loud and deliberate: the operation reports
  what will be discarded, and discarding is part of the explicit act — never a side effect.
- A decommissioned endpoint that later re-announces is a first contact again.

## The startup story, restated end to end

- Within one host: the existing phase ordering (listening → announcing → sending) already guarantees
  everyone sees everyone. Unchanged.
- Across processes, exactly-once: durable membership means downtime and startup races cost nothing —
  tessages persist for known collaborators and deliver on (re)connection. Startup needs no choreography at
  all beyond first-contact.
- Across processes, non-exactly-once: the endpoint gates its readiness on its remembered collaborators
  (bounded patience, then proceed degraded — loudly); first-contact choreography uses the explicit
  readiness primitive from the readiness proposal. Sends wait within patience windows instead of exploding.
- The consuming application's hand-rolled "probes", and this repo's own 30-second retry loop in
  `Given_a_separate_process_hosting_a_transient_tessaging_endpoint_discovered_through_a_shared_interprocess_registry`,
  both dissolve.

## Relation to the readiness/waiting-sends proposal

The two efforts compose; neither replaces the other:

- The **collaborator registry** supplies durable knowledge (who exists, what they advertise).
- **Waiting sends + the readiness primitive** supply bounded patience where knowledge cannot help:
  first contact, and request/response calls to a peer that is down.
- Knowledge upgrades the waiting behavior from one-size-fits-all to the honest split: known-but-down
  (wait for reconnection / persist for delivery) vs never-seen (bounded first-contact patience, then fail
  loud naming the unserved type).

## Open questions (decisions to make before building)

1. **The word.** "Collaborator" / "collaborator registry" is proposed here (it is how the problem was
   stated: "the endpoints it works together with"). Alternatives: known endpoints, partners, peers.
   Whatever is chosen becomes ubiquitous-language and must not blur into `IEndpointRegistry` (discovery).
2. **Composition surface for non-exactly-once endpoints.** Does the transport-speaking core simply require
   a persistence declaration (SQLite file as the documented default), with `Ephemeral…` as the explicit
   opt-out? And does the "transient" *composition* name survive, now that it wires durable relationships +
   best-effort delivery — or does "transient" retreat to naming only the delivery leg, as it always should
   have?
3. **Typermedia scope.** Include typermedia advertisements in the registry from day one (the router gains
   known-but-down waiting), or land Tessaging first and follow?
4. **Exactly-once tommand routing**: patch `SendTransactionally` to route against the registry now, or fold
   it into the route-at-delivery-time outbox redesign (persist unbound, resolve the route per delivery
   attempt) and do that redesign as part of this effort?
5. **Advertisement-shrink surface**: where do stranded undelivered rows get reported (background-exception
   reporter? a management query? both), and is manual discard-after-inspection the resolution?
6. **Startup gating defaults**: is gate-on-remembered-collaborators opt-in per endpoint, or the default for
   non-exactly-once endpoints with a patience timeout? What is the degraded-proceed behavior when patience
   runs out?

## Specs that pin the behavior

The rolling-restart scenario is the headline; each line is a spec (or a small family):

- **Exactly-once tevent survives subscriber downtime**: publisher and subscriber exchange exactly-once
  tevents; subscriber stops cleanly (retracts); publisher publishes T1..Tn; subscriber restarts; it
  receives T1..Tn, in order, exactly once. The crash flavor (killed, liveness-pruned) pins the same
  outcome. (Cross-process via the SameMachine harness; an in-host variant needs per-endpoint stop/restart
  support in the testing host — to be checked.)
- **Exactly-once tevent survives publisher restart between publish and delivery** (already works via
  storage recovery — pin it explicitly).
- **Exactly-once tommand to a known-but-down handler**: send succeeds inside the caller's unit of work,
  delivery happens on the handler endpoint's return.
- **First contact is the boundary**: a subscriber the publisher has never seen receives nothing published
  before its first discovery — pinned as deliberate semantics, not an accident.
- **Advertisement shrink**: subscriber returns advertising fewer types; undelivered rows of dropped types
  are surfaced loudly and never delivered nor silently discarded.
- **Decommission**: explicit decommission removes the collaborator, reports discarded undelivered rows;
  publishes stop fanning out to it; a later re-announce is first contact.
- **Non-exactly-once endpoint remembers collaborators across its own restart**: registry reloaded, startup
  readiness observable, and a publish while a remembered subscriber is down is visible — the down
  subscriber does not receive it (the tier's contract), but nothing about the miss is silent.
- **Ephemeral opt-out**: an endpoint declaring itself ephemeral composes without storage and demonstrably
  forgets — and an endpoint with neither storage nor the declaration fails loud at composition.

## Increment plan (sketch)

1. The collaborator registry: schema, storage in the pluggable SQL layer, upsert-from-advertisement flow,
   loaded at endpoint start. Exactly-once composition only, no behavior change yet.
2. Exactly-once tevent fan-out reads the registry (the data-loss fix) + the rolling-restart specs.
3. Exactly-once tommand routing against the registry (or the route-at-delivery redesign, per open question 4).
4. Non-exactly-once endpoints: persistence declaration + ephemeral opt-out + startup gating.
5. Decommissioning surface + advertisement-shrink surfacing.
6. The readiness/waiting-sends work (separate proposal) lands before, after, or interleaved — it is
   independent until step 4, where known-but-down vs never-seen split the waiting behavior.
