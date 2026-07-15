# Changelog

All notable changes to Compze.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `ITransactionIgnoringTeventPublisher` — the publish escape hatch: publishes immediately and unconditionally, with the ambient transaction suppressed, so the tevent is emitted even if the caller's transaction rolls back. For tracing/monitoring infrastructure that must emit now; a separate interface deliberately, so depending on it makes "this code emits out-of-band" visible in a constructor signature. Rejects any tevent implementing `IMustBeSentTransactionally` — immediate, unconditional delivery structurally cannot back a transactional send. See `src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`.
- `ITeventPublisher` — the one way to publish a tevent, for anything that publishes (the tevent store forwarding committed taggregate tevents is just its most common client). It routes each tevent by the delivery contract the tevent's type declares: synchronous in-process delivery within the caller's transaction always (plus observation, outside it); durable remote delivery for an `IExactlyOnceTevent` and best-effort transient remote delivery for any other `IRemotableTevent` when the endpoint's composition wires the legs. See `src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`.
- Typed endpoint composition: `ComposeEndpoint(it => it.NamedPipeEndpointTransport().SqliteEndpointDatabase(...))` returns an `EndpointFoundation` whose type carries the endpoint's declared database engine — the features added on it (e.g. `AddExactlyOnceTessaging`) bind their engine pairings through the compiler, and a mismatched pairing does not compile. Includes the serializer-slot interfaces (`ITessagingSerializerSlot`, `ITypermediaSerializerSlot`) through which serializer packages offer their implementations inside a feature's compose lambda.
- `IEndpointAddressAnnouncer`: the announcement timing contract is now host-ready rather than endpoint-local — an endpoint announces once every endpoint in its host has finished starting to listen (the sending phase), and retracts as the first act of the host's stopping. The announced address is the endpoint's one transport-server address, serving every distributed capability the endpoint speaks.
- Added the `Tevents()` projection: a sequence of `IPublisherIdentifyingTevent<TTevent>` wrappers projects to the wrapped inner tevents.
- The delivery-tier interfaces now document their settled contracts: `IAtMostOnceTessage` (kept for best-effort send + receiver dedup — the UI double-click case) and `IExactlyOnceTessage` (the durable tier) point at the tevent delivery model (`src/Compze.Tessaging/dev_docs/tevent-delivery-model.md`), replacing the stale open-question comments that document answered.
- The publisher-identifying wrapper carries only publisher identity, not delivery-guarantee markers: `IPublisherIdentifyingTevent<out TTevent>` is the sole wrapper interface, and it has a type-identifier mapping so closed generics over it can travel in persisted and transmitted type references. A tevent's delivery guarantee lives on the tevent itself and is read via covariance (`IPublisherIdentifyingTevent<IExactlyOnceTevent>`); the earlier `IRemotablePublisherIdentifyingTevent<>`/`IExactlyOncePublisherIdentifyingTevent<>` tiers, which re-declared the inner's guarantee interfaces onto the wrapper, are removed.

## 0.1.0-alpha

- Initial pre-release
