# Remove IClient

## Problem
`IClient` is a confused abstraction. It wraps `IRemoteTypermediaNavigator` in an `ExecuteRequest` callback pattern that adds nothing:

```csharp
// IClient forces this indirection:
client.ExecuteRequest(nav => nav.Post(command));

// When you could just:
navigator.Post(command);
```

## Why it exists
Historical misconception that Typermedia clients are mini-endpoints needing DI scopes, transaction tracking, and lifecycle management — the same infrastructure as async bus participants.

## Why that's wrong
- **Typermedia is synchronous request-response.** No outbox, no exactly-once delivery, no event fan-out. The server handles its own transactions.
- **Clients can't use transactions anyway.** `ICannotBeSentRemotelyFromWithinTransaction` explicitly prevents it. The scopes `TestClient` creates are either useless or actively contradictory.
- **A navigator *is* the client.** It has transport, routing, and connection state. There's nothing else a client needs.

## Action
Remove `IClient`. Callers use `IRemoteTypermediaNavigator` directly. `TestClient.ConnectTo(address)` becomes a factory that returns a connected navigator, not a wrapper around one. If it remains at all.
