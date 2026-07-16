# Durable peer topology: endpoints remember whom they work with

**Status: design settled 2026-07-16, implementation in progress.** The open questions from the first draft
are decided (decisions inline, marked ⚖); this document is the effort's single home.

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
   tevent published before the subscriber connects silently goes nowhere.

## The design error: two axes were conflated

- **Delivery guarantee** is a property of a *tessage in flight*: exactly-once (outbox → inbox, durable,
  deduped, retried) or best-effort (what can be achieved without transactions, storage, and dedup).
- **Relationship durability** is a property of an *endpoint pair*: what an endpoint knows about the peers
  it works with — that they exist, what they handle, what they subscribe to.

The old code coupled them: only the exactly-once composition had any persistence at all, and even it
persisted nothing about relationships — all topology knowledge lived in in-memory route tables rebuilt from
live connections, erased by every restart and every membership change.

"Transient" named a **delivery mechanism**, and it leaked into the **relationship model**, where it is
wrong: two endpoints that communicate without exactly-once guarantees are not "transient" in their
relationship to each other — they depend on each other and typically know each other intimately.
Applications *care* which endpoints they communicate with and whether those are up, regardless of the
delivery tier. Endpoints whose relationships are genuinely disposable are the extreme edge case — bus
management/statistics infrastructure, not applications.

The governing principle, in one line: **Do not lose information unless it cannot be avoided. Dropping to a
lower guarantee is opt-in, never a default.**

## ⚖ Vocabulary (settled)

- **Peer** — another endpoint, as seen from this one: the unit of identity (`EndpointId`), lifecycle
  (first contact → advertisement updates → decommissioning), and expectation (`RequiredPeers`).
  *Persisted subscriptions* (tevent side) and *persisted routes* (tommand side) are the plain names of the
  facts recorded about a peer; "peer" names what they belong to. Sharply distinct from `IEndpointRegistry`,
  which stays pure live discovery — who is reachable *right now*, at which per-instance address.
- **Distributed** — the middle composition tier returns to its true name: `in-process ⊂ distributed ⊂
  exactly-once`. The distributed tier is where distribution *lives* (transport, discovery, router);
  exactly-once is distributed plus the guarantee vertical. The composition formerly named
  `AddTransientTessaging` becomes `AddDistributedTessaging` (exact surface naming settled during the
  rename increment).
- **Best-effort** — the delivery rung and leg formerly named "transient": the best delivery that can be
  achieved without transactions, storage, and dedup. Under this design that is genuinely *best* effort —
  in-order while connected, queued in memory while a known peer is down — not fire-and-forget.
  "Transient" dies as a name everywhere.

## The two tiers keep their knowledge in the medium they already have

- **Exactly-once endpoints** (always database-backed) **learn and persist**: a durable peer registry —
  persisted subscriptions and persisted routes per peer — written from the same flow that builds routes
  today (every advertisement fetch upserts the peer's entry, replacing its stored advertisement wholesale).
  Survives restarts on both sides.
- **Distributed (non-exactly-once) endpoints** (no database, by design) **declare**: ⚖ the composition
  declares the endpoint's `RequiredPeers` **by identity — the peer's `EndpointId`**. The declaration *is*
  the durable memory: it survives restarts by being code/config, so the tier needs no storage at all.
  Within a process's lifetime the endpoint additionally learns peers it meets (in memory), and treats them
  the same while it lives.

## What the knowledge drives

### Exactly-once: membership stops depending on liveness

1. **Tevent fan-out reads the persisted subscriptions** — every non-decommissioned peer whose stored
   subscription matches the tevent's wrapper type gets a persisted delivery row, connected or not. Live
   connections decide only *when* delivery happens. The read happens inside the publish's transaction
   against the same database, so fan-out membership commits or rolls back with the tevent. This closes the
   rolling-restart data-loss hole; the delivery/recovery machinery downstream already handles the rest.
2. ⚖ **Tommands route at delivery time, not send time** (alternative B, settled). The outbox persists the
   tommand **unbound** — no endpoint id; a tommand semantically names no recipient, it means "the handler
   of T, whoever that is", and binding at send time froze an accident of timing into durable state.
   Send-time validates only that the type is served by some persisted route (else the loud failure);
   the delivery side resolves the route per attempt, so a known-but-down handler receives the tommand on
   return, and a *replaced* handler (blue/green: new deployment, new `EndpointId` advertising the same
   tommand types) receives in-flight tommands sent to its predecessor. Per-destination send order is
   preserved by binding in `GeneratedId` order.

### Distributed: queue while down — best-effort means best

3. **Everything queues, in memory and in order, while a known peer is down.** Not-yet-sent tessages are
   safe to hold — no duplication risk; they wait. The old drop-stream-whole failure policy softens to
   pause-stream-whole: a delivery failure stops the stream, the remainder stays queued in order, draining
   resumes on reconnect. The only intrinsically ambiguous tessage is the one in flight at the failure
   moment (without dedup, re-sending risks a duplicate; it is dropped, loudly). The tier's loss surface
   shrinks to: that single in-flight tessage, publisher crash (memory is memory), and queue overflow.
4. **A required peer not yet met queues everything** published until its first advertisement arrives; the
   matching subset then delivers in order. With several pending required peers, tevents are held until
   every one is known.
5. ⚖ **The queue bound is size-based and generous: 10,000 tevents per peer** (a lot of tevents, little
   memory on current hardware, unless the application is extremely prolific). ⚖ **Overflow fails the
   publish loud** — backpressure naming the peer and the bound — because failing loud loses nothing while
   shedding does; shedding-oldest-with-reporting can become a per-peer opt-in later. *(The overflow default
   is the implementer's judgment call under the never-lose principle — flip to shedding if preferred.)*
6. **Opt-down is per-peer, not per-endpoint**: ephemerality is a property of the *relationship*. An
   application endpoint requires Billing but genuinely does not care whether the stats collector is up —
   that is a per-peer best-effort-no-queueing opt-in. The freestanding "ephemeral endpoint" declaration
   from the first draft dissolves into this.
7. **Request/response is excluded from queueing**: tueries and typermedia tommands have a caller awaiting a
   result — they get bounded-patience waiting (the readiness/waiting-sends effort), never a queue.

## Advertisement lifecycle

- **First contact**: connecting to a newly discovered endpoint creates its peer entry. Everything published
  before first contact is out of scope by definition — a subscription exists from when it is first known —
  *except* tevents held for a `RequiredPeers` declaration, which exist precisely to bridge first contact.
- **Update**: every advertisement fetch replaces the stored advertisement wholesale.
- ⚖ **Shrink** (settled): a shrunk advertisement is the peer's own explicit declaration — an unsubscribe by
  the subscription's owner, nothing like absence. Persisted subscriptions/routes of the dropped types are
  pruned going forward. Already-persisted undelivered rows split by kind:
  - **Tevents**: the subscriber renounced interest — the undelivered tevents lost their audience by that
    audience's own choice. Dropped, **with loud reporting** (count and types), never silently.
  - **Tommands**: someone commanded an action that now has no handler — almost certainly a deployment
    error. Route-at-delivery partly self-heals it: a successor advertising the type receives the tommand.
    A tommand whose type nobody serves anymore is stranded loudly and resolved by explicit discard or a
    successor's arrival.
- **Absence is not a lifecycle event.** Crash, liveness pruning, clean stop with retraction: none of them
  touch peer knowledge. They affect connections and delivery timing only.

## Decommissioning

The one way a peer leaves the registry; an administrative act, never an inference:

- An explicit operation — working name `Decommission(EndpointId)` — on an endpoint management surface (the
  kind of operation the future bus-management endpoints exist to expose).
- Decommissioning a peer with undelivered rows is loud and deliberate: the operation reports what will be
  discarded; discarding is part of the explicit act, never a side effect.
- A decommissioned peer that later re-announces is a first contact again.

## The startup story, restated end to end

- Within one host: the existing phase ordering (listening → announcing → sending) already guarantees
  everyone sees everyone. Unchanged.
- Across processes, exactly-once: durable membership means downtime and startup races cost nothing —
  tessages persist for known peers and deliver on (re)connection. No choreography beyond first contact.
- Across processes, distributed: `RequiredPeers` + queue-before-first-contact make startup deterministic —
  nothing a required peer should see is lost to the discovery race, and nothing needs probing.
- The consuming application's hand-rolled "probes", and this repo's own 30-second retry loop in
  `Given_a_separate_process_hosting_a_transient_tessaging_endpoint_discovered_through_a_shared_interprocess_registry`,
  both dissolve.

## Relation to the readiness/waiting-sends effort

Independent and composing: that effort adds bounded route patience for request/response calls and an
explicit readiness awaitable. Peer knowledge upgrades its behavior from one-size-fits-all to the honest
split — known-but-down (wait for reconnection / queue / persist) vs never-seen (bounded first-contact
patience, then fail loud naming the unserved type).

## Remaining scope decisions

- ⚖ **Typermedia parity lands before the next release** (settled). Sequencing may put Tessaging first.
- Typermedia's registry entries (persisted routes for typermedia types on exactly-once endpoints;
  known-peer waiting on distributed ones) follow the same model; details when that increment starts.

## Specs that pin the behavior

- **Exactly-once tevent survives subscriber downtime**: publisher and subscriber exchange exactly-once
  tevents; subscriber stops cleanly (retracts); publisher publishes T1..Tn; subscriber restarts; it
  receives T1..Tn, in order, exactly once. The crash flavor (killed, liveness-pruned) pins the same
  outcome.
- **Exactly-once tevent survives publisher restart between publish and delivery** (already works via
  storage recovery — pinned explicitly).
- **Exactly-once tommand to a known-but-down handler**: send succeeds inside the caller's unit of work;
  delivery happens on the handler endpoint's return.
- **Exactly-once tommand survives handler replacement**: handler endpoint retires; a successor with a new
  `EndpointId` advertises the same tommand type; in-flight tommands deliver to the successor.
- **First contact is the boundary** (exactly-once): a subscriber the publisher has never seen receives
  nothing published before its first discovery — deliberate semantics, pinned.
- **Advertisement shrink**: subscriber returns advertising fewer types; persisted subscriptions prune;
  undelivered tevents of dropped types are dropped with loud reporting; undelivered tommands strand loudly
  or deliver to a successor.
- **Decommission**: explicit decommission removes the peer, reports discarded undelivered rows; publishes
  stop fanning out to it; a later re-announce is first contact.
- **Distributed queue-while-down**: peer met, then down; published tevents queue in order; peer returns;
  everything delivers in order; only a tessage in flight at a failure moment may be lost, loudly.
- **Distributed required peer bridges first contact**: endpoint starts with `RequiredPeers = [B]`; tevents
  published before B's first advertisement are held and delivered to B, in order, on its arrival.
- **The bound**: the 10,001st queued tevent for a down peer fails the publish loud, naming the peer and
  the bound.
- **Per-peer opt-down**: a peer declared best-effort-no-queueing gets today's behavior — and nothing else
  does.

## Increment plan

1. **Exactly-once peer registry**: persisted subscriptions + persisted routes in the Tessaging SQL layer
   (all backends), upserted from every advertisement fetch, loaded at endpoint start. No behavior change.
2. **Fan-out reads the registry** — the data-loss fix — plus the rolling-restart/downtime specs.
3. **Tommand route-at-delivery**: unbound outbox rows, bind-per-attempt, known-but-down and
   handler-replacement specs.
4. **The rename**: transient → distributed (composition) / best-effort (delivery rung + leg), everywhere —
   code, specs, docs, ubiquitous language.
5. **Distributed tier**: `RequiredPeers` (by `EndpointId`), queue-while-down with pause-stream-whole,
   queue-before-first-contact, the 10,000 bound with loud overflow, per-peer opt-down.
6. **Shrink + decommission surfaces.**
7. **Typermedia parity** (before the next release).
