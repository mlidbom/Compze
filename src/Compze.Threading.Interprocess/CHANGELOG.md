# Changelog

All notable changes to Compze.Threading.Interprocess will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.9.0-alpha

- **`IProcessSemaphore`: a counting semaphore between processes, whose units belong to their holders.** `IProcessSemaphore.New(name, units, directory)` makes it, and every process that makes one of the same name in the same directory has the same semaphore. `TryTake`, `Take`, `TryTakeAsync` and `TakeAsync` take a number of its units for a purpose, waiting at most a `WaitTimeout`, and return an `IProcessSemaphoreHold`, which any thread may dispose to release them. `DescribeHoldersAndWaiters` says who holds units and who waits for them, for a holder that tells of a long wait.
  - **A take of several units takes them all, or none,** so two holders never each hold a part of what the other needs, and **takes are served in the order they came,** so a take of many units isn't starved by takes of few: a take waits while an earlier one waits, even when there are units enough for it.
  - **The operating system releases the units of a holder whose process dies, and a take whose process dies waiting gives up its place,** so a crashed or killed process never leaves units held, nor the takes after it waiting. Windows' named `Semaphore` has no holders: units a dead process held are gone until it is made anew.
  - **A `Take` that gives up throws a `TimeoutException` saying who holds the units and who waits ahead:** how many units, what for, which process, and since when.
  - **Each unit is a lock file, held as an `IProcessLock`'s is,** and the waiting takes are in a queue in an `IInterprocessObject<T>` beside them. A release ends the waits at once; the death of a holder or of a waiting take tells nothing, so a waiting take also looks every second.
  - **All who use a semaphore at once use the same number of units:** one made with another number takes units only once none are held or waited for, and from then on the semaphore has that number; else its take throws an `InvalidOperationException`.
- **`IInterprocessObject<T>`, an object shared between processes, is here now, in the namespace `Compze.Threading.Interprocess.ResourceAccess`, beside the `IAwaitableProcessShared<T>` it extends.** It came from Compze.InterprocessObject, which is retired: it has no versions after 0.10.0-alpha. With it came `IInterprocessObject` (its factory), `IInterprocessObjectSerializer<T>` and `IInterprocessObjectSerializer` (the factory of its JSON serializer), and `CorruptionAction`, unchanged but for their namespace, which was `Compze.InterprocessObject`. To upgrade, replace a reference to Compze.InterprocessObject with one to this package, and `using Compze.InterprocessObject;` with `using Compze.Threading.Interprocess.ResourceAccess;`. The package now depends on Compze.Internals.SystemCE, as Compze.InterprocessObject did.

## 0.8.0-alpha

- **`IAwaitableMutex`'s condition waits time their `WaitTimeout` by `Stopwatch`, not by the system clock,** as `IAwaitableMonitor`'s are in Compze.Threading: setting the clock no longer shortens or lengthens a wait, nor the polling schedule between its signals.
- **`IProcessLock`: a lock between processes that belongs to its holder, not to a thread.** `IProcessLock.New(name, directory)` makes it, and every process that makes one of the same name in the same directory has the same lock. `TryTake`, `Take`, `TryTakeAsync` and `TakeAsync` take it for a purpose, waiting at most a `WaitTimeout`, and return an `IProcessLockHold`, which any thread may dispose to release it. So async code can hold it across awaits, and an xUnit fixture can take it as it is created and release it as it is disposed, which a lock on an `IMutex` can't do: only the thread that took a mutex may release it. The operating system releases the lock when its holder's process dies, so a process that crashed or was killed never leaves it held. A `Take` that gives up throws a `TimeoutException` saying what the lock is held for, by which process, and since when. The async takes hold no thread while they wait.
  - **The lock is a file in the directory,** which its holder keeps open: on Windows shared for reading, so that a virus scanner or an indexer reading it keeps no holder out; on Linux and macOS locked with `flock`, and there taking it checks that the file keeps a second holder out, and throws if it doesn't, as when .NET's file locking is turned off. Nothing may delete the file while it can be held.
  - **A wait tries every 50 milliseconds,** so the lock isn't fair: whoever tries first after a release takes it. Nor is it reentrant: a second take waits for the first hold, even on the same thread.
- **Requires newer minor versions of other Compze packages:** Compze.Threading 0.14.0-alpha (was 0.13.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.7.2-alpha

- **`IMutex.Global` is shared by every user's processes on Unix again, as its documentation says.** Since .NET 11, and 8.0.32, 9.0.21 and 10.0.13, .NET scopes a named mutex on Unix to its user unless told otherwise, so processes of different users each had a mutex of their own. Every process that shares a global mutex on Unix must use this version or a later one: an earlier one's mutex is its user's own. Its documentation now repeats .NET's warning that on Unix such a mutex should be shared only between users who trust each other.

## 0.7.1-alpha

- **Internal: the interprocess signal Compze.Teventive.MemoryImageStateStore needs to tell the memory images on a machine that another has committed.**
- **Requires newer minor versions of other Compze packages:** Compze.Threading 0.13.0-alpha (was 0.12.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.7.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Threading 0.12.0-alpha (was 0.11.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.6.0-alpha

- **`IAwaitableMutex` implements the async condition waits** that Compze.Threading 0.11.0-alpha adds to `IAwaitableCriticalSection`: `TryAwaitAsync`, `TryUpdateWhenAsync`, `ReadWhenAsync` and `UpdateWhenAsync`. They poll the cross-process signal on the same `ISignalPollingPolicy` schedule as the synchronous waits, but sleep between polls with `Task.Delay`, so no thread is held while they wait. They probe for an abandoned mutex once a second, as the synchronous waits do, and when the wait timeout expires they evaluate the condition once more before giving up.
- **Fixed: a synchronous condition wait on an `IAwaitableMutex` gave up without evaluating its condition once more when its wait timeout expired.** A condition that had become true without an update to wake the wait, such as state changed outside the update lock, was reported as a timeout. Now it evaluates the condition once more first, as the in-process monitor's waits always have, so a wait that times out has seen its condition false.
- **`IAwaitableProcessShared<T>` implements the async condition waits** Compze.Threading 0.11.0-alpha adds to `IAwaitableShared<T>`, on its `IAwaitableMutex`.
- **Requires newer minor versions of other Compze packages:** Compze.Threading 0.11.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.1-alpha

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.5.0-alpha

Extracted from Compze.Threading 0.7.0-alpha into its own package so the young cross-process tier can version
independently of the mature in-process one. The code is unchanged by the extraction; namespaces are unchanged
(`Compze.Threading.Interprocess`). It starts at 0.5.0-alpha to reflect the maturity of the extracted code rather than resetting to a fresh 0.1.0. History before the extraction lives in the Compze.Threading changelog.

### Added

- **`IAwaitableMutex` — `Monitor.Wait` semantics across processes.** The full `IAwaitableCriticalSection` model (read/update locks, `TakeReadLockWhen(condition)`, `TakeUpdateLockWhen(condition)`, `TryTake*` variants) across process boundaries, backed by a system `Mutex` and a file-based change-counter signal. Releases all lock nesting levels while waiting and reacquires on signal, exactly like `Monitor.Wait`. `Global` (cross-session) and `Local` (single-session) variants.
- **`IMutex`** — a cross-process `ICriticalSection` backed by a named system `Mutex`, with `Global`/`Local` variants, cancellation-token support, abandoned-mutex recovery, and the same dual-stack-trace deadlock diagnostics as the in-process locks.
- **`IProcessShared<T>` / `IAwaitableProcessShared<T>`** — pair a shared object with a cross-process mutex for coordinating access to external resources (files, ports, databases) across processes.
- **`ISignalPollingPolicy`** — pluggable poll scheduling for cross-process condition-waits, trading signal-detection latency against CPU power draw. Adaptive backoff by default, capped at 50 ms.

### Depends on

- **Compze.Threading** for the shared `ICriticalSection` / `IAwaitableCriticalSection` / `IShared` / `IAwaitableShared` abstractions and the lock infrastructure both the in-process and cross-process implementations share.
