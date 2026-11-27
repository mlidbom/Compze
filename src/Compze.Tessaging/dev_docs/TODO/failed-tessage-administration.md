# Failed-tessage administration: todo (agreed in outline, nothing designed or built)

## Status

Agreed with Magnus on 2026-11-27, as the effort that follows the exactly-once retry fix (branch `exactly-once-retries`).
The retry fix makes an exactly-once endpoint retry a tessage whose handling fails, with a growing delay, for as long as
its declared `RetryPolicy` allows, and then mark the tessage failed and go on with the tessages behind it. This
effort is how an administrator then sees the failed tessages and retries them, once the problem is fixed.

Until it exists, a failed tessage is retried by setting its inbox row's status back to UnHandled and restarting the
endpoint: the inbox's recovery scan at start re-enqueues it.

## What is agreed

- **A retried tessage is handled out of order.** Marking a tessage failed gives up its place in its pair's order, by
  design. Retrying it later can't give that place back, and the administration surface says so.
- **The admin UI reaches the endpoints through Tessaging,** never through their tables: the endpoint owns its inbox, and
  only it can retry a tessage, since it owns the handling claim and the handler. Magnus expects the UI to be a Blazor web
  app that optionally runs in an endpoint, or an Avalonia app. Both sit on the same Tessaging contract, so the contract is
  the design decision, and the UI can be chosen later.
- **Condition: admin tessages never go through the inbox.** The inbox handles exactly-once tessages one at a time, so an
  exactly-once "retry this" tommand would queue behind the very tessage holding the line. Reads are remotable tueries and
  actions typermedia tommands, which are handled outside the inbox. A retry does nothing to a tessage that isn't marked
  failed, so typermedia's at-most-once delivery is enough.
- **Condition: authorization.** No Tessaging transport authenticates its callers today. An admin surface lets anyone who
  can reach an endpoint retry its tessages and read their bodies, which may hold personal data. Authorization by tessage
  type, above the transport seam, is where it belongs: see `tcp-transport-and-discovery-across-machines.md`, "A pipeline
  belongs above the seam".
- **Administration is a Tessaging endpoint of its own, and not an exactly-once one** (Magnus, 2026-11-27, on topic 7 of
  the tessaging-deferred-handling helper). What the bus itself owns, such as a notice that a tessage was marked failed,
  belongs to no domain, and can't travel by guaranteed delivery: that would cost too much, and a handler of such a
  notice that failed would be a catch-22. It needs no guarantee: the failed mark on the inbox row is the durable record,
  which administration reads with a query, and a notice that may be lost only says when to look again.
- **One administration plane, designed by a helper of its own** (Magnus, 2026-11-27), with Tessaging's other
  administration needs: [peer administration](../WIP/peer-administration.md), the storage-drop act parked at
  `ITessagingSqlLayer.IEndpointCatalogSqlLayer`, distributed quiescence and readiness
  ([the style-substrate evaluation, question 6](../DONE/style-substrate-and-hosting-evaluation.md)).

## A consumer waiting: the Claude tooling's hub

The hub drives its long operations, such as removing a worktree, as deferred tommands its exactly-once endpoint sends
itself ([durable multi-step work on one endpoint](../WIP/durable-multi-step-work-on-one-endpoint.md)). A step that fails
until its retry policy gives up is marked failed, and the operation's request then waits for ever: the hub needs to hear
of it, to end the operation's taggregate. Until it can, its handler catches such a failure and records the operation's
end, giving up the retries. An expected failure, such as git refusing to remove a worktree with changes in it, is the
hub's domain's to record, and needs none of this.

Considered and dropped there: a failure handler at the handler's registration, run in the unit of work that marks the
tessage failed. It served only this case, and one that kept throwing would hold the ordered line for ever.

Prior art of the same shape: NServiceBus's ServiceControl is an endpoint of its own that takes in the failed messages
and publishes a `MessageFailed` integration event for alerting (https://docs.particular.net/servicecontrol/contracts);
its sagas notice a stuck step by a timeout they requested (https://docs.particular.net/nservicebus/sagas/timeouts).

## Open

- The contract's public types, and the package they ship in.
- What a failed tessage shows: the inbox row already records the last exception's type, message and stack trace, the
  number of failed tries, and the time the handling spent failing.
- A hook an application can alert from when a tessage is marked failed, beside the query.
- A failed tessage in an endpoint that is retired can't be retried there: that belongs with
  [peer administration](../WIP/peer-administration.md).
