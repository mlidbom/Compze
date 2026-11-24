# The threading analyzers, down to zero warnings

Magnus, 2026-11-24: add Microsoft.VisualStudio.Threading.Analyzers everywhere, and land it on main only once the build is
back at zero warnings, so that every session's "C-Build reports 0 warnings" check keeps working meanwhile. Being done on
the branch `threading-analyzers-everywhere`, by state-store-in-tables.

## Why

Among its rules, VSTHRD002 flags a synchronous wait on a task, `.Wait()`, `.Result` or `GetAwaiter().GetResult()`,
wherever it is. No rule of `AnalysisLevel latest-all` does: CA1849 flags one only inside a method that returns a task.
Such a wait needs a free thread-pool thread, and stalls while the pool is busy: the memory image state store's deck flake
of 2026-11-24, and the starvation of 2026-11-12.

## Decided

- **In every project:** `src/Directory.Build.props`, which every project under src, test and samples imports, and the two
  release-check projects under `.github/scripts`. Private assets: no Compze package depends on it.
- **VSTHRD003 off everywhere** (Magnus): it guards against deadlocks with Visual Studio's JoinableTaskFactory, which Compze
  doesn't use.
- **VSTHRD200, the Async suffix, in library code only** (Magnus): off in specifications, whose names say what they
  specify. A public name that deliberately breaks it, such as Compze.Must's `InvokingAsync`, gets a same-line
  suppression with its reason; an override of a framework's method, which can't be renamed, too.

## Done

- **VSTHRD110, an awaitable result not observed:** none left. The inbox's handler executions returned a task no one
  awaited, which is gone (b1e7f0120). `TestingTaskRunner.Run` is `Start`, returning nothing, and `RunAsync` returns the
  task for a test that awaits one actor (ba7cb7891). Two `ContinueWithAsync` bookkeeping tasks are discarded with `_ =`.
- **VSTHRD200, the Async suffix in library code:** none left (7f7bda5a5). Public renames, in their changelogs:
  Compze.Internals.SystemCE's `TaskCE.RunAsync`, `RunOnDedicatedThreadAsync`, `ContinueWithAsync`,
  `WithAggregateExceptionsAsync` and `ToCompletedTaskFunc`; Compze.Tessaging.Hosting.Testing's `ConnectToAsync`,
  `AlsoConnectToAsync` and `DisposeWithoutWaitingForEndpointsToBeAtRestAsync`. Compze.Must's `InvokingAsync` keeps its
  name, with its suppression.
- **VSTHRD002 in specifications that could await:** the logging, Must, RunOnce, thread-pool tevent store and memory
  image state store specifications await, in `IAsyncLifetime` or `InitializeAsyncInternal` (185a3a0fb, 79f60e94e).

## What remains, 2026-11-24 (79f60e94e): 37 synchronous waits (VSTHRD002), and main's CA2213

A non-incremental build counts them: an incremental one repeats only the warnings of the projects it rebuilds.

- **On threads of their own, on purpose, 9:** the tessaging delivery streams, 7, on named threads
  (`RunOnNamedThreadAsync`), so they hold no pool thread; `SqliteEndpointProcessLockHold`, whose thread took the mutex
  and must release it; `ThreadPoolCE` in Compze.Internals.Testing, which holds pool threads on purpose.
- **A synchronous API over asynchronous work, 14:** publishing tevents synchronously, 2 (`TaggregateUnitOfWork`,
  `UnitOfWorkTeventPublisher`); `LocalTypermediaNavigatorSession`, 3; `IAsyncShared.Locked`; `TaskCE`'s
  `WaitUnwrappingException` and `ResultUnwrappingException`, 2; the DI containers' synchronous disposals, 4
  (`container-disposal-of-async-only-components.md`); the memory image state store's `Dispose`, 2. These are what
  `synchronous-paths-that-need-a-free-thread-pool-thread.md` is for.
- **Hidden from VSTHRD002, 14 more:** the callers of `WaitUnwrappingException` and `ResultUnwrappingException`, such as
  `EndpointHost.Start`, `NavigationSpecification.NavigateOn`, `RemoteTypermediaNavigator.Post` and `Get`, and
  `TeventObservationDispatcher.Dispose`. The analyzer sees only `.Wait()`, `.Result` and `GetAwaiter().GetResult()`,
  and has no setting for other blocking methods. `GetAwaiter().GetResult()` rethrows a single exception as they do.
- **Specifications whose subject is a synchronous wait, 11:** the locks' owning thread holds the lock while another
  thread's attempt is waited for, 8 (`ICriticalSection`, `IAwaitableCriticalSection`,
  `LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt`); `AsyncLockCE`'s synchronous reentry; the endpoint disposed
  synchronously to record the thread it disposes on; `ThreadGateTestFixture`, whose entrants block at the gate.
- **The AccountManagement sample's startup, 3:** under its `todo:urgent` on restructuring how it creates, starts and
  connects its endpoints.
