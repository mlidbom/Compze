# Readiness and waiting sends: request/response stops racing discovery

**Status: proposal draft 2026-07-17 — nothing here is settled.** This document exists to start the design
conversation; decisions will be marked ⚖ as they land, the way [durable-peer-topology.md](durable-peer-topology.md)
records its. One constraint is inherited as settled: this effort absorbs what the durable peer topology's
increment plan called "Typermedia parity" (increment 7), and that item's ⚖ *lands before the next release*
commitment transfers with it.

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

## Open design questions — the conversation to have

1. **The readiness API shape.** What exactly is awaitable — "handlers for these types reachable", "these
   peers met", "all required peers met", all three? Where does it live, and what is it named?
2. **Patience policy.** Defaults; per-send override vs a per-endpoint composition declaration; different
   defaults for known-but-down (generous?) vs never-seen (short); is infinite patience expressible, and
   should it be?
3. **Does typermedia knowledge need persistence at all?** Its entire value expires with a caller's patience
   window. Durable memory would only change behavior in the window after *our* restart and before discovery
   re-fetches advertisements (seconds), plus failure wording for a long-gone peer. Recommendation to
   examine: process-lifetime knowledge may suffice even on database-backed endpoints — decide by what
   waiting needs, never by symmetry with Tessaging.
4. **Interplay with `RequirePeers`.** Does requiring a peer imply readiness includes it, and confident
   patience for the types it serves?
5. **The failure surface.** Do the existing no-handler exceptions double as the patience-exhausted failure,
   or does exhausted patience deserve its own failure naming what was waited for and for how long?
6. **Advertisement unification.** The peer registry records only Tessaging advertisements today
   (`TessagingEndpointInformation`); typermedia types travel their own discovery query. One unified
   remembered advertisement per peer, or per-style entries in the registry?
7. **Scope exclusions.** Self/in-process sends (the self-connection is always live); anything else?

## Relation to durable-peer-topology.md

Independent and composing, exactly as [durable-peer-topology.md](durable-peer-topology.md) describes: that
effort's increments are complete and unchanged; this one consumes its peer memory (`RememberedPeer`, the
registry, the lifecycle events) as the substrate that tells waiting whether it is rational.

## Increment sketch (to be settled with the design)

1. Peer knowledge covers typermedia types (the retired "increment 7", right-sized per question 3).
2. Waiting sends for typermedia tueries and tommands.
3. Waiting sends for the exactly-once tommand cold-start bind (walked through the exactly-once in-order
   guarantee first).
4. The readiness awaitable.
