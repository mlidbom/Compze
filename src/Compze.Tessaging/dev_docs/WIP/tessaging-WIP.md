# Tessaging work in progress

The hub for ongoing Tessaging work. Current-state documentation — how the project actually works — lives in
[the parent dev_docs folder](../); this folder holds only the efforts still in flight, and
[DONE](../DONE/) holds the completed ones.

## Ongoing work

- [tessaging-migration-plan.md](tessaging-migration-plan.md) — the path from the pre-harmonization code to
  the target design: ten ordered phases, each a run of green committed increments. **Phases 1–9 are
  executed; phase 10 — the Host demotion — remains**, and after it a final rolling-docs coherence sweep.
- [tessaging-target-design.md](tessaging-target-design.md) — the destination, described straight up: ⚖ the
  consistency law, the domain/endpoint/process triad, the LocalTessagingEngine, synchrony-follows-the-type,
  the two endpoint types, administration, topology. Phase 10's remaining delta from the current state is the
  Host's role: the target's "a host is an optional convenience; endpoints are first-class" is not yet what
  the code does. When phase 10 lands, this document retires to DONE and the current-state docs are the
  living truth.
- [src/Compze.Hosting/dev_docs/wip/same-machine-hosting.md](../../../Compze.Hosting/dev_docs/wip/same-machine-hosting.md)
  — same-machine hosting: the named-pipe transport, the interprocess registry, and the router's
  reconciliation.

## Current-state documentation (the parent folder)

- [tessaging-model.md](../tessaging-model.md) — what Tessaging is: the paradigm and its two siblings, the
  consistency law, the engine, endpoints, storage, topology, administration.
- [tevent-delivery-model.md](../tevent-delivery-model.md) — how tevents travel: the delivery ladder,
  publishing, subscribing, observation, ordering.
- [peer-model.md](../peer-model.md) — the endpoint's memory of its peers and everything computed from it:
  fan-out membership, receiver binding, queue-while-down, advertisement lifecycle, decommission, waiting
  sends and readiness.
- [storage-model.md](../storage-model.md) — the domain database: per-endpoint table-sets, the endpoint
  catalog, the process lease, schema creation.
- [code-map.md](../code-map.md) — where everything lives: projects, namespaces, key types, the test suite.
- [src/Compze.Hosting/dev_docs/hosting-model.md](../../../Compze.Hosting/dev_docs/hosting-model.md) — what
  an endpoint and a host are; production and testing hosting.

## References

- [src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md](../../../TODO/type-assignability-routing-and-publisher-identifying-tevents.md)

### Completed efforts

- [DONE/durable-peer-topology.md](../DONE/durable-peer-topology.md) — peer memory, queue-while-down, shrink,
  decommission.
- [DONE/readiness-and-waiting-sends.md](../DONE/readiness-and-waiting-sends.md) — waiting sends and the
  readiness awaitable.
- [DONE/style-substrate-and-hosting-evaluation.md](../DONE/style-substrate-and-hosting-evaluation.md) — the
  evaluation whose verdicts drove the harmonization: Tessaging the common paradigm, the feature machinery's
  death, the concrete endpoint types. Question 6's distributed-quiescence sketch still awaits its own
  design effort.
- [DONE/typermedia-tessaging-split/](../DONE/typermedia-tessaging-split/) — the split that preceded the
  harmonization.
- [DONE/client-endpoint-entanglement.md](../DONE/client-endpoint-entanglement.md),
  [DONE/remove-iclient.md](../DONE/remove-iclient.md) — earlier untanglings.

### Public documentation

- [src/Compze.Tessaging/_docs/introduction.md](../../_docs/introduction.md)
