# Changelog

All notable changes to Compze.InterprocessObject will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

### Added

- **`TryReadWhen(condition, read, out result, …)`** (via `IAwaitableShared<T>`) — the read counterpart of `TryUpdateWhen`. Blocks until the condition holds or the timeout expires; on success runs `read` on the shared object within the still-held read lock, returns its value via `out`, and returns `true`; on timeout returns `false` without reading. Unlike `TryAwait` it keeps the lock across the read, so an expensive read (a full deserialize of the memory-mapped state) can be gated on a cheaply-evaluated condition.
- **`TryAwait(condition, cancellationToken, timeout)`** (via `IAwaitableShared<T>`) — blocks until the condition returns true for the shared object or the timeout expires; the condition re-reads fresh state on each cross-process change signal, so a waiter in any process observes another process's `Update` at signal latency without polling the object itself.

### Fixed

- **`Dispose` releases the backing file's memory mapping.** It only disposed the mutex, so the `.mmf` file stayed locked by the process until finalization — deleting the containing directory after disposing the object failed. The file itself still outlives disposal (shared with every other instance) until `Delete`.

## 0.6.0-alpha

### Added

- **`signalPollingPolicy` parameter on `NewGlobal`/`NewLocal`.** Passes an `ISignalPollingPolicy` (Compze.Threading) through to the backing `IAwaitableMutex`, letting consumers tune the cross-process condition-wait poll cadence — the latency-versus-CPU-power trade-off. Defaults to the adaptive backoff, so existing calls need no change and get lower idle power for free.

Built on Compze.Threading.Interprocess 0.5.0-alpha (extracted from Compze.Threading 0.7.0-alpha).

## 0.5.0-alpha

### Added

- **`IInterprocessObject<T>` — strongly-typed cross-process shared state.** Changes made by one process are immediately visible to all others. Persisted to disk; survives process restarts and reboots. Atomic reads and updates protected by an `IAwaitableMutex` from Compze.Threading, so consumers can `TakeUpdateLockWhen(condition)` and have the wait semantics work across processes — `Monitor.Wait`-equivalent behavior over machine-wide shared state.
- Memory-mapped file backing (`MemoryMappedBinaryFile`) for high-performance reads.
- Automatic corruption recovery (`CorruptionAction`).
- Pluggable serialization via `IInterprocessObjectSerializer<T>`. MemoryPack support shipped separately as `Compze.InterprocessObject.MemoryPack`.

Built on Compze.Threading 0.5.0-alpha's cross-process condition-awaiting primitives.
