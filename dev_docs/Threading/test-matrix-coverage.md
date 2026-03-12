# The goal
We want utilize matrix test attributes to run all tests for our interfaces against all implementations of those interfaces and all significant configuration variations of those interaces to achive full test coverage without duplicating the same tests over and over for different implementations and configurations.

This document details the current state and what we need to change to reach that goal.



## Interface Hierarchy

### Lock / Critical Section

```
ICriticalSection
├── IMonitor                          (in-process)
├── IMutex                            (cross-process, Global/Local)
│
IAwaitableCriticalSection : ICriticalSection
├── IAwaitableMonitor : IMonitor      (in-process)
├── IAwaitableMutex : IMutex          (cross-process, Global/Local)
│   ├── IPollingAwaitableMutex        (condition-wait via polling)
│   └── ISignalingAwaitableMutex      (condition-wait via OS events)
```

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
| MonitorCE | IMonitor, IAwaitableMonitor | In-process | `IMonitor.New()` |
| MutexCE | IMutex | Global/Local | `IMutex.Global()` / `IMutex.Local()` |
| PollingAwaitableMutexCE | IPollingAwaitableMutex | Global/Local | `IPollingAwaitableMutex.Global()` / `.Local()` |
| SignalingAwaitableMutexCE | ISignalingAwaitableMutex | Global/Local | `ISignalingAwaitableMutex.Global()` / `.Local()` |

Total unique ICriticalSection implementations: **4 classes × scope = 7 variants** (MonitorCE + MutexCE×2 + PollingMutex×2 + SignalingMutex×2)

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

### [ICriticalSectionMatrix] — `CriticalSectionImplementation` enum

| Value | Creates | Scope |
|---|---|---|
| `Monitor` | `IMonitor.New()` | In-process |
| `Mutex` | `IMutex.Global()` | **Global only** |

**Used by:**
- `ICriticalSection_specification` — tests `ILock` (Locked, TakeLock, reentrancy, timeouts, contention)
- `IThreadShared_specification` — tests `IShared<T>` (Locked wrapper, CriticalSection property)

**Missing from matrix:**
- `IMutex.Local()` — Local Mutex never tested via matrix
- No PollingAwaitableMutex or SignalingAwaitableMutex as ICriticalSection (they implement ICriticalSection too)

### [IAwaitableCriticalSectionMatrix] — `AwaitableCriticalSectionImplementation` enum

| Value | Creates | Scope |
|---|---|---|
| `Monitor` | `IAwaitableMonitor.New()` | In-process |
| `Mutex` | `IPollingAwaitableMutex.Global()` | **Global only** |
| `SignalingMutex` | `ISignalingAwaitableMutex.Global()` | **Global only** |

**Used by:**
- `IAwaitableCriticalSection_specification` — tests TakeUpdateLock, TakeReadLock, TakeUpdateLockWhen, cursor-based waits
- `ContentionCount_specification` — tests contention tracking

**Missing from matrix:**
- `IPollingAwaitableMutex.Local()` — never tested
- `ISignalingAwaitableMutex.Local()` — never tested

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

### Gap 1: ICriticalSection — missing implementations in matrix

The `CriticalSectionImplementation` enum has **2 values**. There should be **7** (or at least 4 if we ignore scope):

| Should be in `CriticalSectionImplementation` | Currently tested | Status |
|---|---|---|
| Monitor | Yes | ✅ |
| Global Mutex | Yes | ✅ |
| Local Mutex | No | ❌ |
| Global PollingAwaitableMutex (as ICriticalSection) | No | ❌ |
| Local PollingAwaitableMutex (as ICriticalSection) | No | ❌ |
| Global SignalingAwaitableMutex (as ICriticalSection) | No | ❌ |
| Local SignalingAwaitableMutex (as ICriticalSection) | No | ❌ |

### Gap 2: IAwaitableCriticalSection — missing Local scope variants

| Should be in `AwaitableCriticalSectionImplementation` | Currently tested | Status |
|---|---|---|
| Monitor | Yes | ✅ |
| Global PollingAwaitableMutex | Yes | ✅ |
| Global SignalingAwaitableMutex | Yes | ✅ |
| Local PollingAwaitableMutex | No | ❌ |
| Local SignalingAwaitableMutex | No | ❌ |

### Gap 3: IShared\<T\> — only tested as IThreadShared, not IProcessShared

`IThreadShared_specification` uses `[ICriticalSectionMatrix]` to create an `IShared.New(value, lock)` — a bare `IShared<T>`.

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

## Summary: What we have vs what we need

### Matrix attributes — current

| Attribute | Enum | Values | Dimensions | Tests against interface |
|---|---|---|---|---|
| `[ICriticalSectionMatrix]` | `CriticalSectionImplementation` | Monitor, Mutex | 2 | ICriticalSection, IShared |
| `[IAwaitableCriticalSectionMatrix]` | `AwaitableCriticalSectionImplementation` | Monitor, Mutex, SignalingMutex | 3 | IAwaitableCriticalSection |
| `[InterprocessObjectMatrix]` | `InterprocessObjectBackingStore` | File, MemoryMapped | 2 | IInterprocessObject |

### Missing — interfaces with no specification testing all implementations

| Interface | Has specification? | Implementations never tested against it |
|---|---|---|
| `ICriticalSection` | ✅ (`ICriticalSection_specification`) | Local Mutex, all AwaitableMutex variants (4) |
| `IAwaitableCriticalSection` | ✅ (`IAwaitableCriticalSection_specification`) | Local PollingMutex, Local SignalingMutex |
| `IShared<T>` | Partial (`IThreadShared_specification`) | ProcessShared (Global/Local) |
| `IAwaitableShared<T>` | ❌ **No specification** | All 8 implementations |
| `IProcessShared<T>` | Partial (XF, not matrix) | Global/Local not matrix-varied |
| `IAwaitableProcessShared<T>` | ❌ **No specification** | All 6 implementations |
| `IInterprocessObject<T>` | Partial (persistence only) | Never tested as IAwaitableShared/IAwaitableProcessShared |

### Missing — scope dimension

No matrix attribute varies the **Global vs Local** dimension. Every mutex-based test currently uses only `Global`. The `Local` variants of every mutex type are untested through the matrix.
