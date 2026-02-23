# Code Review: Compze.Utilities.SystemCE.ThreadingCE

## Bugs

### 3. `AsyncLockCE` — `AsyncLocal` reentrancy leaks into child tasks

**File:** `Async/AsyncLockCE.cs` (line 34)  
**Severity:** Medium

`AsyncLocal<int>` copies values from parent to child async contexts. If code inside `lockedAction` spawns a child `Task`, that child sees `_lockEntranceCount.Value == 1` and skips acquiring the semaphore — even though it may run on a different thread that does NOT hold the semaphore. This is a correctness risk if callers spawn fire-and-forget tasks inside locked sections.
