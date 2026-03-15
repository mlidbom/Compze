# The goal
We want utilize matrix test attributes to run all tests for our interfaces against all implementations of those interfaces and all significant configuration variations of those interfaces to achive full test coverage without duplicating the same tests over and over for different implementations and configurations.

This document details the current state and what we need to change to reach that goal.



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

**Note:** `ICriticalSection` and `IAwaitableCriticalSection` are **parallel branches** under `ILockInfo`, not a parent-child relationship. The awaitable types do NOT implement `ICriticalSection`.

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

## All Concrete Implementations

### ICriticalSection implementations

| Implementation | Interface | Scope | Factory |
|---|---|---|---|
| MonitorCE | IMonitor | In-process | `IMonitor.New()` |
| MutexCE | IMutex | Global/Local | `IMutex.Global()` / `IMutex.Local()` |

Total unique ICriticalSection implementations: **3 variants** (MonitorCE + MutexCE×2)

### IAwaitableCriticalSection implementations

| Implementation | Interface | Scope | Factory |
|---|---|---|---|
| MonitorCE | IAwaitableMonitor | In-process | `IAwaitableMonitor.New()` |
| AwaitableMutexCE | IAwaitableMutex | Global/Local | `IAwaitableMutex.Global()` / `.Local()` |

Total unique IAwaitableCriticalSection variants: **3** (MonitorCE + AwaitableMutex×2)

### IShared\<T\> implementations

| Implementation | Wraps | Scope |
|---|---|---|
| Shared\<T\> | Any ICriticalSection | Generic |
| ThreadShared\<T\> | IMonitor | In-process |
| ProcessShared\<T\> | IMutex | Global/Local |

### IAwaitableShared\<T\> implementations

| Implementation | Wraps | Scope |
|---|---|---|
| AwaitableShared\<T\> | Any IAwaitableCriticalSection | Generic |
| AwaitableThreadShared\<T\> | IAwaitableMonitor | In-process |
| AwaitableProcessShared\<T\> (via Global) | IAwaitableMutex | Global |
| AwaitableProcessShared\<T\> (via Local) | IAwaitableMutex | Local |
| InterprocessObjectImplementation\<T\> (Global) | IAwaitableMutex + MMF | Global |
| InterprocessObjectImplementation\<T\> (Local) | IAwaitableMutex + MMF | Local |

Total unique IAwaitableShared\<T\> variants: **6**

---

## Current Matrix Attributes

### [ICriticalSectionMatrix] — `ICriticalSectionMatrixAttribute.Implementation` enum

| Value | Creates | Scope |
|---|---|---|
| `Monitor` | `IMonitor.New()` | In-process |
| `GlobalMutex` | `IMutex.Global()` | Global |
| `LocalMutex` | `IMutex.Local()` | Local |

**Used by:**
- `ICriticalSection_specification` — tests `ILock` (Locked, TakeLock, reentrancy, timeouts, contention)

**Coverage: Complete** — all ICriticalSection implementations (Monitor + Global/Local Mutex) are tested.

### [IAwaitableCriticalSectionMatrix] — `IAwaitableCriticalSectionMatrixAttribute.Implementation` enum

| Value | Creates | Scope |
|---|---|---|
| `Monitor` | `IAwaitableMonitor.New()` | In-process |
| `GlobalMutex` | `IAwaitableMutex.Global()` | Global |
| `LocalMutex` | `IAwaitableMutex.Local()` | Local |

**Used by:**
- `IAwaitableCriticalSection_specification` — tests TakeUpdateLock, TakeReadLock, TakeUpdateLockWhen, cursor-based waits
- `ContentionCount_specification` — tests contention tracking

**Coverage: Complete** — all IAwaitableCriticalSection implementations (Monitor + Global/Local Mutex) are tested.

### ~~[InterprocessObjectMatrix] — `InterprocessObjectBackingStore` enum~~ REMOVED

Removed: FileBacked backing store was eliminated. `MachineWideSharedObjectTests` now uses `[XF]` with `IInterprocessObject.NewGlobal()` (MMF) directly.

---

## Coverage Gap Analysis

### ~~Gap 1: ICriticalSection — RESOLVED~~

All ICriticalSection implementations are now in the matrix: Monitor, GlobalMutex, LocalMutex.

(The earlier analysis incorrectly assumed awaitable mutex types implement ICriticalSection. They don't — `ICriticalSection` and `IAwaitableCriticalSection` are parallel branches under `ILockInfo`.)

### ~~Gap 2: IAwaitableCriticalSection — RESOLVED~~

All IAwaitableCriticalSection implementations are now in the matrix: Monitor, GlobalMutex, LocalMutex.

### Gap 3: ~~IShared\<T\> — only tested as IThreadShared, not IProcessShared~~ RESOLVED

`IShared_specification` uses `[ISharedMatrix]` to test the full `IShared<T>` contract against `IThreadShared.New()`, `IProcessShared.Global()`, and `IProcessShared.Local()`. Covers Locked (Func and Action), mutual exclusion, and CriticalSection property.

`IThreadShared_specification` tests the `IThreadShared`-specific concern: the `Monitor` property.

### Gap 4: ~~IAwaitableShared\<T\> — no specification at all~~ RESOLVED

`IAwaitableShared_specification` uses `[IAwaitableSharedMatrix]` to test the full `IAwaitableShared<T>` contract against all 5 variants (Monitor, GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject). Covers:
- `Read` (Func and Action overloads)
- `Update` (Func and Action overloads)
- Full mutual exclusion — all access methods are blocked while any one holds the lock, all succeed after release
- `ReadWhen` — immediate, condition-wait, timeout
- `UpdateWhen` (Func and Action) — immediate, condition-wait, timeout
- `TryUpdateWhen` — true on immediate/eventual condition, false on timeout
- `Await` — immediate, condition-wait, timeout
- `CriticalSection` property — ContentionCount, independent instances

### ~~Gap 5: IInterprocessObject\<T\> — tested for persistence, not as IAwaitableProcessShared~~ RESOLVED

`IInterprocessObject<T>` is now tested as `IAwaitableShared<T>` and `IAwaitableProcessShared<T>` through the `[IAwaitableSharedMatrix]` and `[IAwaitableProcessSharedMatrix]` attributes, which both include Global and Local InterprocessObject variants.

`MachineWideSharedObjectTests` continues to test interprocess-object-specific concerns (persistence across dispose, data sharing) via `[XF]` using MMF-backed objects directly.

### ~~Gap 6: IAwaitableProcessShared\\<T\\> — no specification at all~~ RESOLVED

`IAwaitableProcessShared_specification` uses `[IAwaitableProcessSharedMatrix]` to test all 4 variants (GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject). Covers Mutex property (IsGlobal, Name, LockTimeout, WaitTimeout) and IDisposable. The `IAwaitableShared<T>` contract is covered by `IAwaitableShared_specification`.

`IAwaitableThreadShared_specification` tests the `IAwaitableThreadShared`-specific concern: the `Monitor` property.

### ~~Gap 7: IProcessShared — Global/Local not matrix-tested~~ RESOLVED

Duplicate `Locked` tests removed (covered by `IShared_specification`). Remaining tests assert scope-specific values (`Mutex.IsGlobal`, `Mutex.Name` prefix) — manual Global/Local classes are the correct structure. Unused `[IProcessSharedMatrix]` attribute deleted.

---

## Testing Philosophy

**Test at the abstraction level, not the implementation level.** Every interface gets full contract tests against all implementations, regardless of whether the current implementation is a thin wrapper. Reasons:

- Implementations may change — an `IAwaitableShared` might someday do its own locking instead of delegating to `IAwaitableCriticalSection`.
- Interactions between lock types and shared wrappers may have subtle glitches that can't be predicted by code inspection.
- Specifications should guarantee correctness independently at each level of the type hierarchy.

**No shortcuts based on implementation knowledge.** Even if `IShared.Locked()` currently just delegates to `ICriticalSection.Locked()`, the `IShared` spec still tests locking behavior fully. The specs are contracts, not implementation audits.

**Ordering:** Matrix attributes & specs first → full test coverage → stress tests & performance tests last.

---

## Plan: New Matrix Attributes

All planned matrix attributes have been implemented:
- `[ISharedMatrix]` — Monitor, GlobalMutex, LocalMutex
- ~~`[IProcessSharedMatrix]` — GlobalMutex, LocalMutex~~ (removed — tests are scope-specific, matrix not appropriate)
- `[IAwaitableSharedMatrix]` — Monitor, GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject
- `[IAwaitableProcessSharedMatrix]` — GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject

All factories use the real user-facing factory methods (`IThreadShared.New()`, `IProcessShared.Global/Local()`, `IAwaitableThreadShared.New()`, `IAwaitableProcessShared.Global/Local()`, `MemoryPackInterprocessObject.NewGlobal/NewLocal()`).

## Plan: New Specification Files

| Spec file | Tests interface | Matrix attribute | Status |
|---|---|---|---|
| `IShared_specification` | `IShared<T>` | `[ISharedMatrix]` | ✅ Done — Locked, CriticalSection property, mutual exclusion |
| `IThreadShared_specification` | `IThreadShared<T>` | `[XF]` | ✅ Done — Monitor property |
| `IProcessShared_specification` | `IProcessShared<T>` | `[XF]` Global/Local | ✅ Done — Mutex property (scope-specific assertions). IShared contract covered by IShared_specification |
| `IAwaitableShared_specification` | `IAwaitableShared<T>` | `[IAwaitableSharedMatrix]` | ✅ Done — Read, Update, mutual exclusion, ReadWhen, UpdateWhen, TryUpdateWhen, Await, CriticalSection property |
| `IAwaitableProcessShared_specification` | `IAwaitableProcessShared<T>` | `[IAwaitableProcessSharedMatrix]` | ✅ Done — Mutex property (IsGlobal, Name, LockTimeout, WaitTimeout), IDisposable. IAwaitableShared contract covered by IAwaitableShared_specification |

---

## Summary: What we have vs what we need

### Matrix attributes — current

| Attribute | Enum | Values | Dimensions | Tests against interface |
|---|---|---|---|---|
| `[ICriticalSectionMatrix]` | `ICriticalSectionMatrixAttribute.Implementation` | Monitor, GlobalMutex, LocalMutex | 3 | ICriticalSection |
| `[IAwaitableCriticalSectionMatrix]` | `IAwaitableCriticalSectionMatrixAttribute.Implementation` | Monitor, GlobalMutex, LocalMutex | 3 | IAwaitableCriticalSection |
| ~~`[InterprocessObjectMatrix]`~~ | ~~`InterprocessObjectBackingStore`~~ | ~~Removed~~ | ~~—~~ | ~~IInterprocessObject~~ |
| `[ISharedMatrix]` | `ISharedMatrixAttribute.Implementation` | Monitor, GlobalMutex, LocalMutex | 3 | IShared\<T\> |
| ~~`[IProcessSharedMatrix]`~~ | ~~`IProcessSharedMatrixAttribute.Implementation`~~ | ~~Removed~~ | ~~—~~ | ~~IProcessShared\<T\>~~ |
| `[IAwaitableSharedMatrix]` | `IAwaitableSharedMatrixAttribute.Implementation` | Monitor, GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject | 5 | IAwaitableShared\<T\> |
| `[IAwaitableProcessSharedMatrix]` | `IAwaitableProcessSharedMatrixAttribute.Implementation` | GlobalMutex, LocalMutex, GlobalInterprocessObject, LocalInterprocessObject | 4 | IAwaitableProcessShared\<T\> |

### Matrix attributes — planned

All planned matrix attributes have been implemented. None remaining.

### Interface coverage — current and planned

| Interface | Has specification? | Status |
|---|---|---|
| `ICriticalSection` | ✅ `ICriticalSection_specification` | **Complete** |
| `IAwaitableCriticalSection` | ✅ `IAwaitableCriticalSection_specification` | **Complete** |
| `IShared<T>` | ✅ `IShared_specification` | **Complete** — `[ISharedMatrix]` covers Monitor, GlobalMutex, LocalMutex |
| `IThreadShared<T>` | ✅ `IThreadShared_specification` | **Complete** — tests Monitor property (IShared contract covered by IShared_specification) |
| `IProcessShared<T>` | ✅ `IProcessShared_specification` | **Complete** — `[XF]` Global/Local tests Mutex property (scope-specific). IShared contract covered by IShared_specification |
| `IAwaitableShared<T>` | ✅ `IAwaitableShared_specification` | **Complete** — `[IAwaitableSharedMatrix]` covers all 5 variants (Monitor + 2 AwaitableMutex + 2 InterprocessObject) |
| `IAwaitableThreadShared<T>` | ✅ `IAwaitableThreadShared_specification` | **Complete** — tests Monitor property (IAwaitableShared contract covered by IAwaitableShared_specification) |
| `IAwaitableProcessShared<T>` | ✅ `IAwaitableProcessShared_specification` | **Complete** — `[IAwaitableProcessSharedMatrix]` covers all 4 variants (2 AwaitableMutex + 2 InterprocessObject). IAwaitableShared contract covered by IAwaitableShared_specification |
| `IInterprocessObject<T>` | ✅ `MachineWideSharedObjectTests` + matrix specs | **Complete** — persistence tested via `[XF]`; IAwaitableShared + IAwaitableProcessShared contracts tested via matrix specs |

### ~~Missing — scope dimension~~ RESOLVED

Both `ICriticalSectionMatrix` and `IAwaitableCriticalSectionMatrix` now cover Global/Local for all mutex types.
