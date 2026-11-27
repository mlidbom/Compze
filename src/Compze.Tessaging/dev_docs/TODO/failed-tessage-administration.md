# Failed-tessage administration: todo (agreed in outline, nothing designed or built)

## Status

Agreed with Magnus on 2026-11-27, as the effort that follows the exactly-once retry fix (branch `exactly-once-retries`).
The retry fix makes an exactly-once endpoint retry a tessage whose handling fails, with a growing delay, for as long as
its declared `HandlingRetries` allow, and then mark the tessage failed and go on with the tessages behind it. This
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

## Open

- The contract's public types, and the package they ship in.
- What a failed tessage shows: the inbox row already records the last exception's type, message and stack trace, the
  number of failed tries, and the time the handling spent failing.
- A hook an application can alert from when a tessage is marked failed, beside the query.
- A failed tessage in an endpoint that is retired can't be retried there: that belongs with
  [peer administration](../WIP/peer-administration.md).
