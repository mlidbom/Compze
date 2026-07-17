# Readiness and waiting sends: request/response stops racing discovery

**Status: IMPLEMENTED 2026-07-17 — migration phase 8, four green commits (one per increment below).** The
gating style-substrate question resolved in this effort's favor before building: the substrate collapsed
into one paradigm project during migration phases 2–5 (see
[style-substrate-and-hosting-evaluation.md](style-substrate-and-hosting-evaluation.md) and
[tessaging-migration-plan.md](../WIP/tessaging-migration-plan.md)), so readiness and waiting were built exactly
once, on the one router and one peer memory. The ⚖ *lands before the next release* commitment inherited
from the durable peer topology's "Typermedia parity" item is discharged. The design below stands as built;
the as-built vocabulary: the patience is the endpoint's **handler-availability patience**
(`EndpointBuilder.HandlerAvailabilityPatience`, flat 30s default per ⚖ 2), the waiting mechanism is
`IHandlerAvailability`, readiness is `IEndpoint.AwaitReadinessAsync(ReadinessTypes, patience?)` with the
`ReadinessTypes` reflection factories of ⚖ 1, and the patience-exhausted failure for readiness is
`EndpointNotReadyWithinPatienceException`.

## The trigger (shared with the durable peer topology)

Investigating the "readiness probes" a Compze-consuming application was forced to hand-roll exposed two
holes. The durable peer topology closed the first (durable membership). This effort is the second:

> **A disastrous first-contact experience.** Every remote-facing request/response send races discovery at
> startup: a typermedia tuery or tommand — or an exactly-once tommand whose handler was never met — sent
> before the peer's advertisement is discovered explodes instantly
> (`NoHandlerForTypermediaTypeException` / `NoHandlerForTessageTypeException`).

## Why this is its own effort: nothing here can be deferred

The peer topology fixed one-way tessages by *deferring delivery*: rows and queues wait for the peer's
return, because no one is waiting on a result. Request/response cannot defer — a caller is synchronously
awaiting an answer, and only a live handler can ever produce one. Parking a tuery in a queue for hours would
be lying to that caller. The only levers that exist for request/response are:

1. **How long a send waits** before giving up.
2. **What the failure says** when it gives up.
3. **What an application can await** before sending at all.

Those three levers are this effort: (1) and (2) are *waiting sends*, (3) is *readiness*.

## The two mechanisms

### Waiting sends — implicit, per-call, bounded patience

A send whose type has no live route right now does not explode. It waits — bounded — for the route to
appear (a first contact) or for the known handler peer to reconnect, then proceeds normally; when its
patience runs out it fails loud, naming the unserved type and what was waited for. Every caller gets this
with no code changes, and it is what absorbs *steady-state churn*: a handler endpoint restarting mid-day
creates a seconds-wide window with no route, and calls in that window wait it out instead of erroring.

### Readiness — an explicit awaitable

An application awaits, at a moment of its own choosing: *"complete when this endpoint can reach handlers
for these types / when its required peers are met."* The framework-native replacement for the hand-rolled
probes that triggered everything — the name is literal.

What readiness buys that waiting sends cannot:

- **Who pays the wait.** With waiting sends alone, the startup discovery race is paid by the first unlucky
  caller — typically a user request. Readiness front-loads the wait to startup, before traffic opens.
- **Operations integration.** Readiness is what an orchestrator's readiness probe wires to: "do not route
  traffic to me until I can serve." A per-call mechanism cannot express endpoint-level "I am ready."
- **Fail-fast policy.** "Not ready within 30 seconds → abort startup" surfaces a misdeployment once, at
  boot — instead of as every call timing out, one by one, forever.

What readiness cannot cover — churn during operation, hours after readiness was awaited — is exactly
waiting sends' half. They compose: readiness says *"tell me when the world is right"*; waiting sends says
*"tolerate the world being briefly wrong at the instant I act."*

## The knowledge substrate: peer knowledge covers typermedia types

This is the part that was previously named "Typermedia parity" — a misnomer, retired with this proposal.
The Tessaging peer-memory increments had two halves: *knowledge* (who exists, what they serve) and
*deferred delivery* (what waits for their return). All their user-visible value came from the second half,
which cannot exist for request/response. Extending the knowledge half to typermedia types, alone, would
change **nothing observable**: routing would still require a live connection. There is no behavior for
typermedia to reach parity *with* — the durable peer topology's own settled sentence gives it away by
promising "known-peer **waiting**".

So typermedia peer knowledge is designed *here*, sized by what the two mechanisms need from it — which is
one distinction:

- **Known-but-down**: a remembered peer serves the type and is currently down. Waiting is *rational* — the
  peer's identity-based memory says it will return — so patience is confident, and a failure names the peer.
- **Never-seen**: nothing this endpoint has ever met serves the type. Waiting is a gamble on first contact —
  a short allowance for the startup race — after which the failure is loud and diagnostic: almost certainly
  a deployment or configuration error, named as such.

Plus the lifecycle hygiene the topology effort already defined for tessage types: a shrunk advertisement
renouncing a typermedia type moves it back to never-seen (no waiting on renounced types), and a
decommissioned peer's types count as never-seen again.

## What each delivery shape gains

- **Typermedia tueries and typermedia tommands** — the core case: bounded patience instead of instant
  explosion, informed by known-but-down vs never-seen; readiness available before opening traffic.
- **Exactly-once tommands** — the cold-start bind race: a tommand sent before its sole handler was *ever*
  met fails loud today (deliberately, per bind-at-send). Waiting sends would wait — bounded — for the first
  contact and *then* bind. The wait precedes the bind, so the pair's single ordered receiver-deduped stream
  is untouched: exactly-once in-order must be walked through explicitly in the design conversation, but the
  shape looks guarantee-preserving by construction.
- **Tevents** — nothing. Deliberately out of scope: one-way delivery is fully served by the peer topology's
  queue/persist machinery.

## ⚖ Settled (2026-07-17)

1. ⚖ **Readiness is awaited on types: "handlers for these types are available."** Never on peers —
   deployment topology stays out of application code, matching typermedia's type-routed philosophy. The
   type sets are gathered by reflection helpers along the lines of `TypermediaTypes.InAssemblyContaining<T>()`
   and `TypermediaTypes.InNamespaceOf<SomeQuery>(levelsToWalkUpBeforeRecursingDown: 2)` — the
   `MapTypesFromAssemblyContaining<T>` idiom, applied to readiness — because hand-enumerating types has the
   classic failure mode: the one forgotten type that surfaces as a timeout in production. The sets contain
   only remotable single-handler types (tueries, typermedia tommands, exactly-once tommands); tevents are
   excluded — multi-subscriber, no "available" concept, and fully served by the peer topology's machinery.
   A reflected set that is empty, or that would include anything else, fails loud at composition time.
2. ⚖ **Patience defaults to a flat 30 seconds, then throws.** No differentiated patience: with one flat
   default, the known-but-down vs never-seen distinction drives no *behavior* — it survives only in the
   failure message ("peer X serves this and is down" vs "nothing this endpoint ever met serves this"),
   which is where most of its value was. Differentiated patience can be added if a real need ever shows up.
3. ⚖ **Typermedia knowledge is not persisted.** Its entire value expires with a caller's patience window,
   so process-lifetime memory suffices on every foundation — sized by what waiting needs, never by symmetry
   with Tessaging. (It may not even need the peer registry: a small memory of "types seen served, by whom,
   this process" is enough for the failure wording.)
4. ⚖ **`RequirePeers` is kept.** Readiness likely subsumes it for current application needs — awaiting the
   types a peer serves before publishing covers the first-contact race, and queue-while-down covers
   everything after the first meeting — but `RequirePeers` still expresses what readiness cannot: a pure
   subscriber peer (nothing to await on) and publishing immediately at cold start without blocking on the
   peer's arrival. Deletion is considered only if readiness has actually replaced it in practice, never
   speculatively before.
5. ⚖ **The no-handler exception family becomes exclusively the patience-exhausted failure** — those
   exceptions are never thrown immediately again, and their messages say what was waited for, for how long,
   and what is remembered ("waited 30s; remembered peers serving it: ..."). What still throws immediately
   is *different exception types entirely*: the programming-error-shaped failures (a non-remotable type, an
   unmapped type, a message-type-rule violation — waiting on those only delays the stack trace) and sends
   during shutdown. A side effect worth pinning: the several-remembered-handlers-none-live send
   (`MultipleHandlersForTessageTypeException`) *improves* under waiting — the moment one handler returns,
   binding to the live one is correct by the existing preference order, so waiting legitimately resolves
   the ambiguity; only exhausted patience throws it, still naming the peers and the decommission remedy.
6. ⚖ **Scope exclusion is structural, not a rule:** `ILocalTypermediaNavigatorSession` is a different type
   that never crosses the wire, so in-process navigation cannot race discovery by construction; and a
   "self" send does not exist remotely at all — an in-roster tommand executes inline in the sender's
   execution (the consistency law), so it has nothing to wait for by construction.

## The gating open question: the style substrate *(resolved — the substrate collapsed first, as this section demanded)*

Designing this effort surfaced a recurring pattern: dynamic topology needed a Typermedia parity batch, peer
memory spawned "increment 7", decommission today covers Tessaging routes and not typermedia ones — and this
effort, on the current architecture, would again be built once per style. When every effort ends with "now
do it again for the other style", the architecture is saying the seam is in the wrong place: the
typermedia<>tessaging split kept the two styles' *public models* rightly distinct, but made each style carry
a private copy of the *distributed substrate* (discovery query, route table, topology reconciliation, peer
knowledge) over a hosting layer whose style-ignorance is a polite fiction — its shape exists to serve
exactly these two consumers.

Whether to collapse that — one distributed-endpoint substrate owning identity, discovery, one
advertisement, one router, peers and their lifecycle, readiness and waiting, with the styles as handler
kinds plus delivery semantics on top — is evaluated in
[style-substrate-and-hosting-evaluation.md](style-substrate-and-hosting-evaluation.md) before anything here
is built: building readiness and waiting into the duplication and migrating them later would be the
backwards order. The former question "advertisement unification" dissolves into this one.

## Relation to durable-peer-topology.md

Independent and composing, exactly as [durable-peer-topology.md](durable-peer-topology.md) describes: that
effort's increments are complete and unchanged; this one consumes its peer memory (`RememberedPeer`, the
registry, the lifecycle events) as the substrate that tells waiting whether it is rational.

## The increments, as executed (migration phase 8, one green commit each)

1. Typermedia knowledge (the retired "increment 7", right-sized): peer memory already remembered typermedia
   types since migration phase 3 — this increment generalized the single-handler query
   (`IPeerRegistry.HandlerIdsFor(Type)`, exact match over every remotable single-handler kind), sized
   exactly by what the known-but-down vs never-seen distinction needs. Even leaner than ⚖ 3 anticipated: no
   separate typermedia memory exists at all — the one peer memory serves, with durability following the
   foundation as it always did.
2. Waiting sends for typermedia tueries and tommands: `IHandlerAvailability`, a bounded re-check loop over
   the router's routes and the peer memory (polled deliberately — the wait window is rare and bounded, so
   polling's simplicity beats signal plumbing); the no-handler family became exclusively the
   patience-exhausted failure per ⚖ 5; the external client's router keeps immediate throws (its connections
   change only by its own explicit connects — nothing to wait for); the interprocess spec's hand-rolled
   retry loop dissolved into one waiting navigation.
3. Waiting sends for the exactly-once cold-start bind, walked through the exactly-once in-order guarantee
   first: the wait strictly precedes the one bind-at-send, so the construction the guarantee rides is
   untouched. The ⚖ 5 side effect pinned: with several remembered handlers and none live, the send binds to
   the one that connects — live is current by definition. One documented corner: on SQLite, a sender whose
   transaction already holds the per-database write gate blocks the very advertisement recording that would
   satisfy its wait, degrading to the pre-waiting failure delayed by patience.
4. The readiness awaitable: `IEndpoint.AwaitReadinessAsync(ReadinessTypes, patience?)`, availability
   defined as precisely what a send would not have to wait for (own-roster types count available — served
   in-boundary), `ReadinessTypes` reflection factories per ⚖ 1 failing loud at composition on anything
   non-single-handler or empty, `EndpointNotReadyWithinPatienceException` naming every type still
   unavailable with the known-but-down vs never-seen wording per type.
