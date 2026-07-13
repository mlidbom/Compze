# BUG: outbox delivery ordering is lost on recovery (restart)

Recorded 2026-07-13, during the design discussion on remotable-but-not-exactly-once tevent delivery
(see `TODO_type-assignability-routing-and-publisher-identifying-tevents.md`, the "Remote delivery
guarantees (D6)" section, and `_TessageTypes..Interfaces.cs:78-79`).

## Summary

Tessaging guarantees that tessages sent to a given endpoint are delivered **in order**. That guarantee
holds only while the sending process stays up. **Across a sender restart it is silently lost**: recovery
reloads the undelivered backlog ordered by *retry metadata* instead of original send order, so the backlog
is delivered in the wrong order — and in the worst case, exactly inverted.

This is a real correctness bug in the current exactly-once path, independent of any new delivery-guarantee
work. It must be fixed regardless.

## The guarantee that is supposed to hold

For a given (sender, destination endpoint) pair, tessages are delivered in the order the outbox committed
them. In steady state this is achieved structurally, with **no sequence numbers**, by
`TessagingConnection`'s send loop
(`src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/TessagingConnection.cs`):

- A single-threaded `SendLoop` per destination connection.
- A FIFO `Queue<PendingDelivery>`, **head-of-line blocking**: it `Peek`s the head, sends synchronously, and
  only `Dequeue`s on success. The next tessage is not even looked at until the current one is delivered and
  `MarkAsReceived`. So exactly one tessage is ever in flight per destination, and reordering is impossible
  while the loop runs.

## The bug

On startup, `TessagingConnection.StartDelivery` → `LoadUndeliveredTessages` reloads the undelivered backlog
from the outbox and re-enqueues it. The reload order is whatever the outbox SQL layer returns, and every
backend returns it ordered by retry metadata, not send order:

```sql
ORDER BY d.RetryCount, d.LastAttemptTime
```

(`SqliteOutboxSqlLayer.cs:127`, `PgSqlOutboxSqlLayer.cs:132` (`NULLS FIRST`), `MySqlOutboxSqlLayer.cs:127`,
`MsSqlOutboxSqlLayer.cs:127` — `IServiceBusSqlLayer.GetUndeliveredTessagesForEndpoint`.) There is no stable
send-order column selected or ordered on; the `TessageId` is a `Guid`. So the original order is not merely
unpreserved — there is nothing to reconstruct it from.

## Concrete failure — order fully inverted

Sender A has committed tevents 1,2,3,4,5 for endpoint B; B is currently down.

1. A's send loop is stuck retrying tevent **1** (head-of-line). `RecordDeliveryFailure` increments that
   dispatch row's `RetryCount` on each attempt — say it reaches 5. Tevents 2–5 were never attempted
   (`RetryCount` 0), because head-of-line blocking never advanced past 1.
2. A restarts. `LoadUndeliveredTessages` runs `ORDER BY RetryCount, LastAttemptTime` and returns
   **2, 3, 4, 5** (RetryCount 0) first, then **1** (RetryCount 5) last.
3. They re-enqueue in that order. When B comes up it receives **2, 3, 4, 5, 1** — the oldest tessage, the
   one that *must* go first, is delivered last.

A subscriber applying ordered state (a projection, a cache applying deltas) is now silently corrupted.

## Root-cause diagnosis

The outbox has inherited **inbox** semantics. Ordering by `RetryCount` is sensible for an *inbox* (retry
failed handler executions, deprioritize the ones that keep failing) — but an *outbox* must deliver strictly
in send order and must never advance past an earlier, still-undelivered tessage. Retry-ordered recovery is
inbox thinking misapplied to the outbox.

## Scope of the guarantee (decided 2026-07-13)

- **Ordering must never be given up for tessages we send.** We keep delivering in order, period. Recovery
  must restore the original order and resume head-of-line.
- **The only acceptable order loss is a message that fails *permanently* in the receiver's inbox** (handler
  retries exhausted / dead-lettered). For those there is no choice — but that is a receive-side terminal
  failure, not something the *send* side may ever cause by reordering its backlog.
- **Ordering across a sender restart is required for exactly-once delivery.** (For future transient /
  best-effort tevent delivery it is explicitly *not* required — a disconnect is a legitimate
  resume-from-live boundary. This bug is about the exactly-once path, which has no such excuse.)

## Fix direction (not yet designed in detail)

- Persist a **stable, monotonic send-order key** per destination stream (a per-(sender→endpoint) sequence, or
  an equivalent monotonic outbox insertion order), and make both recovery reload **and** live delivery honor
  it. `RetryCount`/`LastAttemptTime` are attempt bookkeeping, not ordering keys, and must not drive delivery
  order.
- Recovery must re-establish head-of-line on the *oldest undelivered* tessage and never deliver a later one
  ahead of it — the same invariant the in-memory send loop already enforces in steady state.
- Single-in-flight per destination is retained (no pipelining for now — decided 2026-07-13); that keeps the
  ordering guarantee free while both processes are up, and the sequence key is what extends it across a
  restart.

## Code references

- Policy site (where the bug is flagged in code): `TessagingConnection.LoadUndeliveredTessages`.
- Mechanical cause: the four `*OutboxSqlLayer.GetUndeliveredTessagesForEndpoint` `ORDER BY` clauses.
- Steady-state ordering mechanism: `TessagingConnection.SendLoop`.
- Receive-side serialization (in-order, one-at-a-time execution): `Inbox.HandlerExecutionEngine.Coordinator`
  + the `TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint` dispatching rule.
