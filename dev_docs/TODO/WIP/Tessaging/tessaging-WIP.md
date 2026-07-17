# Tessaging work in progress


- [dev_docs/TODO/WIP/Tessaging/tessaging-WIP.md](../../../../dev_docs/TODO/WIP/Tessaging/tessaging-WIP.md) this document itself

## Ongoing work
### Existing documents
- [dev_docs/TODO/WIP/Tessaging/durable-peer-topology.md](../../../../dev_docs/TODO/WIP/Tessaging/durable-peer-topology.md)
- [dev_docs/TODO/WIP/Tessaging/readiness-and-waiting-sends.md](../../../../dev_docs/TODO/WIP/Tessaging/readiness-and-waiting-sends.md)
  — design settled ⚖ 2026-07-17 (absorbs and retires "Typermedia parity"/increment 7); implementation gated
  on the style-substrate question, evaluated in the document below.
- [dev_docs/TODO/WIP/Tessaging/style-substrate-and-hosting-evaluation.md](../../../../dev_docs/TODO/WIP/Tessaging/style-substrate-and-hosting-evaluation.md)
  — the style-substrate + hosting evaluation (revised 2026-07-17): ⚖ Tessaging is the common paradigm,
  Typermedia and TessageBus its two siblings; the feature machinery on trial — proposed collapse into
  concrete endpoint types; the exact set of remaining projects and their names is still unsettled.
- [dev_docs/TODO/WIP/Tessaging/tessaging-target-design.md](../../../../dev_docs/TODO/WIP/Tessaging/tessaging-target-design.md)
  — the imagined target design, described straight up (no narration of change): ⚖ the consistency law
  (endpoint = immediate-consistency boundary, universal; inline in-roster tommands), the
  domain/endpoint/process triad (DB == domain, never endpoint; per-endpoint table-sets + endpoint catalog),
  the LocalTessagingEngine (working name) with builder/roster/executor/doors, synchrony-follows-the-type,
  the two endpoint types, administration, topology.
- [src/Compze.Tessaging/dev_docs/tevent-delivery-model.md](../../../../src/Compze.Tessaging/dev_docs/tevent-delivery-model.md)
- [src/Compze.Hosting/dev_docs/hosting-model.md](../../../../src/Compze.Hosting/dev_docs/hosting-model.md)
- [src/Compze.Hosting/dev_docs/wip/same-machine-hosting.md](../../../../src/Compze.Hosting/dev_docs/wip/same-machine-hosting.md)

## References

- [src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md](../../../../src/TODO/type-assignability-routing-and-publisher-identifying-tevents.md)

### Previous efforts
- [dev_docs/TODO/done/typermedia-tessaging-split/typermedia-tessaging-split-v3.md](../../../../dev_docs/TODO/done/typermedia-tessaging-split/typermedia-tessaging-split-v3.md)
- [dev_docs/TODO/done/client-endpoint-entanglement.md](../../../../dev_docs/TODO/done/client-endpoint-entanglement.md)

### Public documentation
- [src/Compze.Tessaging/_docs/introduction.md](../../../../src/Compze.Tessaging/_docs/introduction.md)