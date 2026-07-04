# Changelog

All notable changes to Compze.Threading will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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
