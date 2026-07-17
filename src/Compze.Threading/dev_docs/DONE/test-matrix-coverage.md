# Threading Test Matrix Coverage

Matrix test attributes run each interface's contract tests against all implementations, eliminating test duplication.

## Interface Hierarchy

### Lock / Critical Section

```
ILockInfo
├── ICriticalSection                  (TakeLock — exclusive lock)
│   ├── IMonitor                      (in-process)
│   └── IMutex                        (cross-process, Global/Local)
│
├── IAwaitableCriticalSection         (TakeUpdateLock, TakeReadLock, TakeUpdateLockWhen)
    ├── IAwaitableMonitor             (in-process)
    └── IAwaitableMutex               (cross-process, Global/Local, signaling-based)
```

`ICriticalSection` and `IAwaitableCriticalSection` are **parallel branches** under `ILockInfo`, not parent-child.

### Shared State

```
IShared<T>  ← wraps a T synchronized by an ICriticalSection
├── IThreadShared<T>                  ← wraps IMonitor
├── IProcessShared<T>                 ← wraps IMutex  (Global/Local)

IAwaitableShared<T>  ← wraps a T synchronized by an IAwaitableCriticalSection
├── IAwaitableThreadShared<T>         ← wraps IAwaitableMonitor
├── IAwaitableProcessShared<T>        ← wraps IAwaitableMutex  (Global/Local)
│   └── IInterprocessObject<T>        ← wraps IAwaitableMutex + MMF  (Global/Local)
```

---

## Matrix Attributes

| Attribute | Values | Tests interface |
|---|---|---|
| `[ICriticalSectionMatrix]` | Monitor, GlobalMutex, LocalMutex | ICriticalSection |
| `[IAwaitableCriticalSectionMatrix]` | Monitor, GlobalMutex, LocalMutex | IAwaitableCriticalSection |
| `[ISharedMatrix]` | Monitor, GlobalMutex, LocalMutex | IShared\<T\> |
| `[IAwaitableSharedMatrix]` | Monitor, GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject | IAwaitableShared\<T\> |
| `[IAwaitableProcessSharedMatrix]` | GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject | IAwaitableProcessShared\<T\> |

All factories use real user-facing factory methods (`IThreadShared.New()`, `IProcessShared.Global/Local()`, `IAwaitableThreadShared.New()`, `IAwaitableProcessShared.Global/Local()`, `MemoryPackInterprocessObject.NewGlobal/NewLocal()`).

---

## Specification Coverage

| Interface | Specification | Attribute | What it tests |
|---|---|---|---|
| `ICriticalSection` | `ICriticalSection_specification` | `[ICriticalSectionMatrix]` | Locked, TakeLock, reentrancy, timeouts |
| `ICriticalSection` | `ICriticalSection_ThreadInterrupt_specification` | `[ICriticalSectionMatrix]` | Thread interrupt during lock wait |
| `ICriticalSection` | `DoubleCheckedLocking_specification.On_ICriticalSection` | `[ICriticalSectionMatrix]` | Double-checked locking pattern |
| `IAwaitableCriticalSection` | `IAwaitableCriticalSection_specification` | `[IAwaitableCriticalSectionMatrix]` | TakeUpdateLock, TakeReadLock, TakeUpdateLockWhen, cursor-based waits |
| `IAwaitableCriticalSection` | `IAwaitableCriticalSection_ThreadInterrupt_specification` | `[IAwaitableCriticalSectionMatrix]` | Thread interrupt during lock wait (all lock types) |
| `IAwaitableCriticalSection` | `ContentionCount_specification` | `[IAwaitableCriticalSectionMatrix]` | Contention tracking |
| `IAwaitableCriticalSection` | `DoubleCheckedLocking_specification.On_IAwaitableCriticalSection` | `[IAwaitableCriticalSectionMatrix]` | Double-checked locking pattern |
| `IShared<T>` | `IShared_specification` | `[ISharedMatrix]` | Locked (Func/Action), mutual exclusion, CriticalSection property |
| `IThreadShared<T>` | `IThreadShared_specification` | `[XF]` | Monitor property |
| `IProcessShared<T>` | `IProcessShared_specification` | `[XF]` Global/Local | Mutex property (IsGlobal, Name, LockTimeout) — scope-specific assertions |
| `IAwaitableShared<T>` | `IAwaitableShared_specification` | `[IAwaitableSharedMatrix]` | Read, Update, mutual exclusion, ReadWhen, TryReadWhen, UpdateWhen, TryUpdateWhen, Await, CriticalSection property |
| `IAwaitableThreadShared<T>` | `IAwaitableThreadShared_specification` | `[XF]` | Monitor property |
| `IAwaitableThreadShared<T>` | `ContentionCount_specification` | `[XF]` | Monitor contention tracking via shared wrapper |
| `IAwaitableProcessShared<T>` | `IAwaitableProcessShared_specification` | `[IAwaitableProcessSharedMatrix]` | Mutex property (IsGlobal, Name, LockTimeout, WaitTimeout), IDisposable |
| `IInterprocessObject<T>` | `InterprocessObjectTests` | `[XF]` | Persistence across dispose, data sharing (IAwaitableShared/IAwaitableProcessShared contracts covered by matrix specs above) |

### Design decisions

- **`IProcessShared_specification` uses `[XF]` with manual Global/Local classes**, not a matrix attribute. The remaining tests assert different values per scope (`Mutex.IsGlobal` true vs false, `Mutex.Name` prefix `Global\` vs `Local\`), so a matrix would require branching on variant — worse than explicit classes.
- **`IInterprocessObject<T>`** has no dedicated matrix attribute. Its IAwaitableShared and IAwaitableProcessShared contracts are tested via `[IAwaitableSharedMatrix]` and `[IAwaitableProcessSharedMatrix]`, which both include InterprocessObject variants. `InterprocessObjectTests` covers interprocess-object-specific concerns only.

---

## Testing Philosophy

**Test at the abstraction level, not the implementation level.** Every interface gets full contract tests against all implementations, regardless of whether the current implementation is a thin wrapper.

**No shortcuts based on implementation knowledge.** Even if `IShared.Locked()` currently delegates to `ICriticalSection.Locked()`, the `IShared` spec still tests locking behavior fully. Specs are contracts, not implementation audits.
