# Client/Endpoint Entanglement

## Status: Fully resolved — delivery manager implemented

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

## Delivery Manager — implemented

The per-connection delivery manager has been implemented, replacing the centralized `OutboxRetryPoller`. Each `TessagingConnection` now owns its own FIFO delivery queue.

### What was done
- **`TessagingConnection`** — owns a FIFO delivery queue with dedicated send loop thread and exponential backoff (0.5s → 64s capped). Head-of-line blocking preserves message ordering per connection. Has `EnqueueForDelivery(tessageId, tessage)`, `StartDelivery()`, `StopDelivery()`. On `StartDelivery()`, loads its own undelivered messages from DB via `GetUndeliveredTessagesForEndpoint(EndpointId)`, deserializes them, enqueues them, then starts the send loop.
- **`ITessagingInboxConnection`** — extended with `EnqueueForDelivery`, `StartDelivery`, `StopDelivery`.
- **`IOutboxSqlLayer` + all 4 SQL providers** — added `GetUndeliveredTessagesForEndpoint(EndpointId)` filtered query, so each connection loads only its own messages.
- **`TessagingRouter`** — passes `ITessageStorage`, `ITaskRunner`, `IBackgroundExceptionReporter` to connections at creation. Exposes `StartDelivery()`, `StopDelivery()` that delegate to all connections. No knowledge of message loading or recovery.
- **`Outbox`** simplified to a thin layer — persists in transaction, on commit calls `connection.EnqueueForDelivery(...)`. No delivery management at all.
- **`OutboxRetryPoller`** eliminated entirely.

### Design
- One delivery queue per `TessagingConnection`, owned by the connection itself
- Backoff is per-connection (not per-message): consecutive failure count tracks connection health
- On success: backoff resets immediately, next message processes without delay
- On failure: `RecordDeliveryFailure` records to DB, backoff wait, retry same message (head-of-line blocking)
- Thread-safe enqueue from transaction commit callbacks; single-threaded dequeue in send loop

## Key ordering guarantee

The bus must guarantee that exactly-once commands and events from the same outbox execute in order at the receiving inbox. The FIFO delivery queue per connection upholds send-order. Note: the inbox itself does NOT currently enforce strict execution order (it's FIFO-biased but can skip blocked messages). Strict inbox ordering would require sequence numbers — a separate piece of work with a todo comment in `Inbox.HandlerExecutionEngine.Coordinator.cs`.
