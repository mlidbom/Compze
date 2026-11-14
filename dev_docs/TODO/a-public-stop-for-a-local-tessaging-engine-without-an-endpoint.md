# A public stop for a LocalTessagingEngine without an endpoint

Agreed with Magnus 2026-11-14. Mostly done the same day, for Deskmancer, in Compze.Tessaging 0.8.0-alpha: what remains
is the last item of the task.

## The problem

- **Draining is done only by `Endpoint`, or by disposal.** `TeventObservationDispatcher`, the engine's tevent
  observation dispatch and its only background machinery, drains its observer queues before it stops. An endpoint
  drains it in `IEndpoint.StopAsync`, after its listening stops and before its container tears down. That is already
  the pattern this task asks for: a stop that disposes nothing, run before disposal.
- **A plain container has no public way to drain or stop its engine.** That's a container that composes its
  LocalTessagingEngine through `LocalTessagingEngineRegistrar.LocalTessagingEngine`, with no endpoint, such as
  Deskmancer's worker. The LocalTessagingEngine has no public object to call a stop on. The container's disposal is its
  only lifecycle: the dispatcher's `Dispose` and `DisposeAsync` drain, and its documentation calls that "only the
  backstop for compositions without such a lifecycle".
- **So stop logic runs in disposal.** Magnus, 2026-11-14: a stop "most likely disposes nothing"; the point is "to make
  sure that disposal was not responsible for the Stop logic".

## What it cost

- **Deskmancer's teardown left the UI thread.** In Deskmancer's black-box test host, the drain had work left when the
  container was disposed. That made Microsoft's `DisposeAsync` continue on the thread pool, and Deskmancer's graph, which
  is bound to the UI thread, was disposed there. See `container-disposal-of-async-only-components.md`.
- **Deskmancer's named shutdown can't stop Tessaging,** except through testing machinery.

## The task

- **Done: a public stop for a LocalTessagingEngine in a plain container.** `ILocalTessagingEngine.StopAsync()`, resolved
  from the container, drains the observation dispatch, holding no thread, and disposes nothing.
- **Done: a publish after the stop fails loud.**
- **Done: specifications through the public API,** in `Given_a_container_composing_a_LocalTessagingEngine`:
  - observations queued before the stop have all been observed when it completes;
  - a publish afterwards throws;
  - a container disposed synchronously after the stop disposes its components on the calling thread. Without the stop,
    it fails with the Microsoft adapter, as Deskmancer's test host did.
- **Remaining: have `Endpoint` stop its engine through the same stop,** so there is one way to do it. Today
  `IEndpoint.StopAsync` calls the dispatcher's drain itself, and a publish after an endpoint's stop isn't refused.
