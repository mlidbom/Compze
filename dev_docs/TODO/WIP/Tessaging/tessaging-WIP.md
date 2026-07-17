# Tessaging work in progress — document hub

The ongoing Tessaging work is recorded across documents in several places. This page links them all until the
documentation gets a proper home; nothing lives here — each link's target remains its topic's single home.

## The active effort

- [durable-peer-topology.md](../../durable-peer-topology.md) — **the current effort's single home**: the
  settled design (⚖-marked decisions: vocabulary, shrink, decommissioning, the queue bound, ...) and the
  increment log. Increments 1–6 are done (peer registry → fan-out reads it → tommand receiver binding → the
  transient→distributed/best-effort rename → the distributed tier: queue-while-down, `RequirePeers`, the
  bound, opt-down → shrink + decommission). **Next: increment 7, Typermedia parity.**
- **Readiness/waiting-sends** — the companion effort: bounded route patience for request/response sends and
  an explicit readiness awaitable. *No document of its own yet* — it exists only as the "Relation to the
  readiness/waiting-sends effort" section and scattered mentions inside durable-peer-topology.md; writing its
  proposal is the next design conversation.
- **Typermedia parity** (increment 7) likewise has no document of its own: the settled part (it lands before
  the next release; registry entries follow the same model) is in durable-peer-topology.md, details
  deliberately deferred until the increment starts.

## Standing design documents the effort keeps as-built

- [tevent-delivery-model.md](../../../../src/Compze.Tessaging/dev_docs/tevent-delivery-model.md) — the
  delivery-model reference: the participation / exactly-once / best-effort / observation ladder, the
  wiring rules, the no-escape-hatch decisions.
- [hosting-model.md](../../../../src/Compze.Hosting/dev_docs/hosting-model.md) — the hosting/composition
  model the Tessaging features plug into (features, foundations, endpoint lifecycle phases).
- [Compze.Tessaging/_docs/introduction.md](../../../../src/Compze.Tessaging/_docs/introduction.md) — the
  public, website-published introduction (`_docs` = public docs, `dev_docs` = internal — a deliberate split,
  never merged).

## Changelogs carrying the effort's as-built record

- [Compze.Tessaging/CHANGELOG.md](../../../../src/Compze.Tessaging/CHANGELOG.md) — the effort's main record,
  one bullet per increment.
- [Compze.Tessaging.Abstractions/CHANGELOG.md](../../../../src/Compze.Tessaging.Abstractions/CHANGELOG.md)
- [Compze.Internals.Transport/CHANGELOG.md](../../../../src/Compze.Internals.Transport/CHANGELOG.md) and
  [Compze.Internals.Transport.AspNet/CHANGELOG.md](../../../../src/Compze.Internals.Transport.AspNet/CHANGELOG.md)
  — the wire surface (request kinds, routes) renamed in increment 4.
- [Compze.Internals.SystemCE/CHANGELOG.md](../../../../src/Compze.Internals.SystemCE/CHANGELOG.md) —
  `Transaction.OnCompletedWithoutCommitting`, added for the queue-slot reservations in increment 5.

## Parked and completed relatives

- [type-assignability-routing-and-publisher-identifying-tevents.md](../../../../src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md)
  — parked notes on the routing model and the `IPublisherTevent` wrapper family.
- [done/typermedia-tessaging-split/](../../done/typermedia-tessaging-split/typermedia-tessaging-split-v3.md)
  — the completed predecessor effort this work grew out of (v3 is the final design; the folder holds the
  earlier versions and design questions).
- [done/client-endpoint-entanglement.md](../../done/client-endpoint-entanglement.md) — the completed
  client/endpoint untangling that preceded it.
