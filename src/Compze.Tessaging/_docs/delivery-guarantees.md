# Delivery guarantees

A tessage's type declares what its delivery guarantees, and the endpoints honor it: neither the code that publishes or
sends it nor the code that handles it chooses. An endpoint that can't honor what a type declares refuses it, loudly,
when it is declared or when it is published: a guarantee is never silently weakened.

## The four ways a tevent reaches a handler

Each way gives up one thing the one above it has:

| | What it is | Gives up |
|---|---|---|
| **Participation** | Handlers in the publishing endpoint run within the publisher's transaction, before the publish returns. A failing handler fails the publish. | |
| **Exactly-once** | The tevent is stored in the publisher's transaction, and each subscribing endpoint handles it exactly once, in a transaction of its own. | Taking part in the publisher's transaction. |
| **Best-effort** | The tevent is sent to the subscribing endpoints, which handle it in a transaction of their own. Nothing is stored, deduplicated or retried. | The certainty that it arrives. |
| **Observation** | [Observers](observing-tevents.md) watch committed tevents, outside every transaction. | Transactions. |

Which way a tevent reaches a handler is decided like this:

- **Every handler in the publishing endpoint participates,** whatever the tevent's type.
- **A handler in another endpoint receives it exactly once or best-effort,** as the tevent's type declares:
  `IExactlyOnceTevent` exactly once, any other `IRemotableTevent` best-effort. An `ITevent` that is not remotable never
  leaves the endpoint that publishes it.
- **An observer observes,** wherever it is.

Every way delivers a remote tevent on commit: a tevent whose publisher's transaction rolls back reaches no other
endpoint.

## Exactly-once delivery

An exactly-once tevent or tommand is written to the sending endpoint's outbox within the sender's transaction, so it is
sent if, and only if, the sender's change commits. The outbox delivers it until the receiving endpoint has stored it in
its inbox, through network failures and restarts on either side. The receiving endpoint deduplicates it by its `Id`, and
handles it in a transaction that also records that it was handled, so its handler's transactional effects commit once.

- **A subscriber that is down gets it when it returns.** An exactly-once tevent goes to every endpoint whose
  subscription the publisher *remembers*, not only to those connected when it is published. An endpoint remembers every
  other endpoint it has met, with what that endpoint handles and subscribes to.
- **A tommand is bound to one receiver when it is sent:** the endpoint that handles its type and is connected, or else
  the one remembered endpoint that handles it. When that endpoint is down, the tommand waits for it.
- **In order.** What one endpoint sends another, tevents and tommands alike, the other receives in the order the sending
  transactions committed, across restarts on either side. An endpoint handles its exactly-once tessages one at a time,
  in the order they arrived.
- **A failing handler is retried, for 5 minutes unless the endpoint declares otherwise.** When a handler throws, its
  transaction rolls back and the tessage is tried again: a few times at once, then with a delay that grows from a second
  up to a minute. The tessages behind it wait, since the endpoint handles them in order. When its handling has failed
  for 5 minutes, the tessage is marked failed, and isn't handled again, not after a restart either; the endpoint goes on
  with the next. The time counts while the endpoint runs and can reach its database, across restarts: when the
  endpoint can't reach its database, it is the endpoint that is failing, not the tessage, and nothing counts.
- **When to give up, declared on the endpoint:** its `RetryPolicy`, in an exactly-once declaration, or on its builder.
  `RetryPolicy.GiveUpAfter(...)` gives up after another time, and `RetryPolicy.RetryForever` never gives up on a tessage:
  the order is kept, whatever that costs the tessages behind it.
- **A handler can be cancelled.** A handler that takes a `CancellationToken`, after the unit of work, is handed the
  token of the execution it serves: for a tessage the endpoint received, one the endpoint's stop cancels; for a handler
  run within a send or a publish, the token given to `SendAsync` or `PublishAsync`. A try that a stop cancels doesn't
  count toward the retry policy: the endpoint's next start tries the tessage again.

[!code-csharp[](delivery-guarantees.cs#declaring_a_retry_policy)]

## Best-effort delivery

A best-effort tevent is sent to each endpoint that subscribes to it. Its handlers there run in a transaction of their
own, so their effects are atomic, but nothing is stored, deduplicated or retried, and a handler that throws is logged
and not run again.

Best-effort is still the best effort the publisher can make: it doesn't drop tevents merely because a subscriber is
away.

- **Held while a subscriber is down.** The publisher keeps, in memory, a queue for each endpoint it remembers as a
  subscriber. Tevents published while that endpoint is down wait in the queue, and are delivered, in order, when it
  returns.
- **In order.** A subscriber receives the tevents in the order they were published.
- **What can be lost:** what was published before a subscriber was first met, the one tevent being delivered when a
  delivery fails, and what is queued when the publisher's process ends. A failed delivery pauses the queue until the
  subscriber answers again: the tevents behind the lost one are delivered, in order, never skipped.
- **Bounded.** A queue holds at most 10,000 tevents. A publish that would overflow one fails with
  `BestEffortTeventQueueOverflowException`, naming the subscriber.

Two declarations adjust this for chosen endpoints:

- **`RequiredPeers`:** endpoints this one requires. Everything published before a required endpoint is first met is
  held for it, so tevents aren't lost to the moments it takes the endpoints to find each other at startup.
- **`PeersNotQueuedFor`:** endpoints this one keeps nothing for. A tevent reaches such an endpoint only while it is
  connected.

## Consistency: the endpoint is the boundary

After each unit of work an endpoint is consistent, not eventually consistent: what one tessage's handling changed in the
endpoint has been changed in full, by the handlers of every tevent it published there, within the same transaction.
Where a handler lives decides when it runs:

- **Within the endpoint, at once.** A tevent's handlers in the publishing endpoint participate in the publish, and a
  tommand whose handler is in the sending endpoint is handled at once, within the sender's unit of work: no outbox, no
  inbox, and exactly once because it is one transaction.
- **Within the endpoint, after the sender commits, when the handler is deferred.** A tommand handler registered in an
  exactly-once declaration's `RegisterDeferredTommandHandlers` is never run within its sender's unit of work. The
  endpoint stores the tommand in its own inbox within the sender's transaction, and handles it in a unit of work of its
  own once the sender has committed: exactly once, what the endpoint sends itself in the order the senders committed,
  and tried again while it fails, as the endpoint's retry policy says. The sender neither waits for it nor sees it fail.
- **In another endpoint, eventually,** with the guarantee its type declares.

So moving a handler to another endpoint, or deferring it, is a decision about consistency, and it is visible where it is
made, in the declarations. The price of the boundary is that everything within it runs in one transaction: each participating
handler adds to the time it holds its locks. When that costs too much, split the endpoint, observe instead of
participating, or send a tommand to a deferred handler, or to an endpoint, that does the work later.

Work of several steps, each of which must be durable, runs on one endpoint as deferred handlers that each send the next
step to the endpoint itself: once a step's sender has committed, the step is sure to be handled, across crashes and
restarts. Sending one is refused before the endpoint has started and once its stop has ended; one sent while the
endpoint stops is handled when it next starts.

[!code-csharp[](delivery-guarantees.cs#deferring_a_tommand_handler)]

## What isn't there yet

- **Forgetting an endpoint.** An endpoint remembers every endpoint it has met, and nothing yet makes it forget one. What
  is owed an endpoint that never returns waits for it: exactly-once tessages in the outbox, and best-effort tevents in
  memory until the queue's bound fails the publishes.
- **Administering failed tessages.** A tessage marked failed stays recorded as failed in the inbox, with its last
  failure, and nothing yet shows or retries it. Retrying it will handle it out of order: marking it failed gave up its
  place.
