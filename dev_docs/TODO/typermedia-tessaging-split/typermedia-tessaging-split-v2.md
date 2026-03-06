# Typermedia–Tessaging Split v2

## Goal

Typermedia and Tessaging are completely separate paradigms that should be completely separate codebases. Their only shared dependencies are `Compze.Abstractions` (message type hierarchy) and utility packages (serialization, contracts, threading).

- **Typermedia** — synchronous request-response. Client sends request, blocks, gets result. Server owns the transaction. Client is stateless. Transport is just HTTP + JSON.
- **Tessaging** — asynchronous fire-and-forget. Events fan out to subscribers. Exactly-once delivery requires outboxes, tracking, and per sender-receiver ordering.

Their accidental merger was a historical implementation shortcut that created enormous complexity.

## Key corrections from v1

- **No shared transport.** Typermedia transport is plain HTTP. Tessaging transport needs delivery tracking, retries, outbox integration. `ITransportMessagePoster` as a shared abstraction was wrong.
- **No unified handler execution engine.** v1 proposed a convergence point for ordering guarantees across both paradigms. Typermedia doesn't need ordering — it's inherently serialized by the caller. Read-after-write staleness is a server-side projection concern, not a transport concern.
- **`IClient` should be removed.** It wraps `IRemoteTypermediaNavigator` in scopes/callbacks that add nothing. A navigator *is* the client. (See `remove-iclient.md`)

## Completed Work

### Phase 1: Transport cleanup
- `IEndpointRegistry` returns `IEnumerable<EndPointAddress>` instead of `IEndpoint`
- In-memory transport uses static `InMemoryTransportNetwork` with unique addresses
- `ITessagesInFlightTracker` removed from all typermedia paths

### Phase 2: Split routing and connection types
- Deleted `RoutingInboxClient`, `Outbox.InboxConnection`, `IRoutingInboxClient`, `IInboxConnection`
- New: `ITypermediaRouter` / `ITessagingRouter` / `ITessagingInboxConnection` — separate interfaces per paradigm
- Temporary: `RemoteEndpointConnection` still bundles both; `InboxConnectionRouter` still shared

### Phase 3: Package extractions
- Extracted `Compze.Tessaging.Abstractions` — handler registration, hosting contracts, navigator interfaces
- Extracted `Compze.DocumentDb` — no dependency on Core

## Next Steps

### Immediate
1. [DONE] Remove `IClient`
2. Separate `RemoteEndpointConnection` — own connection type per paradigm
3. Dissolve `InboxConnectionRouter`

### Strategic
4. Split handler registration — typermedia (`ForTuery`, `ForTommandWithResult`) vs tessaging (`ForTevent`, fire-and-forget `ForTommand`). The void `ForTommand<T>()` is ambiguous — used by both currently.
5. Separate handler dispatch — typermedia is single-handler lookup; tessaging is fan-out with ordering/tracking
6. Separate transport — typermedia gets trivial HTTP transport; kill `ITransportMessagePoster` as shared abstraction
7. Package topology — Typermedia and Tessaging become independent package families
