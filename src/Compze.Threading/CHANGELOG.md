# Changelog

All notable changes to Compze.Threading will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

### Added

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
