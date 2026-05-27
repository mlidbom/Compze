# Changelog

All notable changes to Compze.InterprocessObject will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.5.0-alpha

### Added

- **`IInterprocessObject<T>` — strongly-typed cross-process shared state.** Changes made by one process are immediately visible to all others. Persisted to disk; survives process restarts and reboots. Atomic reads and updates protected by an `IAwaitableMutex` from Compze.Threading, so consumers can `TakeUpdateLockWhen(condition)` and have the wait semantics work across processes — `Monitor.Wait`-equivalent behavior over machine-wide shared state.
- Memory-mapped file backing (`MemoryMappedBinaryFile`) for high-performance reads.
- Automatic corruption recovery (`CorruptionAction`).
- Pluggable serialization via `IInterprocessObjectSerializer<T>`. MemoryPack support shipped separately as `Compze.InterprocessObject.MemoryPack`.

Built on Compze.Threading 0.5.0-alpha's cross-process condition-awaiting primitives.
