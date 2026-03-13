# Test Coverage Gaps: Compze.Threading + Compze.InterprocessObject

Review of the full public API surface against existing test files. Matrix coverage (tracked in `test-matrix-coverage.md`) is complete — these are gaps outside the matrix system.

---

## Gap 1: `IPollingAwaitableMutex.PollingInterval` property — untested

`IPollingAwaitableMutex` adds `PollingInterval PollingInterval { get; }` beyond `IAwaitableMutex`. No test verifies that the factory-specified polling interval is stored and exposed correctly (the way `MutexCE_specification` tests `LockTimeout` and `IAwaitableProcessShared_specification` tests `WaitTimeout`).

**Lock behavior** is fully covered through `[IAwaitableCriticalSectionMatrix]` and `[IAwaitableProcessSharedMatrix]`, but the interface-specific property is not.

**Where:** Would fit in a new `IPollingAwaitableMutex_specification` or alongside the existing `IAwaitableProcessShared_specification`.

---

## Gap 2: `ISignalingAwaitableMutex` — no interface-specific spec

Same situation as Gap 1. Lock behavior is covered through matrices, but there's no test verifying signaling-specific concerns. Unlike polling, signaling has no additional properties, so this is lower priority — the only signaling-specific thing would be verifying that an invalid/missing directory is rejected, but `InterprocessSignal_specification` already tests that for the internal component.

**Verdict:** Low priority. Signaling-specific validation is tested at the `InterprocessSignal` level.

---

## Gap 3: `IInterprocessObject<T>.Delete()` — untested as behavior

`MachineWideSharedObjectTests.CreateAndDeleteFileWhenTestCompletes()` calls `.Delete()` in cleanup, but no test asserts:
- After Delete, a new instance with the same name gets the default value (not stale data)
- After Delete, the backing file/MMF is actually gone

---

## Gap 4: `CorruptionAction.ReplaceContentWithDefaultAndThrow` — untested

All test factories use `CorruptionAction.ThrowException`. The alternative behavior — replace corrupt content with the default, then throw — is never exercised. This is a real code path in `InterprocessObjectImplementation` that handles deserialization failures.

---

## Gap 5: `DoubleCheckedLocking` extension methods — untested

Both `ICriticalSection` and `IAwaitableMonitor` expose a `DoubleCheckedLocking<TResult>` extension method with non-trivial concurrent semantics (optimistic read → lock → recheck pattern). No tests cover:
- Returns cached value on second call without taking the lock
- Calls `updateOnFailedRead` exactly once under concurrent access
- Multiple threads racing to populate all see the same result

---

## Gap 6: `IProcessShared_specification` uses `[XF]` with manual nested classes, not `[IProcessSharedMatrix]`

The `test-matrix-coverage.md` doc says `IProcessShared_specification` uses `[IProcessSharedMatrix]`, but the actual code uses `[XF]` with manually duplicated `Global` and `Local` nested classes. The coverage is equivalent (both scopes tested), but it doesn't use the matrix attribute that was specifically created for this purpose.

---

## Gap 7: `IMutex` — abandoned mutex callback never tested in the positive case

`MutexCE_specification.Locked_with_onAbandonedMutex_callback` tests that the callback is NOT invoked when the mutex isn't abandoned. But no test verifies:
- The callback IS invoked when another process dies while holding the mutex

This is inherently difficult to test (requires killing a process holding a mutex). May not be worth the complexity.

---

## Gap 8: `IMutex` — Local scope cross-instance synchronization untested

`Two_IMutex_instances_with_the_same_Global_name.synchronize_with_each_other` exists for Global. No equivalent test for Local scope — verifying that two `IMutex.Local()` instances with the same name synchronize within the same session.

---

## Gap 9: `LockTimeout` / `WaitTimeout` / `PollingInterval` — no dedicated value type specs

These strongly-typed duration wrappers have factory methods, operators (`==`, `!=`), implicit conversions to `TimeSpan`, `ToString()`, and validation logic (e.g., `LockTimeout` rejects `InfiniteTimeSpan`). None of this is systematically tested. They're exercised indirectly through lock specs, but edge cases aren't covered:
- `LockTimeout` constructor throws on `Timeout.InfiniteTimeSpan`
- Equality operators
- Implicit `TimeSpan` conversions
- `PollingInterval.Default` value
- `WaitTimeout.Infinite` and `WaitTimeout.IsInfinite`

---

## Priority Assessment

| Gap | Impact | Effort | Priority |
|-----|--------|--------|----------|
| 1. PollingInterval property | Low — property exposure only | Small | Low |
| 2. SignalingAwaitableMutex spec | Very low — no extra surface | Minimal | Skip |
| 3. Delete() behavior | Medium — untested mutation | Small | Medium |
| 4. CorruptionAction | Medium — untested error path | Medium | Medium |
| 5. DoubleCheckedLocking | Medium — concurrent pattern | Medium | Medium |
| 6. IProcessShared matrix usage | Cosmetic — coverage is equivalent | Small | Low |
| 7. Abandoned mutex positive | Low — hard to test reliably | Hard | Skip |
| 8. Local mutex cross-instance | Low — symmetric with Global | Small | Low |
| 9. Value type specs | Low — indirectly covered | Medium | Low |
