# The goal
We want utilize matrix test attributes to run all tests for our interfaces against all implementations of those interfaces and all significant configuration variations of those interaces to achive full test coverage without duplicating the same tests over and over for different implementations and configurations.

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
- `IThreadShared_specification` — tests `IShared<T>` (Locked wrapper, CriticalSection property)

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

### Gap 3: IShared\<T\> — only tested as IThreadShared, not IProcessShared

`IShared_specification` uses `[ISharedMatrix]` to test the full `IShared<T>` contract against `IThreadShared.New()`, `IProcessShared.Global()`, and `IProcessShared.Local()`.

**What's never tested:**
- `IProcessShared<T>` against the `IShared<T>` specification (it IS-A `IShared<T>` but its `Locked()` is never matrix-tested — only property-level [XF] tests exist)
- The Global vs Local dimension is not a matrix: there are duplicate test methods written by hand

### Gap 4: IAwaitableShared\<T\> — no specification at all

There is **no `IAwaitableShared_specification`**. The behaviors inherited from `IAwaitableShared<T>` — `Read`, `Update`, `ReadWhen`, `UpdateWhen`, `TryUpdateWhen`, `Await` — are not tested against varying implementations.

**What's never tested as IAwaitableShared\<T\>:**
- `IAwaitableThreadShared<T>` (not tested at all except ContentionCount)
- `IAwaitableProcessShared<T>` (all 4 factory variants: GlobalPolling, LocalPolling, GlobalSignaling, LocalSignaling)
- `IInterprocessObject<T>` (tested for its own concerns in DbPool.Tests, but never as a generic IAwaitableShared)

### Gap 5: IInterprocessObject\<T\> — tested for persistence, not as IAwaitableProcessShared

`MachineWideSharedObjectTests` tests interprocess-object-specific behavior (Create, persistence, data sharing, backing store variants). But it never tests:

- That IInterprocessObject fulfills the IAwaitableShared contract (Read/Update/ReadWhen/UpdateWhen with proper lock semantics)
- That it fulfills the IAwaitableProcessShared contract (Mutex property, CriticalSection property)

### Gap 6: IAwaitableProcessShared\<T\> — no specification at all

No test file exists that tests the `IAwaitableProcessShared<T>` interface contract against its various instantiations. The 4 factory variants (GlobalPolling, LocalPolling, GlobalSignaling, LocalSignaling) are never tested as an `IAwaitableProcessShared<T>`.

### Gap 7: IProcessShared — Global/Local not matrix-tested

`IProcessShared_specification` has duplicate `Global` and `Local` inner classes with manually duplicated tests. This should be a matrix dimension, not copy-pasted test methods.

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

| Attribute | Enum values | Factory creates via |
|---|---|---|
| `[ISharedMatrix]` | Monitor, GlobalMutex, LocalMutex | `IThreadShared.New()` / `IProcessShared.Global/Local()` |
| `[IProcessSharedMatrix]` | GlobalMutex, LocalMutex | `IProcessShared.Global/Local()` |
| `[IAwaitableSharedMatrix]` | Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | `IAwaitableThreadShared.New()` / `IAwaitableProcessShared.New()` |
| `[IAwaitableProcessSharedMatrix]` | GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | `IAwaitableProcessShared.GlobalPolling/LocalPolling/GlobalSignaling/LocalSignaling(...)` |

Each attribute follows the established pattern: partial class with nested `Implementation` enum and `Factory<TTest>`.

## Plan: New Specification Files

| Spec file | Tests interface | Matrix attribute | Full contract? |
|---|---|---|---|
| `IShared_specification` | `IShared<T>` | `[ISharedMatrix]` | Yes — Locked, CriticalSection property, contention |
| `IProcessShared_specification` | `IProcessShared<T>` | `[IProcessSharedMatrix]` | Yes — full IShared contract + Mutex property |
| `IAwaitableShared_specification` | `IAwaitableShared<T>` | `[IAwaitableSharedMatrix]` | Yes — Read, Update, ReadWhen, UpdateWhen, TryUpdateWhen, Await, CriticalSection property |
| `IAwaitableProcessShared_specification` | `IAwaitableProcessShared<T>` | `[IAwaitableProcessSharedMatrix]` | Yes — full IAwaitableShared contract + Mutex property |

Existing `IThreadShared_specification` to be refactored: rename to `IShared_specification`, use `[ISharedMatrix]`.

---

## Summary: What we have vs what we need

### Matrix attributes — current

| Attribute | Enum | Values | Dimensions | Tests against interface |
|---|---|---|---|---|
| `[ICriticalSectionMatrix]` | `ICriticalSectionMatrixAttribute.Implementation` | Monitor, GlobalMutex, LocalMutex | 3 | ICriticalSection |
| `[IAwaitableCriticalSectionMatrix]` | `IAwaitableCriticalSectionMatrixAttribute.Implementation` | Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | 5 | IAwaitableCriticalSection |
| `[InterprocessObjectMatrix]` | `InterprocessObjectBackingStore` | File, MemoryMapped | 2 | IInterprocessObject |

### Matrix attributes — planned

| Attribute | Enum values | Tests against interface |
|---|---|---|
| `[ISharedMatrix]` | Monitor, GlobalMutex, LocalMutex | IShared\<T\> |
| `[IProcessSharedMatrix]` | GlobalMutex, LocalMutex | IProcessShared\<T\> |
| `[IAwaitableSharedMatrix]` | Monitor, GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | IAwaitableShared\<T\> |
| `[IAwaitableProcessSharedMatrix]` | GlobalPollingMutex, LocalPollingMutex, GlobalSignalingMutex, LocalSignalingMutex | IAwaitableProcessShared\<T\> |

### Interface coverage — current and planned

| Interface | Has specification? | Status |
|---|---|---|
| `ICriticalSection` | ✅ `ICriticalSection_specification` | **Complete** |
| `IAwaitableCriticalSection` | ✅ `IAwaitableCriticalSection_specification` | **Complete** |
| `IShared<T>` | Partial (`IThreadShared_specification`) | **Planned**: `IShared_specification` with `[ISharedMatrix]` |
| `IProcessShared<T>` | Partial (XF, not matrix) | **Planned**: `IProcessShared_specification` with `[IProcessSharedMatrix]` |
| `IAwaitableShared<T>` | ❌ None | **Planned**: `IAwaitableShared_specification` with `[IAwaitableSharedMatrix]` |
| `IAwaitableProcessShared<T>` | ❌ None | **Planned**: `IAwaitableProcessShared_specification` with `[IAwaitableProcessSharedMatrix]` |
| `IInterprocessObject<T>` | Partial (persistence only) | Separate concern — persistence/backing store tested via `[InterprocessObjectMatrix]` |

### ~~Missing — scope dimension~~ RESOLVED

Both `ICriticalSectionMatrix` and `IAwaitableCriticalSectionMatrix` now cover Global/Local for all mutex types.
