# Changelog

All notable changes to Compze.InterprocessObject will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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
