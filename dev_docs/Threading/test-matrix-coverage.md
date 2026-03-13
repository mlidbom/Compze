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
    └── IAwaitableMutex               (cross-process, Global/Local)
        ├── IPollingAwaitableMutex    (condition-wait via polling)
        └── ISignalingAwaitableMutex  (condition-wait via OS events)
```

**Note:** `ICriticalSection` and `IAwaitableCriticalSection` are **parallel branches** under `ILockInfo`, not a parent-child relationship. The awaitable mutex types do NOT implement `ICriticalSection`.

### Shared State

```
IShared<T>  ← wraps ICriticalSection
├── IThreadShared<T>                  ← wraps IMonitor
├── IProcessShared<T>                 ← wraps IMutex  (Global/Local)

IAwaitableShared<T>  ← wraps IAwaitableCriticalSection
├── IAwaitableThreadShared<T>         ← wraps IAwaitableMonitor
├── IAwaitableProcessShared<T>        ← wraps IAwaitableMutex  (Global/Local × Polling/Signaling)
│   └── IInterprocessObject<T>        ← wraps ISignalingAwaitableMutex + file  (Global only)
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
| PollingAwaitableMutexCE | IPollingAwaitableMutex | Global/Local | `IPollingAwaitableMutex.Global()` / `.Local()` |
| SignalingAwaitableMutexCE | ISignalingAwaitableMutex | Global/Local | `ISignalingAwaitableMutex.Global()` / `.Local()` |

Total unique IAwaitableCriticalSection variants: **5** (MonitorCE + PollingMutex×2 + SignalingMutex×2)

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
| AwaitableProcessShared\<T\> (via GlobalPolling) | IPollingAwaitableMutex | Global |
| AwaitableProcessShared\<T\> (via LocalPolling) | IPollingAwaitableMutex | Local |
| AwaitableProcessShared\<T\> (via GlobalSignaling) | ISignalingAwaitableMutex | Global |
| AwaitableProcessShared\<T\> (via LocalSignaling) | ISignalingAwaitableMutex | Local |
| InterprocessObjectImplementation\<T\> (Create) | ISignalingAwaitableMutex + MMF | Global |
| InterprocessObjectImplementation\<T\> (CreateFileBacked) | ISignalingAwaitableMutex + File | Global |

Total unique IAwaitableShared\<T\> variants: **8**

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
| `GlobalPollingMutex` | `IPollingAwaitableMutex.Global()` | Global |
| `LocalPollingMutex` | `IPollingAwaitableMutex.Local()` | Local |
| `GlobalSignalingMutex` | `ISignalingAwaitableMutex.Global()` | Global |
| `LocalSignalingMutex` | `ISignalingAwaitableMutex.Local()` | Local |

**Used by:**
- `IAwaitableCriticalSection_specification` — tests TakeUpdateLock, TakeReadLock, TakeUpdateLockWhen, cursor-based waits
- `ContentionCount_specification` — tests contention tracking

**Coverage: Complete** — all IAwaitableCriticalSection implementations (Monitor + Global/Local × Polling/Signaling Mutex) are tested.

### [InterprocessObjectMatrix] — `InterprocessObjectBackingStore` enum

| Value | Creates |
|---|---|
| `File` | `IInterprocessObject.CreateFileBacked()` |
| `MemoryMapped` | `IInterprocessObject.Create()` (MMF) |

**Used by:**
- `MachineWideSharedObjectTests` — Create, Read/Update, data sharing, persistence, blocking

**Missing from matrix:**
- Doesn't test IInterprocessObject as an IAwaitableShared (all the IAwaitableShared behavior is untested for this implementation)

---

## Coverage Gap Analysis

### ~~Gap 1: ICriticalSection — RESOLVED~~

All ICriticalSection implementations are now in the matrix: Monitor, GlobalMutex, LocalMutex.

(The earlier analysis incorrectly assumed awaitable mutex types implement ICriticalSection. They don't — `ICriticalSection` and `IAwaitableCriticalSection` are parallel branches under `ILockInfo`.)

### ~~Gap 2: IAwaitableCriticalSection — RESOLVED~~

All IAwaitableCriticalSection implementations are now in the matrix: Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex.

### Gap 3: ~~IShared\<T\> — only tested as IThreadShared, not IProcessShared~~ RESOLVED

`IShared_specification` uses `[ISharedMatrix]` to test the full `IShared<T>` contract against `IThreadShared.New()`, `IProcessShared.Global()`, and `IProcessShared.Local()`. Covers Locked (Func and Action), mutual exclusion, and CriticalSection property.

`IThreadShared_specification` tests the `IThreadShared`-specific concern: the `Monitor` property.

### Gap 4: ~~IAwaitableShared\<T\> — no specification at all~~ RESOLVED

`IAwaitableShared_specification` uses `[IAwaitableSharedMatrix]` to test the full `IAwaitableShared<T>` contract against all 5 variants (Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex). Covers:
- `Read` (Func and Action overloads)
- `Update` (Func and Action overloads)
- Full mutual exclusion — all access methods are blocked while any one holds the lock, all succeed after release
- `ReadWhen` — immediate, condition-wait, timeout
- `UpdateWhen` (Func and Action) — immediate, condition-wait, timeout
- `TryUpdateWhen` — true on immediate/eventual condition, false on timeout
- `Await` — immediate, condition-wait, timeout
- `CriticalSection` property — ContentionCount, independent instances

### Gap 5: IInterprocessObject\<T\> — tested for persistence, not as IAwaitableProcessShared

`MachineWideSharedObjectTests` tests interprocess-object-specific behavior (Create, persistence, data sharing, backing store variants). But it never tests:

- That IInterprocessObject fulfills the IAwaitableShared contract (Read/Update/ReadWhen/UpdateWhen with proper lock semantics)
- That it fulfills the IAwaitableProcessShared contract (Mutex property, CriticalSection property)

### ~~Gap 6: IAwaitableProcessShared\\<T\\> — no specification at all~~ RESOLVED

`IAwaitableProcessShared_specification` uses `[IAwaitableProcessSharedMatrix]` to test all 4 variants (GlobalPolling, LocalPolling, GlobalSignaling, LocalSignaling). Covers Mutex property (IsGlobal, Name, LockTimeout, WaitTimeout) and IDisposable. The `IAwaitableShared<T>` contract is covered by `IAwaitableShared_specification`.

`IAwaitableThreadShared_specification` tests the `IAwaitableThreadShared`-specific concern: the `Monitor` property.

### Gap 7: ~~IProcessShared — Global/Local not matrix-tested~~ RESOLVED

`IProcessShared_specification` uses `[IProcessSharedMatrix]` to test Global and Local variants. The factory calls `IProcessShared.Global()` / `IProcessShared.Local()` — the real user-facing factory methods.

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
- `[IProcessSharedMatrix]` — GlobalMutex, LocalMutex
- `[IAwaitableSharedMatrix]` — Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex
- `[IAwaitableProcessSharedMatrix]` — GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex

All factories use the real user-facing factory methods (`IThreadShared.New()`, `IProcessShared.Global/Local()`, `IAwaitableThreadShared.New()`, `IAwaitableProcessShared.GlobalPolling/LocalPolling/GlobalSignaling/LocalSignaling()`).

## Plan: New Specification Files

| Spec file | Tests interface | Matrix attribute | Status |
|---|---|---|---|
| `IShared_specification` | `IShared<T>` | `[ISharedMatrix]` | ✅ Done — Locked, CriticalSection property, mutual exclusion |
| `IThreadShared_specification` | `IThreadShared<T>` | `[XF]` | ✅ Done — Monitor property |
| `IProcessShared_specification` | `IProcessShared<T>` | `[IProcessSharedMatrix]` | ✅ Exists — Mutex property, IDisposable. Full IShared contract covered by IShared_specification |
| `IAwaitableShared_specification` | `IAwaitableShared<T>` | `[IAwaitableSharedMatrix]` | ✅ Done — Read, Update, mutual exclusion, ReadWhen, UpdateWhen, TryUpdateWhen, Await, CriticalSection property |
| `IAwaitableProcessShared_specification` | `IAwaitableProcessShared<T>` | `[IAwaitableProcessSharedMatrix]` | ✅ Done — Mutex property (IsGlobal, Name, LockTimeout, WaitTimeout), IDisposable. IAwaitableShared contract covered by IAwaitableShared_specification |

---

## Summary: What we have vs what we need

### Matrix attributes — current

| Attribute | Enum | Values | Dimensions | Tests against interface |
|---|---|---|---|---|
| `[ICriticalSectionMatrix]` | `ICriticalSectionMatrixAttribute.Implementation` | Monitor, GlobalMutex, LocalMutex | 3 | ICriticalSection |
| `[IAwaitableCriticalSectionMatrix]` | `IAwaitableCriticalSectionMatrixAttribute.Implementation` | Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | 5 | IAwaitableCriticalSection |
| `[InterprocessObjectMatrix]` | `InterprocessObjectBackingStore` | File, MemoryMapped | 2 | IInterprocessObject |
| `[ISharedMatrix]` | `ISharedMatrixAttribute.Implementation` | Monitor, GlobalMutex, LocalMutex | 3 | IShared\<T\> |
| `[IProcessSharedMatrix]` | `IProcessSharedMatrixAttribute.Implementation` | GlobalMutex, LocalMutex | 2 | IProcessShared\<T\> |
| `[IAwaitableSharedMatrix]` | `IAwaitableSharedMatrixAttribute.Implementation` | Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | 5 | IAwaitableShared\<T\> |
| `[IAwaitableProcessSharedMatrix]` | `IAwaitableProcessSharedMatrixAttribute.Implementation` | GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | 4 | IAwaitableProcessShared\<T\> |

### Matrix attributes — planned

All planned matrix attributes have been implemented. None remaining.

### Interface coverage — current and planned

| Interface | Has specification? | Status |
|---|---|---|
| `ICriticalSection` | ✅ `ICriticalSection_specification` | **Complete** |
| `IAwaitableCriticalSection` | ✅ `IAwaitableCriticalSection_specification` | **Complete** |
| `IShared<T>` | ✅ `IShared_specification` | **Complete** — `[ISharedMatrix]` covers Monitor, GlobalMutex, LocalMutex |
| `IThreadShared<T>` | ✅ `IThreadShared_specification` | **Complete** — tests Monitor property (IShared contract covered by IShared_specification) |
| `IProcessShared<T>` | ✅ `IProcessShared_specification` | **Partial** — `[IProcessSharedMatrix]` covers Mutex property + IDisposable. IShared contract covered by IShared_specification |
| `IAwaitableShared<T>` | ✅ `IAwaitableShared_specification` | **Complete** — `[IAwaitableSharedMatrix]` covers Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex |
| `IAwaitableThreadShared<T>` | ✅ `IAwaitableThreadShared_specification` | **Complete** — tests Monitor property (IAwaitableShared contract covered by IAwaitableShared_specification) |
| `IAwaitableProcessShared<T>` | ✅ `IAwaitableProcessShared_specification` | **Complete** — `[IAwaitableProcessSharedMatrix]` covers Mutex property (IsGlobal, Name, LockTimeout, WaitTimeout) + IDisposable. IAwaitableShared contract covered by IAwaitableShared_specification |
| `IInterprocessObject<T>` | Partial (persistence only) | Separate concern — persistence/backing store tested via `[InterprocessObjectMatrix]` |

### ~~Missing — scope dimension~~ RESOLVED

Both `ICriticalSectionMatrix` and `IAwaitableCriticalSectionMatrix` now cover Global/Local for all mutex types.
