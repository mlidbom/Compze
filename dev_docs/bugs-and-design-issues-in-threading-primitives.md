# Code Review: Compze.Utilities.SystemCE.ThreadingCE

## Bugs

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

### 5. `MutexCE.ExecuteWithLock` — `AbandonedMutexException` not handled

**File:** `MachineWideSingleThreaded.cs` (line 23)  
**Severity:** Low

`_mutex.WaitOne()` can throw `AbandonedMutexException` if the previous owner crashed without releasing. The current code propagates this as a failure. Depending on intent, you may want to catch it and proceed — the mutex IS acquired when this exception is thrown.

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
