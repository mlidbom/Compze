# Client/Endpoint Entanglement

## Problem

The concept of a "client-only endpoint" (`IsPureClientEndpoint`) is architectural nonsense. A **client** and an **endpoint** are fundamentally different things forced through the same `ServerEndpointBuilder` pipeline, causing repeated entanglement issues.

### What a Client needs
- Transport (HTTP posting)
- Serialization + type mapping
- Remote hypermedia navigator (Post/Get for Typermedia commands/queries)
- Routing (which endpoint handles which Typermedia commands/queries)

### What an Endpoint needs (in addition to all of the above)
- Identity (`EndpointId`, `EndpointConfiguration`)
- Outbox (persist + deliver exactly-once messages)
- Inbox (receive, deduplicate, dispatch)
- Handler registry
- Background exception reporting, task runner
- SQL layers, document DB, tevent store, etc.

### Current state
`ServerEndpointBuilder` registers everything for both, gated by `if(!Configuration.IsPureClientEndpoint)`. This means:
- Client endpoints go through the same builder but skip half the registrations
- Shared infrastructure (`RoutingInboxClient`, `IInboxConnection`) serves both RPC routing and exactly-once delivery routing
- Adding dependencies to shared infrastructure (e.g., giving `InboxConnection` outbox storage for delivery management) breaks client endpoints because outbox storage isn't registered for them

### Files involved
- `src/Compze.Tessaging/Hosting/ServerEndpointBuilder.cs` — the builder with the `IsPureClientEndpoint` gate
- `src/Compze.Tessaging/Implementation/Transport/Client/Internal/IInboxConnection.cs` — single interface serving both RPC and exactly-once delivery
- `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/RoutingInboxClient .cs` — creates `InboxConnection` instances, serves both RPC and delivery routing
- `src/Compze.Tessaging/Implementation/Outbox/Outbox.InboxConnection.cs` — concrete connection class nested in `Outbox`, doing both RPC and delivery
- `src/Compze.Tessaging.Hosting.Testing/Tessaging/Buses/TestingEndpointHost.cs` — `RegisterClientEndpoint` / `RegisterClientEndpointForRegisteredEndpoints`

## Proposed solution

### 1. Split the builder
- **`EndpointBuilder`** — registers everything an endpoint needs
- **`ClientBuilder`** — registers only client concerns (transport, serialization, remote navigator, routing)
- Shared registrations extracted to common helper methods

### 2. Split `IInboxConnection`
- **`IRemoteApiConnection`** — for RPC: `PostAsync`, `GetAsync` (used by both clients and endpoints)
- **`IOutboxConnection`** (or delivery-capable connection) — adds `EnqueueForDelivery` (used only by endpoints with outboxes)
- The concrete `InboxConnection` can implement both, but the abstractions are clean

### 3. Untangle `RoutingInboxClient`
- Client instances only need RPC routing → only deal in `IRemoteApiConnection`
- Endpoint instances need delivery routing → deal in `IOutboxConnection` additionally
- Could be two separate classes, or one class with optional delivery capability

## Context: Delivery Manager feature blocked by this

We designed a per-`InboxConnection` delivery manager where each connection owns a FIFO delivery queue with its own send loop thread and exponential backoff. The `OutboxRetryPoller` (centralized poller) would be eliminated entirely. The design:

- `InboxConnection` gets: delivery queue, send loop thread, `EnqueueForDelivery(tessageId)`, `LoadUndeliveredTessages()`, `StartDelivery()`/`StopDelivery()`
- Outbox simplifies to: persist in transaction → on commit call `connection.EnqueueForDelivery(tessageId)`
- Backoff is per-connection (not per-message): 0.5s, 1s, 2s, 4s...64s capped
- Head-of-line blocking preserves message ordering per connection
- On startup: each connection loads its own undelivered messages from DB by `EndpointId`

This feature was blocked because giving `InboxConnection` the outbox storage dependency (needed for marking received/recording failures) broke client endpoints since they don't register outbox infrastructure. Three attempts were made, all hitting the same entanglement. The clean fix is the client/endpoint separation above, after which the delivery manager can be added naturally to the endpoint-only connection type.

## Key ordering guarantee

The bus must guarantee that exactly-once commands and events from the same outbox execute in order at the receiving inbox. The FIFO delivery queue per connection upholds send-order. Note: the inbox itself does NOT currently enforce strict execution order (it's FIFO-biased but can skip blocked messages). Strict inbox ordering would require sequence numbers — a separate piece of work with a todo comment in `Inbox.HandlerExecutionEngine.Coordinator.cs`.
