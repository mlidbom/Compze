# Containers: a synchronous Dispose throws on components that are only async-disposable

Decided by Magnus 2026-11-14, to be done after the .NET 11 migration. Not started.

It also fixes the failure Deskmancer hit, described below.

## Why

- `MicrosoftContainer.Dispose()`, its scopes' `Dispose()`, and the `DisposableTracker` of the DryIoc and LightInject
  adapters block on `DisposeAsync` for components that are only `IAsyncDisposable`. That blocking holds a thread, which
  Compze.Tessaging and Compze.Threading have just stopped doing.
- Scope disposal runs inside async code: the async overloads of `IScopeFactoryUnitOfWorkRunner`, and the inbox's
  handling loop. There a truly async disposal parks a pool thread, which is how the starvation of 2026-11-12 came about.
- A caller blocked on a thread that the disposal needs deadlocks.
- Deskmancer hit it. Its UI thread blocked in `MicrosoftContainer.Dispose()`. Meanwhile Microsoft's `DisposeAsync`,
  once one disposal didn't complete synchronously, disposed the rest of the graph on the thread pool.
  - That disposal was most likely Compze.Tessaging's `TeventObservationDispatcher`. The Deskmancer session found it, by
    reading the code; it hasn't been run.
  - Since 2026-11-12 (2969672b9, released in Compze.Tessaging 0.6.4 and 0.7.0) the dispatcher is `IAsyncDisposable`
    too, and drains its observer queues asynchronously. Before, it was only `IDisposable`, and the drain blocked on the
    calling thread.
  - Microsoft's `DisposeAsync` prefers a component's `DisposeAsync`. So a drain with work left let the rest of the graph
    be disposed on the pool.
- How it came about: two specifications in `When_resolving_a_tracked_transient`,
  `async_disposable_only_instances_are_disposed_when_container_is_disposed` and
  `async_disposable_only_instances_are_disposed_when_scope_is_disposed`, pin a synchronous `Dispose` that disposes
  async-only components. They were added on 2026-03-07 as missing tests, and the adapters were then written to pass
  them. Nothing records a decision on whether a synchronous `Dispose` should accept such components.

## What the underlying containers do

Decompiled 2026-11-14, from the versions Compze uses:

- **Microsoft.Extensions.DependencyInjection 9.0.9:** `ServiceProviderEngineScope.Dispose()` throws
  `InvalidOperationException` at the first async-only service, leaving the services it hasn't reached undisposed. Its
  `DisposeAsync` continues with `ConfigureAwait(false)` after the first `DisposeAsync` that doesn't complete
  synchronously.
- **Autofac 8.3.0:** `Disposer.Dispose` writes a trace warning, blocks on `Task.Run(DisposeAsync)`, and carries on on the
  calling thread.
- **LightInject 7.0.1:** `Scope.Dispose()` throws. `ServiceContainer` has no `DisposeAsync`, and disposes only
  `IDisposable`.
- **DryIoc 6.0.0-preview-09:** no `IAsyncDisposable` support. Its scopes track only `IDisposable`.

So under the DryIoc and LightInject adapters, an async-only singleton or scoped component is most likely never disposed,
synchronously or asynchronously. That is reasoned from the decompiled code and hasn't been run. No specification
covers it.

## The change

- **A synchronous `Dispose` throws** `InvalidOperationException`, saying to use `DisposeAsync`, when any component the
  container or `IScope` would dispose is only `IAsyncDisposable`. It checks before disposing anything.
- **Otherwise a synchronous `Dispose` is truly synchronous,** and its documentation says so: it calls each component's
  `Dispose()` on the calling thread, in reverse creation order. The Microsoft adapter calls `ServiceProvider.Dispose()`.
  - Deskmancer relies on this for its graph bound to the UI thread. The container is what knows the order that graph's
    dependencies need: a teardown written by hand would drift (the Deskmancer session, 2026-11-14).
  - A component that is both `IDisposable` and `IAsyncDisposable` gets its `Dispose()`.
    `TeventObservationDispatcher.Dispose()` blocks the calling thread until its observers have drained, so an observer
    that needs the calling thread would deadlock.
- **`IScope` gains `DisposeAsync`,** and Compze's async callers use `await using`.
- **Compze tracks the components it creates.** That is what it takes for all four adapters to behave the same: Autofac
  blocks rather than throws, and DryIoc and LightInject can't dispose async-only components at all.
- **Disposing in reverse creation order** can then be pinned in all four adapters.
- **Open:** Microsoft's container disposes whatever its factories return, and there is no way to opt out. The tracking
  must avoid disposing twice.
- **Specifications under `[DependencyInjectionContainerMatrix]`:**
  - start with ones that show today's behavior, the never-disposed async-only singleton included;
  - flip the two above.
- **Changelog entries** for Compze.DependencyInjection and the four adapters. Consumers, Deskmancer and JAStudio among
  them, move to `DisposeAsync` wherever they have async-only components.

## Not part of it

`DisposeAsync` promises no thread: Microsoft's and Autofac's asynchronous disposal continue wherever an await completes.

Stop logic doesn't belong in disposal. Magnus, 2026-11-14: "Deskmancer should have call Stop or something similar on
that type of component before disposing the composion root stuff". A stop "most likely disposes nothing": the point is
"to make sure that disposal was not responsible for the Stop logic".

- **Deskmancer agrees.** Draining and stopping Tessaging, and `AppShell`'s steps that quiet the reactors and remove the
  hooks, become a named stop before the root is disposed. Disposing the graph stays with the synchronous `Dispose`.
- **Compze's side of it is `a-public-stop-for-a-local-tessaging-engine-without-an-endpoint.md`.** Today the drain of a
  plain container's LocalTessagingEngine runs only in disposal.
