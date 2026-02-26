# Client/Endpoint Entanglement

## Status: Core entanglement resolved

The `IsPureClientEndpoint` flag and shared `ServerEndpointBuilder` pipeline have been eliminated. Clients and endpoints are now built through separate pipelines:
- **`ClientBuilder`** — registers only client concerns (transport, serialization, type mapping, remote navigator, routing)
- **`ServerEndpointBuilder`** — always registers full endpoint infrastructure (no `IsPureClientEndpoint` gates)
- **`Client`** — self-contained with its own lifecycle (start routing, connect, stop, dispose). No longer wraps an `Endpoint`.
- **`EndpointHost`** — tracks clients separately from endpoints

### Removed
- `IsPureClientEndpoint` property from `EndpointConfiguration`
- `RegisterClientEndpoint` from `IEndpointHost` interface
- `RegisterClientEndpointForRegisteredEndpoints` from `ITestingEndpointHost` interface
- All `ExecuteClientRequest*` / `ExecuteAsClientRequestOn*` extension methods on `IEndpoint`
- All `if(!Configuration.IsPureClientEndpoint)` gates in `ServerEndpointBuilder`, `Endpoint`, and `Outbox`

## Remaining work

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
