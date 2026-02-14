# Code Review: Compze.Utilities.SystemCE.ThreadingCE

## Bugs

### 1. Monitor lock leak in `TryTakeLockWhen` (PR #73 — not yet merged)

**File:** `ResourceAccess/IMonitorCE.MonitorCE.cs` (lines 94–105)  
**Severity:** High

If `ThreadInterruptedException` is thrown during `Monitor.Wait` inside `ReleaseLockAndReacquireItOnPulseOrTimeout`, the monitor is re-acquired by .NET but never released, permanently leaking the lock. The same leak occurs if the user-supplied `condition()` delegate throws.

The fix from PR #73 (wrapping the while loop in `try { ... } catch { ReleaseLock(); throw; }`) is correct.

### 2. `AsyncLockCE` — reentrancy counter corruption on timeout

**File:** `Async/AsyncLockCE.cs` (lines 57–65 and 72–80)  
**Severity:** High

In both `LockedAsync` and `Locked`, the `Exit()` cleanup is registered via `AsyncDisposable`/`Disposable` **before** the semaphore is acquired. When `_semaphore.WaitAsync` times out, `RegisterTimeoutException()` throws, and `Exit()` still fires via the disposable. This decrements `_lockEntranceCount` from 0 to -1, corrupting the reentrancy counter for all future operations on that `AsyncLocal` flow.

**Suggested fix:** Move the disposable registration to after the semaphore is successfully acquired, or guard `Exit()` against negative counts.

### 3. `AsyncLockCE` — `AsyncLocal` reentrancy leaks into child tasks

**File:** `Async/AsyncLockCE.cs` (line 34)  
**Severity:** Medium

`AsyncLocal<int>` copies values from parent to child async contexts. If code inside `lockedAction` spawns a child `Task`, that child sees `_lockEntranceCount.Value == 1` and skips acquiring the semaphore — even though it may run on a different thread that does NOT hold the semaphore. This is a correctness risk if callers spawn fire-and-forget tasks inside locked sections.

---

## Design Concerns

### 4. `Disposable` is not idempotent

**File:** `Testing/Disposable.cs`  
**Severity:** Low

`Dispose()` calls `_action()` unconditionally every time. Double-dispose executes the action twice. The `_readLock` and `_updateLock` in `MonitorCE` are singleton instances reused for every lock acquisition, so this is by design there. But `Disposable` is also used in `GatedCodeSection.Enter()`, `ThreadGate.LogMethodEntryExit()`, and `AsyncLockCE.Locked` — some of those callers might accidentally double-dispose.

### 5. `MutexCE.ExecuteWithLock` — `AbandonedMutexException` not handled

**File:** `MachineWideSingleThreaded.cs` (line 23)  
**Severity:** Low

`_mutex.WaitOne()` can throw `AbandonedMutexException` if the previous owner crashed without releasing. The current code propagates this as a failure. Depending on intent, you may want to catch it and proceed — the mutex IS acquired when this exception is thrown.

### 6. `MonitorCEExtensions` — unused `timeout` parameter

**File:** `ResourceAccess/MonitorCEExtensions.cs` (lines 29 and 36)  
**Severity:** Low

The `ReadOrUpdate` overloads declare a `timeout` parameter but never pass it through to the underlying `Read`/`Update` calls. Callers setting a timeout get no effect.

### 7. `SingleTransactionUsageGuard` — unsynchronized field access

**File:** `SingleTransactionUsageGuard.cs` (lines 11–18)  
**Severity:** Low

`_transaction` is read and written without synchronization. If `EnsureAccessValid()` is called from two threads concurrently when `_transaction` is still `null`, there is a data race. Given this class exists to prevent multi-threaded use, this may be acceptable by design — but the guard itself is the component that should be safe to call from multiple threads to detect the violation.

---

## Summary

| # | Severity | Location | Issue |
|---|----------|----------|-------|
| 1 | **High** | `IMonitorCE.MonitorCE.cs` | Lock leak on exception in `TryTakeLockWhen` |
| 2 | **High** | `AsyncLockCE.cs` | `Exit()` fires on timeout, corrupting `_lockEntranceCount` |
| 3 | **Medium** | `AsyncLockCE.cs` | `AsyncLocal` reentrancy leaks into child tasks |
| 4 | **Low** | `Disposable.cs` | Not idempotent on double-dispose |
| 5 | **Low** | `MachineWideSingleThreaded.cs` | `AbandonedMutexException` not handled |
| 6 | **Low** | `MonitorCEExtensions.cs` | `timeout` parameter unused |
| 7 | **Low** | `SingleTransactionUsageGuard.cs` | Unsynchronized read/write of `_transaction` |
