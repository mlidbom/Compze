# CancellationToken Support — Implementation Plan

## Goal

Add `CancellationToken` support to all blocking operations in the threading and interprocess object libraries.

## API Shape

Optional parameter alongside existing timeouts, backwards compatible. `CancellationToken` comes first among the optional parameters — it's the one callers actually pass at call sites, while timeouts are structural defaults rarely overridden:

```csharp
// ICriticalSection
ILock TakeLock(CancellationToken cancellationToken = default, LockTimeout? timeout = null);

// IAwaitableCriticalSection
IUpdateLock TakeUpdateLockWhen(Func<bool> condition, CancellationToken cancellationToken = default,
    WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null);
```

When triggered: throw `OperationCanceledException`.

## Bridging to Monitor

| Scenario | Mechanism |
|---|---|
| **Condition waits** (`Monitor.Wait`) | `CancellationToken.Register` → `Monitor.PulseAll` wakes the waiting thread; check token at top of wait loop |
| **Lock acquisition** (`Monitor.TryEnter`) | Short-interval `Monitor.TryEnter(obj, smallTimeout)` loop with token check between iterations |

## Bridging to Mutex / InterprocessSignal

| Scenario | Mechanism |
|---|---|
| `InterprocessSignal.TryAwait` | Check token in existing 1ms poll loop |
| `AwaitableMutex` condition waits | Check token in existing 50ms signal-wait loop |
| `Mutex` lock acquisition | Short-interval `WaitOne` loop with token check |

## Test Design: 2D Cancellation Matrix

ThreadInterrupt and CancellationToken are semantically symmetric — same behavioral contract, different trigger mechanism and exception type. A 2D matrix attribute captures this:

**Dimension 1 — Implementation:** Monitor, GlobalMutex, LocalMutex
**Dimension 2 — Cancellation mechanism:** ThreadInterrupt, CancellationToken

The matrix attribute inherits from `MatrixTheoryAttribute<TImplementation, CancellationMechanism>` using the existing 2-component matrix infrastructure. The factory uses the cancellation mechanism dimension to determine:
- How to trigger cancellation (call `thread.Interrupt()` vs `cts.Cancel()`)
- What exception type to assert (`ThreadInterruptedException` vs `OperationCanceledException`)

## Phases

### Phase 1 — Test infrastructure (DONE)

- `CancellationMechanism` enum (`ThreadInterrupt`, `CancellationToken`) in `TestInfrastructure/`
- 2D matrix attributes: `ICriticalSectionCancellationMatrixAttribute`, `IAwaitableCriticalSectionCancellationMatrixAttribute` — inherit `MatrixTheoryAttribute<Implementation, CancellationMechanism>`
- `CancellationFactory` exposes expected exception type and `CancellationTrigger` (abstracts thread interrupt vs CTS)

### Phase 2 — Restructure existing ThreadInterrupt tests (DONE)

- Migrated to 2D matrix, renamed `*_ThreadInterrupt_specification` → `*_Cancellation_specification`
- CancellationToken dimension skipped via `SkipValues` in attribute constructors (remove when Phase 4-5 land)
- TODO comments in test methods replaced with actual `cancellationTrigger.Token` calls in Phase 3

### Phase 3 — Add CancellationToken to interfaces (DONE)

Added `CancellationToken cancellationToken = default` to all blocking methods:

- `ICriticalSection`: `TakeLock`
- `ICriticalSection.Functional`: `Locked`
- `IAwaitableCriticalSection`: `TakeReadLock`, `TakeUpdateLock`, `TakeReadLockWhen`, `TakeUpdateLockWhen`, `TryTakeReadLockWhen`, `TryTakeUpdateLockWhen`
- `IAwaitableCriticalSection.Functional`: `Read`, `Update`, `ReadWhen`, `UpdateWhen`, `TryUpdateWhen`
- `IAwaitableCriticalSection.Awaiting`: `TryAwait`
- `IShared<T>`: `Locked`
- `IAwaitableShared<T>`: `Read`, `ReadWhen`, `Update`, `UpdateWhen`, `TryUpdateWhen`, `Await`
- `InterprocessSignal.TryAwait`
- `IInterprocessObject.Implementation`: all 5 methods

Implementations (MonitorCE, MutexCE, AwaitableMutex) accept the parameter but ignore it. All callers updated to use named parameters where needed. Test TODO markers replaced with actual `cancellationTrigger.Token` arguments. SkipValues updated to "not yet implemented in MonitorCE/MutexCE".

### Phase 4 — Implement in MonitorCE (DONE)

- `ThinMonitorWrapper.TryTakeLock`: when token is cancellable, polls with 50ms `Monitor.TryEnter` intervals checking token between iterations
- `TryTakeLockWhen` condition waits: caps `Monitor.Wait` interval to 50ms when token is cancellable, checks token at top of each loop iteration
- Non-cancellable paths unchanged — zero overhead when `CancellationToken.None` is passed

### Phase 5 — Implement in AwaitableMutex / Mutex / InterprocessSignal (DONE)

- `InterprocessSignal.TryAwait`: `ThrowIfCancellationRequested` added to existing 1ms poll loop
- `AwaitableMutex.TryTakeLockWhen`: token threaded to `_mutex.TakeLock`, `_signal.TryAwait`, and checked at top of condition loop
- `MutexCE.TakeLock`/`TryTakeLockCore`: when token is cancellable, polling interval capped to 50ms (min of cancellation and interrupt intervals)

### Phase 6 — Light up the CancellationToken dimension (DONE)

- Removed `SkipValues` from both `ICriticalSectionCancellationMatrixAttribute` and `IAwaitableCriticalSectionCancellationMatrixAttribute`
- CancellationToken tests now run for all implementations: Monitor, GlobalMutex, LocalMutex

### Phase 7 — Full test suite run (DONE)

- 2101 passed, 0 failed — CancellationToken dimension fully lit up (10+ new test cases running)
