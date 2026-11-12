# Changelog

All notable changes to Compze.Threading will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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
