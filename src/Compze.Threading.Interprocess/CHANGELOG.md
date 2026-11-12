# Changelog

All notable changes to Compze.Threading.Interprocess will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **`IAwaitableMutex` implements the async condition waits** that Compze.Threading 0.11.0-alpha adds to `IAwaitableCriticalSection`: `TryAwaitAsync`, `TryUpdateWhenAsync`, `ReadWhenAsync` and `UpdateWhenAsync`. They poll the cross-process signal on the same `ISignalPollingPolicy` schedule as the synchronous waits, but sleep between polls with `Task.Delay`, so no thread is held while they wait. They probe for an abandoned mutex once a second, as the synchronous waits do, and when the wait timeout expires they evaluate the condition once more before giving up.
- **Fixed: a synchronous condition wait on an `IAwaitableMutex` gave up without evaluating its condition once more when its wait timeout expired.** A condition that had become true without an update to wake the wait, such as state changed outside the update lock, was reported as a timeout. Now it evaluates the condition once more first, as the in-process monitor's waits always have, so a wait that times out has seen its condition false.
- **`IAwaitableProcessShared<T>` implements the async condition waits** Compze.Threading 0.11.0-alpha adds to `IAwaitableShared<T>`, on its `IAwaitableMutex`.

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
