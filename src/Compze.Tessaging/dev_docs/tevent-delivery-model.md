# The Compze tevent delivery model

This document takes a developer who is new to Compze from zero to understanding how tevents are delivered —
what delivery guarantees exist, where each guarantee comes from, how publishing and subscribing work, and
what happens when a tevent crosses a process boundary. It is the companion to
[the hosting model](../../Compze.Hosting/dev_docs/hosting-model.md), which explains what an endpoint *is*; this
document explains how tevents travel between the code that publishes them and the handlers that receive them.

Parts of this model are built and parts are decided-but-pending; the honest inventory is in
[Implementation status](#implementation-status) at the end. Everything before that describes the *model* —
the design the code is converging on, decided 2026-07-13.

## The big picture

### Tevents, briefly

A tevent is a type-routed event: subscribers subscribe to a tevent *type*, and receive every published
tevent whose runtime type is assignable to it — `IUserImported : IUserRegistered : IUserEvent` means an
`IUserEvent` subscriber hears about imports and registrations without either publisher or subscriber knowing
the other exists. Publishing is 1:N fan-out to unknown subscribers. (Tommands are the 1:1 opposite — one
sender, exactly one handler — and follow different rules; see [Tommands are different](#tommands-are-different).)

### The two orthogonal axes

Two independent questions decide how a tevent travels:

- **Remotability** — may this tevent leave the process at all?
- **Delivery guarantee** — what promise does its delivery make?

These are orthogonal by design. `IRemotableTevent` says only "may cross an endpoint boundary" and promises
nothing about how; the guarantees are separate markers (`IMustBeSentTransactionally`,
`IMustBeHandledTransactionally`) and tiers (`IAtMostOnceTessage`, `IExactlyOnceTessage`) layered on top.
Conflating the axes — assuming "crosses a boundary" implies "durable and transactional" — was the historical
mistake this model corrects.

### The one design rule

Everything below follows from a single rule:

> **A tevent's type states its delivery contract. The endpoint's wiring supplies the delivery legs. A
> subscription may only opt down — and only all the way.**

Unpacked:

- **The type is the contract.** Neither the publisher nor the subscriber picks a tevent's guarantee; the
  tevent's own interfaces declare it, and every delivery edge honors it. Publishing an `IExactlyOnceTevent`
  *means* exactly-once delivery to its default subscribers — there is no parameter that weakens it.
- **Wiring supplies the legs, and missing legs fail loud.** Which delivery mechanisms an endpoint has
  (outbox, inbox, transient transport) is composition. A tevent whose contract needs a leg the endpoint did
  not wire is a loud setup/publish error — never a silent downgrade. Silently degrading a durability
  guarantee is data loss dressed as success.
- **Subscriptions opt down binarily or not at all.** A subscription either takes the tevent's full declared
  guarantee (the default) or opts all the way out to *observation* (the escape hatch, below). There is no
  menu of intermediate levels — deliberately: the only reason to want less than the declared guarantee is to
  shed cost, and any intermediate tier (say, dedup-without-durability) still needs the inbox's store to
  dedup, so it saves nothing. Full guarantee and free observation span the entire cost/guarantee frontier.

## The delivery ladder

Every (tevent, subscriber) edge lands on one of four rungs. Each rung is the one above it minus exactly one
property:

| Rung | What it is | Loses vs. the rung above |
|---|---|---|
| **Participation** | In-process, synchronous, in the *publisher's* transaction. A handler failure aborts the publish itself. | — (the strongest thing there is) |
| **Exactly-once** | Durable (outbox → inbox), handler in its *own* transaction, deduped, retried until handled. | Participation in the publisher's transaction |
| **Transient** | Best-effort across the wire, handler in its *own* transaction. No store, no dedup, no retry. | Guaranteed arrival |
| **Observation** | Direct invocation, no transaction, no guarantees. | Transactional handling |

Which rung an edge lands on:

- **Participation** is automatic for every local subscriber — it is what in-process delivery *is*.
- **Exactly-once vs. transient** is decided by the tevent's type: `IExactlyOnceTevent` travels the durable
  pipeline; a remotable tevent that is not exactly-once travels transiently. The subscriber does not choose.
- **Observation** is the one subscription-side choice: the escape hatch drops any subscriber to this rung,
  whatever the tevent's type.

**Exactly-once membership is remembered, not live**: an exactly-once tevent's receivers are the peers whose
remembered advertisement subscribes to it (the peer registry — `dev_docs/TODO/durable-peer-topology.md` at
the repo root), never the connections that happen to be live at publish time. A subscribing peer that is
down when the tevent is published receives it on its return.

Note that **transient still means transactional handlers**. A projection updating from a transient tevent
wants its *own* writes to be atomic; that is handler correctness, not a delivery guarantee, and the two must
not be conflated. Transient delivery drops guaranteed *arrival*; the handler still executes in its own scope
and transaction (the same shape as Typermedia's handler execution, which also pairs no-delivery-guarantees
with transactional handler execution). Only observation runs without a transaction.

## The tevent type hierarchy: contracts, not mechanisms

Defined in `Compze.Abstractions` (`_TessageTypes..Interfaces.cs`):

- **`ITevent`** — a type-routed event. By itself: participation only; never leaves the process.
- **`IRemotableTevent : ITevent`** — may cross endpoint boundaries; promises nothing further. **This is the
  transient tier.** There is deliberately no `ITransientTevent` marker: "transient" names the delivery
  *mechanism*, and putting a mechanism's name into a tessage type would re-entangle the two axes the model
  keeps apart. A plain `IRemotableTevent` and "delivered transiently" are the same statement made once.
- **`IExactlyOnceTevent : IRemotableTevent, IExactlyOnceTessage`** — guaranteed arrival: sent
  transactionally through the outbox, handled transactionally through the inbox, deduped by its
  `IAtMostOnceTessage` `Id`, retried until handled.
- The markers underneath: `IMustBeSentTransactionally` (the send joins a transaction — requires the outbox),
  `IMustBeHandledTransactionally` (dedup/handling is atomic with the handler's effects — requires the
  inbox), and `IAtMostOnceTessage` (carries the `TessageId` infrastructure dedups on). At-most-once
  constrains only *handling*: an `IAtMostOnceTessage` may be *sent* best-effort, carrying its `Id`, and the
  receiver's dedup still guarantees ≤1 handling — the UI double-click case.

### The publisher-identifying wrapper carries identity, nothing else

Every tevent is wrapped in `IPublisherIdentifyingTevent<out TTevent>` before routing (routing matches on the
wrapper type; covariance makes a wrapper assignable to the wrapper-of every type its inner is assignable
to — see the routing model in `src/TODO/TODO_type-assignability-routing-and-publisher-identifying-tevents.md`).
The wrapper carries **only publisher identity**. It declares no delivery-guarantee interfaces of its own:

- The guarantee is read off the *inner* tevent through covariance — a wrapper whose inner is exactly-once is
  an `IPublisherIdentifyingTevent<IExactlyOnceTevent>`, and that is what the outbox and router demand.
- The dedup identity is the inner tevent's `Id`, extracted once where the type is statically known (the
  outbox entry) and carried as transport-envelope data from there on.

One concept, one home: the tevent owns its guarantee, the wrapper owns publisher identity, the envelope owns
the dedup key. (An earlier design mirrored the inner's guarantee interfaces onto parallel wrapper tiers;
the mirror could silently diverge and its constraints made a transient wrapper inexpressible. Removed
2026-07-13.)

## Publishing

There are two ways to publish, distinguished by the caller's relationship to a **unit of work** — one scope
paired with one ambient transaction, begun and completed together (see
`src/Compze.DependencyInjection/dev_docs/unit-of-work-model.md`). Code inside one publishes through the
unit-of-work publisher, which joins it; code outside any publishes through the independent publisher, which
gives each publish its own. The same duality repeats across the front doors:
`IUnitOfWorkTommandSender`/`IIndependentTommandSender` for sending tommands, and
`ISessionLocalTypermediaNavigator`/`IIndependentLocalTypermediaNavigator` for navigating the local
typermedia API.

### `IUnitOfWorkTeventPublisher` — publish within the caller's unit of work

```csharp
public interface IUnitOfWorkTeventPublisher
{
   void Publish(ITevent tevent);
}
```

Tevent-only, scoped, and the single fan-out point: it routes each tevent by the guarantee interfaces the
tevent's type declares:

- always → in-process synchronous delivery, inline in the caller's transaction (participation);
- `IExactlyOnceTevent` → also the outbox (durable, delivered on commit);
- `IRemotableTevent` but not `IExactlyOnceTevent` → also the transient transport (best-effort, delivered on
  commit);
- not `IRemotableTevent` → local only.

It requires and honors the ambient transaction. Required: publishing with none present throws — there is no
unit of work to publish within, and the independent publisher is the door for such callers. Honored: both
remote legs deliver on commit, so a rolled-back transaction never leaks a tevent — for the transient leg
that is "sent-on-commit without durability", not "sent transactionally". It auto-wraps a tevent published
without a publisher-identifying wrapper.

What this shape buys:

- **Anything can publish.** The tevent store is `IUnitOfWorkTeventPublisher`'s most common *client* — forwarding each
  committed taggregate tevent — not the owner of publishing. A service raising a transient monitoring
  signal, code with no taggregate in sight: same one interface.
- **Exactly-once is decoupled from the tevent store.** It requires only an ambient transaction (asserted)
  plus the outbox — the outbox row joins whatever transaction is present. Committing domain state and the
  tevent atomically is what the store *uses* this for, not what publishing *is*.
- **No publication mode.** Whether a tevent crosses the wire, and under which guarantee, is a property of
  the tevent's type — not an endpoint-wide mode declared once. An endpoint has one `IUnitOfWorkTeventPublisher`;
  which legs stand behind it is wiring, and a tevent needing an unwired leg fails loud.

### `IIndependentTeventPublisher` — publish as your own unit of work

The independent counterpart, for code that runs outside any unit of work — application code with no ambient
scope or transaction. A root-resolvable singleton, so a plain application class takes it as an ordinary
constructor dependency; each `Publish` runs as its own unit of work (`ExecuteUnitOfWork` around the
unit-of-work publisher), committed when the call returns.

Independence is asserted, not assumed — safety lives in asserts, not names: called from within an ambient
transaction it throws, because `TransactionScopeOption.Required` would silently *join* that transaction and
the publish would not stand alone. Inside a unit of work, publish through `IUnitOfWorkTeventPublisher`.

Before this door existed, every outside-the-pipeline caller hand-built the unit of work from container
primitives — `ExecuteInIsolatedScope(scope => scope.Resolve<ITeventPublisher>().Publish(fact))` — forcing
application code to speak container language to say one domain verb.

### No publish-side escape hatch, deliberately

There is no "publish ignoring my transaction" door. One existed briefly
(`ITransactionIgnoringTeventPublisher`, the ordinary publisher under ambient-transaction suppression); it
was deleted 2026-07-16: nothing ever consumed it — its imagined client, tracing/monitoring infrastructure
that must emit *now* regardless of the surrounding transaction's fate, never materialized — and with no
consumer to arbitrate, its correct semantics were contested (deliver in the caller's scope under suppression
vs. as its own unit of work; whether an `IMustBeSentTransactionally` tevent may pass at all). Keeping the
type would have implied a settled abstraction that did not exist.

A caller that genuinely needs the behavior composes it explicitly — suppress the ambient transaction and
publish through `IIndependentTeventPublisher`, whose no-ambient-transaction assert passes under
suppression — stating "this publish deliberately escapes my transaction's fate" at the call site. If a real
need materializes, its requirements design the abstraction.

## Subscribing

### Default: the tevent's declared guarantee

Registering a handler (`ForTevent<TTevent>(...)`) says only "this endpoint understands these tevents". The
delivery guarantee comes from each arriving tevent's type — the subscriber never picks it:

- a local publish → participation (synchronous, publisher's transaction);
- an arriving `IExactlyOnceTevent` → through the inbox: persisted, deduped, handled in its own transaction,
  retried until handled;
- an arriving transient tevent → direct dispatch in a unit of work of its own — atomic handler writes, but
  no dedup and no retry.

### `RegisterTransactionIgnoringTeventHandlers` — observation

The one subscription-side choice: opt a handler all the way down to observation — "observe regardless of
transactional fate". Where the default registration delivers under the arriving tevent type's full
guarantee, `RegisterTransactionIgnoringTeventHandlers` observes even if the transaction the tevent was
published or is processed in rolls back.

The observation contract, stated honestly — this is the fine print a subscriber accepts by using the escape
hatch, and it is why the escape hatch is for infrastructure, never domain logic:

- **Fires once, immediately, at registration of the tevent** — for an exactly-once arrival that is the
  moment the inbox registers it (after dedup, before transactional processing); for a transient arrival, on
  arrival; for a local publish, at publish time, outside the publisher's transaction.
- **At most once, never duplicates.** The exactly-once path dispatches observers only on first registration,
  so the inbox's dedup shields observers too; the transient path delivers at most once by nature.
- **May observe doomed tevents.** The transactional processing of an observed tevent may subsequently fail;
  a locally observed tevent's publishing transaction may roll back. The observer has already run.
- **Ordering relative to transactional handlers is unspecified.** Observation is dispatched immediately;
  the transactional handlers run when the inbox schedules them. Races are possible.
- **A throwing observer is reported, never retried** — surfaced through the background-exception reporter,
  not swallowed, not redelivered.

### Wiring rules — loud failure, one direction

- A subscription demanding *more* than the endpoint can deliver fails at setup: a subscription to an
  `IExactlyOnceTevent` — either kind — or an `IExactlyOnceTommand` handler on an endpoint with no inbox wired
  is an error. Observation is no exception, even though its dispatch needs no machinery: an observation
  subscription joins the advertisement, and observing a remote exactly-once tevent still requires *receiving*
  it exactly-once — the dedup shield the observation contract promises IS the inbox's. An endpoint that
  cannot honor a guarantee must not advertise for it.
- The reverse never fails: an endpoint with the full durable pipeline delivers a transient tevent
  transiently. Capability above the contract is simply unused.
- The dynamic remainder fails loud at receive: a *wide* subscription (say, to a plain `ITevent` interface)
  can match a remote publisher's exactly-once tevent no setup-time rule could see. Arriving exactly-once
  traffic on an endpoint without the inbox is refused — the request fails on the sender and the tessage stays
  safely undelivered in the sender's outbox — never silently downgraded.

## Ordering

> **Tevents sent to a given endpoint are delivered in the order they were sent. We never reorder what we
> send.**

The mechanism is structural, not bookkeeping: one tessage in flight per destination — a single-threaded send
loop that does not look at message N+1 until N is delivered and acknowledged. No sequence numbers are needed
while both processes stay up. Pipelining (multiple in flight) is deliberately not implemented: it is only
worth its complexity for high-latency links, and same-machine delivery is fast sequentially.

Per rung:

- **Exactly-once: order also survives a sender restart.** Recovery reloads the undelivered backlog in send
  order — the outbox tessage table's monotonic `GeneratedId` — re-establishing head-of-line on the oldest
  undelivered tessage.
- **Transient: in order within a connected session.** On disconnect or delivery failure, the remaining
  queued stream for that subscriber is dropped *whole* — the failed tevent and everything queued behind
  it — and the subscriber resumes from live: tevents published afterwards form a new stream, attempted
  normally. The unit of loss is the stream, never the message: a gap is one clean "you were disconnected"
  boundary, never a silent mid-stream skip that would deliver 54 after dropping 53. Any delivery failure
  triggers the drop — best-effort promises nothing that would justify retry machinery for a blip, and while
  an endpoint stays unreachable each new attempt fails and drops whatever queued since, which is exactly
  what best-effort means.
- **Receive side: failing to *receive* is fatal.** An endpoint that cannot register an arriving guaranteed
  tevent in its inbox has a fatal bug — the system goes down rather than lose the tevent. Failing to
  *handle* (handler exceptions, retries exhausted) is a separate concern with its own policy.

## Tommands are different

Tommands are 1:1 — one sender, exactly one handler — and there the type dictates *everything*, with no
subscription-side election at all: an `IExactlyOnceTommand`'s type is its delivery contract (exactly-once,
transactional, asynchronous), sent through `IUnitOfWorkTommandSender.Send`, and an endpoint even
routes tommands to *itself* through the outbox so the guarantee holds. A tommand binds to its one specific
receiver at send time — the live handler when one is connected, otherwise the sole remembered peer whose
advertisement handles the type (`dev_docs/TODO/durable-peer-topology.md` at the repo root) — so a
known-but-down handler receives the tommand on its return without the send exploding, while every tessage
between a sender and a receiver rides that pair's single ordered, receiver-deduped delivery stream:
exactly-once in-order holds by construction. An in-flight tommand therefore never delivers to a blue/green
successor — it waits for its own endpoint — while new sends bind to the live successor. The synchronous local ask has its own
truthful home in Typermedia's strictly-local tommand — see
[the hosting model](../../Compze.Hosting/dev_docs/hosting-model.md). The subscription-side opt-down and the
transient tier described in this document are tevent concepts: they exist because tevent publishing is
decoupled 1:N fan-out, where the publisher must not decide the durability needs of subscribers it does not
know.

## Implementation status

As of 2026-07-15:

**Built and verified:**

- The exactly-once vertical end to end: outbox, inbox, router, per-destination
  single-in-flight ordered delivery, dedup on the envelope `TessageId` — including recovery reloading a
  restarted sender's undelivered backlog in send order.
- In-process participation delivery, including the publisher-identifying wrapping and type-assignability
  routing.
- The pure `IPublisherIdentifyingTevent<out TTevent>` wrapper with guarantee-via-covariance and the
  envelope-carried dedup identity.
- Cross-process endpoint discovery and dynamic topology: endpoints announce their addresses into the
  same-machine `InterprocessEndpointRegistry`, the router continuously reconciles its connections with the
  registry's membership, and the story is proven across real OS processes over the named-pipe transport —
  see [same-machine hosting](../../Compze.Hosting/dev_docs/wip/same-machine-hosting.md).
- `IUnitOfWorkTeventPublisher` — the one public way to publish, routing each tevent by its declared contract
  (participation always; an `IExactlyOnceTevent` also through the exactly-once delivery leg when the
  composition wires one) — and the dissolution of the endpoint-wide publication-mode split (2026-07-14): the
  in-process Tessaging core registers the publisher, wiring the outbox is what wires the exactly-once leg,
  the tevent store forwards committed tevents through `IUnitOfWorkTeventPublisher` like any other client, and the mode
  publishers and their mutual exclusion are gone.
- The transient tier end to end (2026-07-14). The router routes every advertised remotable tevent
  subscription — the exactly-once-only gate is gone; matching stays pure wrapper-type assignability, and
  which leg a matched tevent travels is decided by the published tevent's own type, never by routing.
  `IUnitOfWorkTeventPublisher` routes a remotable-but-not-exactly-once tevent through the endpoint's transient
  delivery leg (`ITransientTeventDeliveryLeg`, wired by every transport-speaking Tessaging composition) — on commit when a
  transaction is present, immediately otherwise (the no-transaction branch is gone since 2026-07-16: the
  publisher asserts its ambient transaction, so transient delivery is always on-commit). Each subscriber
  connection carries an in-memory transient stream (`TransportRequestKind.TransientTevent` on the wire)
  delivering in order with the drop-stream-whole failure policy above, and the receiving endpoint dispatches
  an arriving transient tevent directly to its handlers in a unit of work of its own — no inbox, no dedup, no retry; a
  failed handling is reported through the background-exception reporter and the tevent is gone. The
  acknowledgement is written after the handlers execute, so single-in-flight keeps handling in send order.
  And with a second leg existing, the loud unwired-leg publish failure is real: an endpoint wiring any
  remote delivery but not the leg a tevent's contract demands fails the publish naming the missing leg
  (zero wired legs remains the deliberately local composition, where participation serves every subscriber
  that exists).
- The guarantee-free Tessaging composition on the database-less endpoint foundation (2026-07-15):
  `AddTransientTessaging(tessaging => tessaging.NewtonsoftSerializer())` — the transport-speaking Tessaging
  core, which the full distributed pipeline composes and extends. It wires the transport server, the router,
  and the transient delivery leg on the plain (database-less) `EndpointFoundation`: no outbox, no inbox, no
  SQL anywhere — the transient tier and participation are all the delivery there is. A connection carries one
  delivery stream per tier the endpoint wires (the in-memory transient stream always; the durable
  exactly-once stream exactly when the outbox's wiring grants the router its storage-backed stream factory),
  and the router's delivery lifecycle belongs to this core — the outbox's lifecycle is its storage. Proven
  across real OS processes with no database in either process
  (`Given_a_separate_process_hosting_a_transient_tessaging_endpoint_discovered_through_a_shared_interprocess_registry`).
- The setup-time wiring rule and the every-advertised-type-gets-a-route assert (2026-07-15). On an endpoint
  whose composition wires no exactly-once machinery, registering a handler for a tessage type that declares
  the exactly-once contract fails at setup, naming the types — observation subscriptions included, because an
  observation subscription joins the advertisement and observing a remote exactly-once tevent still requires
  receiving it exactly-once; advertising a subscription the endpoint cannot honor would pull exactly-once
  traffic it must refuse, stalling every sender's in-order delivery to it. And the advertisement asserts its
  own soundness at endpoint setup: every advertised type must be one the peers' routers can serve (tevent
  subscriptions as wrapper-of-remotable types; tommands exactly-once only — anything else fails loud instead
  of becoming a silently dead subscription), with the routers asserting the same contract again route by
  route. An arriving exactly-once tessage on an endpoint without the inbox is refused loud — the transport
  server serves no handler for the request kind — so the tessage stays undelivered on the sender, never
  silently downgraded; this is reachable through a wide subscription (say, to a plain `ITevent` interface)
  that a remote publisher's exactly-once tevent happens to match, which no setup-time rule can see.
- The transaction-ignoring subscription escape hatch (2026-07-15).
  `RegisterTransactionIgnoringTeventHandlers` (an endpoint-builder property, backed by
  `ITransactionIgnoringTeventHandlerRegistrar` — a separate registrar, so opting out of every guarantee is
  visible and off the common surface) registers observation handlers, dispatched by the observation
  dispatcher at every point a tevent is first registered: a local publish (at publish time), an
  exactly-once arrival (at inbox registration, after dedup — so the dedup shields observers — before
  transactional processing), a transient arrival (on arrival). Observers run in a fresh scope with any
  ambient transaction suppressed; a throwing observer is reported through the background-exception
  reporter, never retried, and never stops the remaining observers or the triggering publish/arrival. An
  observation subscription joins the endpoint's advertisement like any other — an observer-only endpoint
  still pulls the tevent across the wire. A publish-side counterpart (`ITransactionIgnoringTeventPublisher`,
  the ordinary publisher under ambient-transaction suppression) was built alongside it and deleted
  2026-07-16: nothing ever consumed it, and with no consumer to arbitrate its contested semantics, the type
  claimed a settled abstraction that did not exist — see "No publish-side escape hatch" above.

Everything this document describes is built. One refinement the guarantee-free composition forced on the
wiring rules as first written: "Observation is allowed anywhere — direct dispatch needs no machinery" holds
for every tevent an endpoint can actually encounter, but a *statically exactly-once* observation subscription
on an endpoint without the exactly-once machinery is rejected at setup like the default kind — it would join
the advertisement and pull exactly-once traffic the endpoint must refuse, and the dedup shield the observation
contract promises rides the inbox it does not have.
