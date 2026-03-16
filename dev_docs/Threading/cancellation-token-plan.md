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

### Phase 1 — Test infrastructure

- Create `CancellationMechanism` enum: `ThreadInterrupt`, `CancellationToken`
- Create 2D matrix attributes for both `ICriticalSection` and `IAwaitableCriticalSection` (cancellation-specific, inheriting from `MatrixTheoryAttribute<TImplementation, CancellationMechanism>`)
- The factory exposes the expected exception type and a method to trigger cancellation, derived from the current `CancellationMechanism`

### Phase 2 — Restructure existing ThreadInterrupt tests

- Migrate `ICriticalSection_ThreadInterrupt_specification` and `IAwaitableCriticalSection_ThreadInterrupt_specification` to use the new 2D matrix
- Initially only the ThreadInterrupt dimension is populated (CancellationToken tests will fail until Phase 4-5)
- Rename: `*_ThreadInterrupt_specification` → `*_Cancellation_specification` (since it now covers both mechanisms)
- Run tests — all existing ThreadInterrupt tests must pass identically

### Phase 3 — Add CancellationToken to interfaces

Add `CancellationToken cancellationToken = default` to:

- `ICriticalSection`: `TakeLock`
- `IAwaitableCriticalSection`: `TakeReadLock`, `TakeUpdateLock`, `TakeReadLockWhen`, `TakeUpdateLockWhen`, `TryTakeReadLockWhen`, `TryTakeUpdateLockWhen`, `TryAwait`, and all default-implemented functional helpers (`Locked`, `Read`, `Update`, `ReadWhen`, `UpdateWhen`, `TryUpdateWhen`)
- `IShared<T>`: `Locked`
- `IAwaitableShared<T>`: `Read`, `ReadWhen`, `Update`, `UpdateWhen`, `TryUpdateWhen`, `Await`
- `IProcessShared<T>`, `IAwaitableProcessShared<T>`: same methods as their parent interfaces
- `InterprocessSignal.TryAwait`

### Phase 4 — Implement in MonitorCE

- Condition waits: `CancellationToken.Register(() => Monitor.PulseAll(lockObj))`, check token at top of wait loop
- Lock acquisition: short-interval `Monitor.TryEnter` loop with token check

### Phase 5 — Implement in AwaitableMutex / Mutex / InterprocessSignal

- `InterprocessSignal.TryAwait`: add `token.ThrowIfCancellationRequested()` in 1ms poll loop
- `AwaitableMutex.TryTakeLockWhen`: check token in 50ms signal-wait loop
- `Mutex` lock acquisition: short-interval `WaitOne` loop with token check

### Phase 6 — Light up the CancellationToken dimension

- The restructured tests from Phase 2 now automatically run for both mechanisms via the 2D matrix

### Phase 7 — Full test suite run
