# Changelog

All notable changes to Compze.Threading will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.11.0-alpha

- **`TryUpdateWhen` can return what the update returns:** `IAwaitableCriticalSection.TryUpdateWhen(condition, func, out result, …)` and `IAwaitableShared<T>.TryUpdateWhen(condition, update, out result, …)`, the update counterparts of `TryReadWhen`. Both are default interface methods, so code that implements either interface needs no change.
- **The documentation says why reads and updates are declared apart, and what that asks of the code.** The declaration tells whoever reads the code whether the state changes there, and only releasing an update lock wakes the waits for a condition, since only an update can make a condition true. So code under a read lock must not change the state, and neither may a condition: a change there wakes no wait. All locks are exclusive, read locks included. And a wait for a condition waits forever by default: the deadlock diagnosis covers taking a lock.
- The README's examples that didn't compile are fixed: one called `IAwaitableThreadShared.WithDefaultTimeouts`, which doesn't exist (it is `New`), and others named a variable `lock`.
- **Async condition waits, which hold no thread while they wait.** `IAwaitableCriticalSection` gains `TryAwaitAsync` and `TryUpdateWhenAsync`, and the default interface methods `ReadWhenAsync` and `UpdateWhenAsync` (with an action, or with a func whose result it returns). While the condition is false nothing waits but a task, which the next update-lock release completes, so no thread is parked. A synchronous condition wait on a thread-pool thread holds that thread for as long as it waits, and enough of them starve the pool: the work they wait for can't get a thread to run on. Taking the lock stays synchronous and brief, and the condition and the action or func run under the lock, on whichever thread resumes the wait. `IAwaitableMonitor` implements them, and so does `IAwaitableMutex` from Compze.Threading.Interprocess 0.6.0-alpha.
- **`IAwaitableShared<T>` gains the same async condition waits:** `ReadWhenAsync`, `UpdateWhenAsync`, `TryUpdateWhenAsync` and `TryAwaitAsync`, with the default interface methods `UpdateWhenAsync` (with an action) and `AwaitAsync`. `IAwaitableThreadShared<T>` implements them on its monitor. Breaking only for code that implements `IAwaitableShared<T>` itself: the four are new abstract members.
- **A thread that holds the critical section can't start an async condition wait on it.** A synchronous wait releases its caller's lock while it waits and takes it back before it returns; an async wait may resume on another thread, which can do neither. So it fails at once with a `StateAssertionFailedException`, instead of waiting until its timeout for an update its own caller's lock prevents.
- Breaking only for code that implements `IAwaitableCriticalSection` itself: `TryAwaitAsync` and `TryUpdateWhenAsync` are new abstract members.
- A monitor, as `IMonitor.New` and `IAwaitableMonitor.New` create it, is 152 bytes instead of 144: the field the async waits need. What it holds is created only when an async wait waits.
- The internal `TryAwaitOnDedicatedThreadAsync`, which parked a dedicated thread for each waiting async caller, is gone, and Compze.Tessaging no longer sees this package's internals.

## 0.10.1-alpha

- A monitor, as `IMonitor.New` and `IAwaitableMonitor.New` create it, is four allocations and 144 bytes instead of nine and 384, so code can create one for each of a great many objects. The objects that release its lock call the monitor directly instead of through a delegate each, it locks on the object wrapping the .NET `Monitor` instead of on a separate object, it needs no lock to record lock timeouts, and its list of them starts out as the shared empty one. Taking and releasing a lock still allocates nothing.
- Fixed: when taking a monitor's lock timed out, the exception's message named the monitor's default timeout, even when the call had passed a timeout of its own.

## 0.10.0-alpha

- **Breaking:** `DoubleCheckedLocking` with `tryRead` gives its functions the field's value: `tryRead` is a `Func<TField, TResult?>` and `createUpdatedFieldValue` a `Func<TField, TField>`. `tryRead` gets the value read with acquire semantics, paired with the release that publishes an updated value (`Volatile.Write`, which replaces `Interlocked.Exchange`), so `createUpdatedFieldValue` may change other state too, and the warning against that is gone, as it went from the single-field overloads in 0.9.1-alpha. When `tryRead` finds nothing in the updated value, the call throws an `ArgumentException` instead of an `Exception`.
- `DoubleCheckedLocking`: a value creation that reads the value it is creating, directly or not, makes the call throw an `InsufficientExecutionStackException` when the stack is nearly used up. Every critical section lets the thread holding its lock enter it again, so the creation ran again and again until the stack overflowed, which ends the process. A check whether the thread already holds the lock can't replace this: creating another field guarded by the same critical section is legitimate.

## 0.9.1-alpha

- Fixed a deadlock in the condition waits of `IAwaitableCriticalSection` as implemented by `IAwaitableMonitor` (`TakeReadLockWhen`, `TakeUpdateLockWhen`, their `Try` forms, and the waits built on them) when given a cancellable token. A cancellation that landed while the waiting thread held the lock, such as while it evaluated its condition, deadlocked the waiting thread and the cancelling one permanently: the wait disposed its cancellation registration, which waits for a running callback, and the callback waited for the lock. The wait now unregisters the callback instead of waiting for it.
- `DoubleCheckedLocking` on a single field no longer forbids its factory to change state other than the value it creates. A thread that got the value without taking the lock could see that other state as it was before the factory ran. The lock-free read of the field is now an acquire, paired with the release that publishes the value (`Volatile.Read` and `Volatile.Write`, which replaces `Interlocked.Exchange`), so such a thread sees everything the factory wrote before returning it. The documentation now states the guarantees: the factory runs once, under the lock, and runs again on the next call if it throws.

## 0.9.0-alpha

- Internal async bridging for condition waits: `IAwaitableCriticalSection` gains the `_internal` extension `TryAwaitOnDedicatedThreadAsync`, awaiting the blocking `TryAwait` from async callers. The condition is first evaluated inline on the calling thread — in the common case it already holds, and awaiting must not cost a thread to discover that; only a wait that must actually block parks a thread, and the name says where: a dedicated thread, because a condition wait can span seconds and parked pool threads starve the pool. Internal deliberately: it belongs on the public surface once the API is declared ready for publishing; Tessaging's waiting sends consume it meanwhile (`InternalsVisibleTo`).
- The `IShared`/`IAwaitableShared` container interfaces are dissolved. They existed to keep an implementation out of sight, which is what the `_internal`/`_private` namespace sections do now.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.8.0-alpha

### Added

- **`IAwaitableCriticalSection.TryReadWhen(condition, func, out result, …)` and `IAwaitableShared<TShared>.TryReadWhen(condition, read, out result, …)`** — the read counterpart of `TryUpdateWhen`. Blocks until the condition holds or the timeout expires; on success runs the read within the read lock still held from evaluating the condition, returns its value via `out`, and returns `true`; on timeout returns `false` without reading. Unlike `TryAwait`, which releases the lock before returning, it keeps the lock held across the read, so an expensive read can be gated on a cheaply-evaluated condition.
- **`IAwaitableShared<TShared>.TryAwait(condition, cancellationToken, timeout)`** — the pure condition wait: blocks until the condition returns true for the shared object or the timeout expires, returning whether it did. Unlike `TryUpdateWhen` nothing is written when the condition passes, so waiters observing the shared object never wake each other.

## 0.7.0-alpha

### Changed

- **Moved the cross-process primitives to a new package, Compze.Threading.Interprocess.** `IMutex`, `IAwaitableMutex`, `IProcessShared`/`IAwaitableProcessShared`, the new adaptive-backoff `ISignalPollingPolicy`, and the file-based `InterprocessSignal` now live there, so the young cross-process tier can version independently of the mature in-process one. This package now holds only the in-process primitives (`IMonitor`, `IAwaitableMonitor`, `IThreadShared`, `IAwaitableThreadShared`, double-checked locking, `RunOnce`) and the shared `ICriticalSection`/`IAwaitableCriticalSection`/`IShared` abstractions that both tiers implement. Namespaces are unchanged, so cross-process consumers only add a reference to the new package.

## 0.6.0-alpha

### Fixed

- Removed spurious type parameter causing ambiguous method errors.

## 0.5.0-alpha

### Added

- **`IAwaitableMutex` — `Monitor.Wait` semantics across processes.** The full `IAwaitableCriticalSection` model (read/update locks, `TakeReadLockWhen(condition)`, `TakeUpdateLockWhen(condition)`, `TryTake*` variants) now works across process boundaries, backed by a system `Mutex` and a file-based `InterprocessSignal`. Mirrors `Monitor.Wait` exactly: releases all lock nesting levels while waiting and reacquires on signal. `Global` (cross-session) and `Local` (single-session) variants. As far as we know, no other .NET library exposes the full Monitor/condition-wait model over a cross-process mutex.
- **`IAwaitableProcessShared<T>`** — pairs a shared object with an `IAwaitableMutex` for coordinating access to external resources (files, ports, databases) across processes.
- `InterprocessSignal` and `InterprocessChangeCounter` — file-based primitives for cross-process notification. Conditions are only re-evaluated when an update lock is released, not on every poll interval.
- Cancellation token support throughout the public API.
- Abandoned-mutex recovery.

### Changed

- Renamed `IAwaitableMonitor` → `IAwaitableCriticalSection` so the abstraction reads correctly when applied to both in-process and cross-process implementations.
- Renamed `IMonitor` → `ILock` → `ICriticalSection` (final).
- Refactored the public API.
- README and full XML doc comments on every public type and method.

### Fixed

- Race conditions in awaitable mutex.
- `Thread.Interrupt` reliability on Linux (less aggressive polling).
- Default deadlock-detection stack-trace timeout.

## 0.3.0-alpha

- Major refactorings of the public interfaces
- README updates
- Project rename

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
