# Test Coverage Gaps: Compze.Threading + Compze.InterprocessObject

Review of the full public API surface against existing test files. Matrix coverage (tracked in `test-matrix-coverage.md`) is complete — these are gaps outside the matrix system.

---

## ~~Gap 1: `IPollingAwaitableMutex.PollingInterval` property — untested~~ ELIMINATED

Polling awaitable mutex removed. `IAwaitableMutex` is now the sole implementation (signaling-based). No `PollingInterval` property exists.

---

## ~~Gap 2: `ISignalingAwaitableMutex` — no interface-specific spec~~ ELIMINATED

`ISignalingAwaitableMutex` was collapsed into `IAwaitableMutex`. There is now only one awaitable mutex interface. Signaling-specific validation is tested at the `InterprocessSignal` level.

---

## ~~Gap 3: `IInterprocessObject<T>.Delete()` — untested as behavior~~ RESOLVED

Test added in `MachineWideSharedObjectTests.After_Delete_a_new_instance_with_the_same_name_gets_the_default_value`.

---

## ~~Gap 4: `CorruptionAction.ReplaceContentWithDefaultAndThrow` — untested~~ RESOLVED

Tests added in `When_deserialization_fails` (in `MachineWideSharedObjectTests.cs`):
- `and_CorruptionAction_is_ReplaceContentWithDefaultAndThrow.throws_exception_mentioning_replacement` — verifies exception message describes the replacement
- `and_CorruptionAction_is_ReplaceContentWithDefaultAndThrow.replaces_content_with_default_so_next_read_succeeds` — verifies file is replaced and next read returns default
- `and_CorruptionAction_is_ThrowException.throws_exception_without_modifying_the_backing_file` — verifies the contrast: file untouched, next read returns previously-set value

---

## ~~Gap 5: `DoubleCheckedLocking` extension methods — untested~~ RESOLVED

Tests added in `DoubleCheckedLocking_specification` covering both `ICriticalSection` and `IAwaitableMonitor` variants:
- Returns cached value without calling update
- Calls update and returns populated value when initially null
- Throws when tryRead returns null even after update
- Concurrent callers: update runs exactly once, both get the same result

---

## Gap 6: `IProcessShared_specification` uses `[XF]` with manual nested classes, not `[IProcessSharedMatrix]`

The `test-matrix-coverage.md` doc says `IProcessShared_specification` uses `[IProcessSharedMatrix]`, but the actual code uses `[XF]` with manually duplicated `Global` and `Local` nested classes. The coverage is equivalent (both scopes tested), but it doesn't use the matrix attribute that was specifically created for this purpose.

---

## ~~Gap 7: `IMutex` — abandoned mutex callback never tested in the positive case~~ RESOLVED

Tests added in `MutexCE_specification.Locked_with_onAbandonedMutex_callback`:
- `invokes_callback_when_acquiring_an_abandoned_mutex` — verifies callback fires on uncontended abandon
- `acquires_the_lock_successfully_after_abandonment` — verifies lock acquisition succeeds after abandonment
- `invokes_callback_when_mutex_is_abandoned_while_waiting_for_it` — verifies callback fires when mutex is abandoned while another thread is waiting

Testing helper `IMutexCE.AbandonLock()` and `IMutexCE.HoldLockUntilAbandoned()` added to `Compze.Threading.Testing`.

---

## Gap 8: `IMutex` — Local scope cross-instance synchronization untested

`Two_IMutex_instances_with_the_same_Global_name.synchronize_with_each_other` exists for Global. No equivalent test for Local scope — verifying that two `IMutex.Local()` instances with the same name synchronize within the same session.

---

## Gap 9: `LockTimeout` / `WaitTimeout` — no dedicated value type specs

These strongly-typed duration wrappers have factory methods, operators (`==`, `!=`), implicit conversions to `TimeSpan`, `ToString()`, and validation logic (e.g., `LockTimeout` rejects `InfiniteTimeSpan`). None of this is systematically tested. They're exercised indirectly through lock specs, but edge cases aren't covered:
- `LockTimeout` constructor throws on `Timeout.InfiniteTimeSpan`
- Equality operators
- Implicit `TimeSpan` conversions
- `WaitTimeout.Infinite` and `WaitTimeout.IsInfinite`

---

## Priority Assessment

| Gap | Impact | Effort | Priority |
|-----|--------|--------|----------|
| 1. PollingInterval property | ~~Low~~ | ~~Small~~ | ~~ELIMINATED~~ |
| 2. SignalingAwaitableMutex spec | ~~Very low~~ | ~~Minimal~~ | ~~ELIMINATED~~ |
| 3. Delete() behavior | ~~Medium~~ | ~~Small~~ | ~~DONE~~ |
| 4. CorruptionAction | ~~Medium — untested error path~~ | ~~Medium~~ | ~~DONE~~ |
| 5. DoubleCheckedLocking | ~~Medium~~ | ~~Medium~~ | ~~DONE~~ |
| 6. IProcessShared matrix usage | Cosmetic — coverage is equivalent | Small | Low |
| 7. Abandoned mutex positive | ~~Low — hard to test reliably~~ | ~~Hard~~ | ~~DONE~~ |
| 8. Local mutex cross-instance | Low — symmetric with Global | Small | Low |
| 9. Value type specs | Low — indirectly covered | Medium | Low |
