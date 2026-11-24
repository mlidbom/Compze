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

- **Every synchronous wait VSTHRD002 flags gets a same-line suppression with its reason, and a `todo:urgent` unless
  it is surely safe** (Magnus: "Suppress with reason AND urgent todo comments unless you are sure this is truly safe
  code.").
- **The synchronous waits through `TaskCE.WaitUnwrappingException` and `ResultUnwrappingException`, which VSTHRD002
  can't see, get a `todo:urgent` at each call** (Magnus: "Add urgent todos at the call site"). The two methods stay:
  they are not `GetAwaiter().GetResult()`, which throws only the first exception of a task that failed with several,
  where they rethrow the `AggregateException` with all of them.

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

- **Every synchronous wait marked, as decided:** VSTHRD002 at none.
  - **Suppressed only, as surely safe, 21:** the six delivery-stream waits in the send loops, on each stream's named
    thread; `SqliteEndpointProcessLockHold`'s own thread; `ThreadPoolCE`, which holds pool threads on purpose; the two
    bridge methods themselves, whose callers have the todos; and the 11 specifications whose subject is a synchronous
    wait, where nothing needs the waiting thread. `LockHeldByAnotherThreadWhileWeTimeOutTryingToTakeIt`'s holder now
    runs on a thread of its own, not a pool thread it blocked for the whole scenario.
  - **Suppressed with a `todo:urgent`, 16:** `ExactlyOnceDeliveryStream.LoadUndeliveredTessages`, which `Start()` calls
    on the thread that starts delivery, not the stream's own; publishing tevents synchronously, 2;
    `LocalTypermediaNavigatorSession`, 3; `IAsyncShared.Locked`; the DI containers' synchronous disposals, 4; the memory
    image state store's `Dispose`, 2; the AccountManagement sample's startup and shutdown, 3.
  - **A `todo:urgent` at each of the 14 calls of the two bridge methods.** Expression-bodied members among them got block
    bodies, so that the todo is at the wait, not in a doc comment that ships to consumers.

## What remains

Main's CA2213 in `DbConnectionPool.TransactionSpecificConnection`, reported to commit-as-the-decision. The `todo:urgent`
comments are the inventory of `synchronous-paths-that-need-a-free-thread-pool-thread.md`: a grep for `VSTHRD002` and
for `UnwrappingException()` lists every synchronous wait on a task in Compze's code.
