# Readiness and waiting sends

Endpoints find each other after they start, and an endpoint that restarts is away for a moment. A tessage sent in
such a moment, to a handler not yet found or briefly gone, doesn't fail at once. What happens instead depends on its
kind:

- **A tevent** is held for the endpoints that will receive it: see [Delivery guarantees](delivery-guarantees.md).
- **An exactly-once tommand whose handler the endpoint remembers** is stored in the outbox, bound to that handler's
  endpoint, and delivered when it is back. The send doesn't wait.
- **A tommand or tuery whose handler can't be reached** waits for it, and goes ahead the moment it can. This is a
  *waiting send*.

## Waiting sends

A send waits when no handler of its tessage's type is reachable: a tuery or a typermedia tommand when no endpoint that
handles it is connected, and an exactly-once tommand when it can't be bound to one endpoint: none that handles it has
been met, or several have and none of them is connected. It waits for the endpoint's *handler-availability patience*,
30 seconds unless its declaration says otherwise:

[!code-csharp[](readiness-and-waiting-sends.cs#declaring_patience)]

When the patience runs out, the send fails with `NoHandlerForTessageTypeException` for an exactly-once tommand, and
`NoHandlerForTypermediaTypeException` for a tuery or typermedia tommand. The message tells the two causes apart: an
endpoint that handles the type is remembered and is down, or no endpoint ever met handles it, which is most likely an
endpoint not deployed or not configured to find this one.

A send that could never succeed fails at once instead, such as one of a type that isn't remotable.

## Readiness

Waiting sends make the first callers after a start pay for the endpoints finding each other. An application that would
rather pay before it takes any traffic awaits its endpoint's readiness:

[!code-csharp[](readiness-and-waiting-sends.cs#awaiting_readiness)]

`AwaitReadinessAsync` completes when the endpoint can reach a handler for each of the types: its own handlers count, an
exactly-once tommand counts when its handler's endpoint is connected or remembered, and a tuery or typermedia tommand
when exactly one endpoint that handles it is connected. These are the conditions under which a send doesn't wait.

`ReadinessTypes` gathers the types by reflection, so that a type added later isn't forgotten:

- `ReadinessTypes.InAssemblyContaining<T>()`: those in `T`'s assembly.
- `ReadinessTypes.InNamespaceOf<T>(levelsToWalkUpBeforeRecursingDown)`: those in `T`'s namespace and the namespaces
  below it, after walking up the given number of levels.
- `ReadinessTypes.These(...)`: the types named.

They take only tueries, typermedia tommands and exactly-once tommands, the kinds that have one handler to wait for,
and fail at once on an empty set or a type of another kind. Readiness waits as long as the endpoint's
handler-availability patience, or as long as its `patience` argument says, then fails with
`EndpointNotReadyWithinPatienceException`, naming every type still unreachable, and why.

Readiness is the readiness probe an orchestrator calls, and the place to give up on a misconfigured deployment once, at
startup, rather than through every call that times out. It covers startup; waiting sends cover the moments of a
restart, later.
