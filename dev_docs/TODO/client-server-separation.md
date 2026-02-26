# Client-Server Separation

## Goal
Fully separate Typermedia/RPC from Asynchronous messaging with fire-and forget commands and events.

## Current State (after initial cleanup)

### What's been done
- **`IEndpointRegistry`** now returns `IEnumerable<EndPointAddress>` instead of `IEndpoint` objects — no more reaching into endpoint internals
- **In-memory transport** uses a static `InMemoryTransportNetwork` (process-wide) with unique per-instance addresses (`memory://{endpointId}/instance/{count}`). Behaves like a real network — servers bind on start, unbind on stop, clients look up by address. No per-host shared objects threaded through DI.
- **`ITessagesInFlightTracker`** removed from all typermedia paths:
  - `ApiEndpointClient` no longer takes or uses the tracker
  - `SendingTessageOnTransport` removed from typermedia message sends (client side)
  - `DoneWith` skipped for typermedia message types (server side)
  - Tracker now confined to exactly-once tommands and tevents only
- **Test assertions** for typermedia failures now only assert the client-side exception (which propagates back through the transport). Host-side tracker assertions removed for typermedia. Exactly-once test behavior unchanged.

### What's still entangled

#### `InboxConnection` wears two hats
`Outbox.InboxConnection` mixes:
1. **Typermedia API calls** — delegates to `ApiEndpointClient` (pure client, no server needed)
2. **Exactly-once sending** — delegates to `HttpExactlyOnceTessageSender` (outbox/server concept, needs tracker)

Both are created together in `InitAsync()`. A standalone client only needs #1.

#### `RoutingInboxClient` wears two hats
1. **Route typermedia requests** — find which connection handles a command/query type, delegate (`PostAsync`, `GetAsync`)
2. **Route exactly-once messages** — find subscriber connections for events, handler for exactly-once commands (`SubscriberConnectionsFor`, `ConnectionToHandlerFor` for `IExactlyOnceTommand`)

The typermedia routing is used by `RemoteTypermediaNavigator` (client-side). The exactly-once routing is used by `Outbox` (server-side). A standalone client only needs typermedia routing.

#### `ClientBuilder` still takes `IEndpointHost` and `ITessagesInFlightTracker`
- Checks `if(_host is IEndpointRegistry)` to get server addresses — a standalone client would just receive addresses directly
- Takes a container factory from the host — a standalone client would create its own container

## Target Architecture

### Client side (typermedia only)
```
Client → TypermediaRouter → ApiEndpointClient → ITransportMessagePoster
```
- No outbox, no tracker, no exactly-once anything
- Type-based routing: "this command type is handled by endpoint X" → post to X's address
- Creates own DI container with: serializer, type mapper, transport, routing

### Server side
- Endpoints use their own internal client for typermedia needs (querying other endpoints)
- `Outbox` sends exactly-once messages directly to known addresses via `ITransportMessagePoster` — no type routing needed, it already knows the target
- `HttpExactlyOnceTessageSender` keeps `ITessagesInFlightTracker` for exactly-once delivery tracking
- Server management/status queries go to specific known addresses (not type-routed)

### Handler Execution Engine (convergence point)
- Receives ALL incoming transport messages (typermedia AND exactly-once)
- Must handle both because ordering guarantees depend on awareness of all message types
- Ensures queries see effects of commands/events (prevents read-after-write races)
- Doesn't care HOW messages arrived — just receives `TransportTessage.InComing` and executes handlers

### Shared bottom layer
`ITransportMessagePoster.PostAsync(tessage, realTessage, endPointAddress)` — serialize and send to a specific address. Used by both:
- Client's typermedia routing (type → address → post)
- Server's outbox (known address → post directly)
- Server management (known address → post directly)

## Completed: Split routing and connection types

### What was done
- **Deleted `RoutingInboxClient`** (single class doing both typermedia and tessaging routing)
- **Deleted `Outbox.InboxConnection`** (single connection type mixing typermedia API client with exactly-once sender)
- **Deleted `IRoutingInboxClient`** and **`IInboxConnection`** (interfaces that bundled both concerns)

### New types

**Interfaces (domain-named, ISP-compliant):**
- **`ITypermediaRouter`** — `PostAsync`, `GetAsync` only. Consumer: `RemoteTypermediaNavigator`. No lifecycle methods on the interface.
- **`ITessagingRouter`** — `ConnectionToHandlerFor`, `SubscriberConnectionsFor`. Consumer: `Outbox`, `OutboxRetryPoller`.
- **`ITessagingInboxConnection`** — `SendAsync(tevent)`, `SendAsync(tommand)`, plus `EndpointInformation`. The exactly-once connection abstraction.

**Concrete types:**
- **`TypermediaRouter`** — implements `ITypermediaRouter` + `IDisposable`. Owns lifecycle (Start/Stop/ConnectAsync), connection management, and the `InboxConnectionRouter` stop propagation.
- **`TessagingRouter`** — implements `ITessagingRouter`. Pure delegation to `InboxConnectionRouter`.
- **`RemoteEndpointConnection`** — implements `ITessagingInboxConnection` + `IDisposable`. Holds both `ApiClient` (for typermedia) and `ExactlyOnceSender` (for tessaging). Created per remote endpoint during `ConnectAsync`.

**Shared infrastructure (temporary):**
- **`InboxConnectionRouter`** — internal route registry shared by both routers. Stores `RemoteEndpointConnection` → type mappings. Has `Stop()`/`AssertNotStopped()` lifecycle to prevent routing after shutdown. Will dissolve when the two paradigms manage their own routes independently.
- **`TransportRegistrar`** — registers both routers via `registrar.Transport()`.

### Key design decisions
- Interface names use **domain terminology** (`Typermedia`, `Tessaging`) not implementation details (`ExactlyOnce`, `AtMostOnce`)
- No "Client" suffix on routers — they route, they don't "client"
- `ITypermediaRouter` has no lifecycle methods — `RemoteTypermediaNavigator` shouldn't see Start/Stop. Lifecycle stays on the concrete `TypermediaRouter`, used directly by `Client` and `Endpoint`.
- `InboxConnectionRouter.Stop()` is called from `TypermediaRouter.Stop()` to ensure both routing paths fail after shutdown — this prevents a race condition where in-flight handlers could `MarkAsSucceeded` then fail on scope disposal, causing `MarkAsFailed` to find 0 unhandled rows.

## Next Steps
1. Separate `RemoteEndpointConnection` — it still bundles `ApiClient` (typermedia) and `ExactlyOnceSender` (tessaging). Each paradigm should have its own connection type.
2. Dissolve `InboxConnectionRouter` — each paradigm discovers and manages its own routes
3. Create standalone `Client.ConnectTo(params EndPointAddress[])` factory that needs no host
4. `ClientBuilder` becomes optional convenience (host knows its endpoint addresses)
