# ThreadingCE ↔ SystemCE Circular Dependency Analysis

## The Circular Dependency

```
Compze.Utilities.SystemCE ──ProjectReference──▶ Compze.Utilities.SystemCE.ThreadingCE
                           ◀──InternalizedSource──
```

- **SystemCE → ThreadingCE**: Normal `ProjectReference`. SystemCE uses `IMonitorCE`, `IThreadShared`, and other types exported by ThreadingCE.
- **ThreadingCE → SystemCE**: Cannot be a `ProjectReference` (would create a cycle). Instead, ThreadingCE internalizes **all** source from SystemCE via `Compze.Build.InternalizedSourceReferences`. This copies every `.cs` file, rewrites `public` to `internal`, and compiles them as ThreadingCE's private copy.

## What ThreadingCE Actually Uses from SystemCE

ThreadingCE internalizes **all 50+ source files** from SystemCE but only uses a subset. Of 25 ThreadingCE source files, **13 use zero SystemCE types**. The actual dependencies cluster into 6 categories:

### 1. `ActionFuncHarmonization` — `AsFunc()` extension method

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `ActionFuncHarmonization/ActionToUnitFuncConverter.cs` | `Action.AsFunc()` → `Func<unit>` | `MachineWideSingleThreaded.cs`, `IMonitorCE.DefaultImplementations.Functional.cs`, `IThreadShared.cs` |

**Complexity**: Trivial (20 LOC, depends only on `Compze.Utilities.Functional.unit`).

### 2. `TimeSpanCE` — fluent time factories and `None()`

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `TimeSpanCE.FluentFactory.cs` | `int.Seconds()`, `int.Minutes()`, `int.Milliseconds()` | `IMonitorCE.cs`, `IMonitorCE.MonitorCE.cs` |
| `TimeSpanCE.cs` | `TimeSpan.None()` | `IMonitorCE.MonitorCE.cs` |

**Complexity**: `FluentFactory` is ~90 LOC of pure extension methods with zero Compze dependencies. `TimeSpanCE.cs` is ~40 LOC (also pure extensions, no Compze deps). Three methods from FluentFactory and one from TimeSpanCE are actually used.

### 3. `DateTimeCE` + `CompzeEnvironment`

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `DateTimeCE.cs` | `DateTimeCE.TimeElapsedSince()` | `IMonitorCE.MonitorCE.cs` |
| `CompzeEnvironment.cs` | `CompzeEnvironment.IsNCrunch` (const bool) | `IMonitorCE.cs` |

**Complexity**: `TimeElapsedSince` is a one-liner (`DateTime.UtcNow - pointInThePast`). `CompzeEnvironment` is a 12-LOC class with a single compile-time constant. Neither has any Compze dependencies.

### 4. `Disposable` class

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `Disposable..cs` | `new Disposable(action)` constructor | `IMonitorCE.MonitorCE.cs` |

**Complexity**: Simple wrapper (~25 LOC). Depends on `Compze.Utilities.Contracts.Assert.Argument.NotNull()` (already a ThreadingCE dependency) and `ActionCE.NullOp` (only for the static `Disposable.NullOp` field, which ThreadingCE doesn't use).

### 5. `LinqCE` — `int.Through()` sequence generator

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `LinqCE/EnumerableCE.IntSequenceGeneration.cs` | `int.Through(int)` | `ThreadPoolCE.cs` |

**Complexity**: The `Through` method itself is simple, but the file is ~85 LOC and defines `IterationSpecification` with additional overloads. Only the basic `int.Through(int)` overload is used.

### 6. `ThreadingCE/TasksCE/` — Task utilities (lives inside SystemCE project)

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `ThreadingCE/TasksCE/TaskCE.EnsureAsyncOperation.cs` | `TaskCE.Run(Action)`, `TaskCE.Run<T>(Func<T>)` | `ThreadPoolCE.cs`, `TestingTaskRunner.cs`, `ThreadGateExtensions.cs` |
| `ThreadingCE/TasksCE/TaskCE.SyncShim.cs` | `ValueTask.WaitUnwrappingException()` | `TestingTaskRunner.cs` |
| `ThreadingCE/TasksCE/TaskCE.SynchronizationContext.cs` | `Task.caf()` (`ConfigureAwait(false)`) | `TestingTaskRunner.cs` |

**Complexity**: `TaskCE.Run` is ~60 LOC, uses `ActionToUnitFuncConverter.AsFunc()` (category 1 above). `SyncShim` is ~60 LOC, no Compze deps. `ConfigureAwaitCE` is ~50 LOC, no Compze deps. These files currently live in SystemCE's `ThreadingCE/` subfolder but are in the `Compze.Utilities.SystemCE.ThreadingCE.TasksCE` namespace — they are arguably mis-homed.

### 7. `IOCE/` — file system utilities (isolated to one consumer)

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `IOCE/DirectoryCE.cs` | `DirectoryCE` class | `MachineWideSharedObject.cs` |
| `IOCE/DirectoryCE.StandardDirectories.cs` | `DirectoryCE.StandardDirectories.LocalApplicationData` | `MachineWideSharedObject.cs` |
| `IOCE/TextFile.cs` | `TextFile` class | `MachineWideSharedObject.cs` |
| `IOCE/PathCE.cs` | `PathCE.ReplaceInvalidCharactersWith()` | `MachineWideSharedObject.cs` |

**Complexity**: These 4 files form a small file system abstraction. `DirectoryCE` inherits `FileSystemInfoCE` which uses `StringCE`. `TextFile` inherits `FileCE`. This cluster pulls in 6+ transitive files. **All usage is isolated to the single file `MachineWideSharedObject.cs`.**

### 8. `TransactionsCE/Testing/` — transaction test intercept (isolated to one consumer)

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `TransactionsCE/Testing/TransactionInterceptorExtensions.cs` | `Transaction.FailOnPrepare()` | `ThreadGateExtensions.cs` |

**Complexity**: `FailOnPrepare` is a 7-LOC extension method, but it calls `Transaction.AddPrepareTasks()` (in `VolatileLambdaTransactionParticipantExtensions.cs`), which uses `IThreadShared`, `VolatileLambdaTransactionParticipant`, `VolatileTransactionParticipant`, and `DictionaryCE.GetOrAdd()` — a significant transitive dependency chain. **Used by exactly one method in `ThreadGateExtensions.cs`.**

### 9. `LazyCE<T>` (isolated to one consumer)

| SystemCE File | Type / API | Used By (ThreadingCE) |
|---|---|---|
| `LazyCE.cs` | `LazyCE<DirectoryCE>` | `MachineWideSharedObject.cs` |

**Complexity**: 20 LOC. **Crucially, `LazyCE<T>` depends on `IMonitorCE` from ThreadingCE itself** — so the internalized copy uses the internalized copy of `IMonitorCE` rather than the "real" one. Only used in `MachineWideSharedObject.cs`.

---

## Summary: Dependency Map

```
ThreadingCE source file                    SystemCE types used
─────────────────────────────────────────  ──────────────────────────────────────────
IMonitorCE.cs                              CompzeEnvironment.IsNCrunch, .Seconds(), .Minutes()
IMonitorCE.MonitorCE.cs                    Disposable, .Seconds(), .Milliseconds(), DateTimeCE.TimeElapsedSince(), .None()
IMonitorCE.DefaultImplementations.Functional.cs   .AsFunc()
IThreadShared.cs                           .AsFunc()
MachineWideSingleThreaded.cs               .AsFunc()
ThreadPoolCE.cs                            .Through(), TaskCE.Run()
TestingTaskRunner.cs                       TaskCE.Run(), .WaitUnwrappingException(), .caf()
ThreadGateExtensions.cs                    TaskCE.Run(), Transaction.FailOnPrepare()
MachineWideSharedObject.cs                 DirectoryCE, TextFile, PathCE, LazyCE<T>
```

**Files with NO SystemCE dependency** (13 of 25): `ThreadCE.cs`, `ThreadSafe.cs`, `Throw.cs`, `ReadonlyCollectionsTE.cs`, `AwaitingConditionTimeoutException.cs`, `IMonitorCE.DefaultImplementations.Awaiting.cs`, `ISharedObjectSerializer.cs`, `MonitorCEExtensions.cs`, `TakeLockTimeoutException.cs`, `Testing/Disposable.cs`, `Testing/GatedCodeSection.cs`, `Testing/ThreadGate.cs`, `Testing/ThreadSnapshot.cs`, `Testing/TransactionSnapshot.cs`, `Testing/_ThreadingApi.cs`

---

## Strategies to Break the Dependency

### Strategy A: Move small utilities into ThreadingCE (or a shared source project)

Copy the ~6 tiny types that core ThreadingCE needs into the ThreadingCE project directly, either as owned source or via `Compze.Shared.Source`:

| Type | LOC | Transitive deps |
|---|---|---|
| `ActionToUnitFuncConverter.AsFunc()` | 20 | `Functional.unit` (already a dependency) |
| `TimeSpanCE.FluentFactory` (3 methods) | ~15 | None |
| `TimeSpanCE.None()` | ~5 | None |
| `DateTimeCE.TimeElapsedSince()` | 1 | None |
| `CompzeEnvironment.IsNCrunch` | 12 | None |
| `Disposable` | 25 | `Contracts.Assert` (already a dependency) |

**Total**: ~80 LOC of trivially self-contained code.

**Difficulty: Low**. These are small, stable utility types with minimal or zero transitive dependencies. They could be added to `Compze.Shared.Source` with `#if COMPZE_PUBLIC_API` guards so SystemCE exports them publicly while ThreadingCE gets internal copies — exactly the pattern already established for shared source.

### Strategy B: Move `TasksCE` sources into the ThreadingCE project

The `TaskCE`, `ConfigureAwaitCE`, and related files currently live in `src/Compze.Utilities.SystemCE/ThreadingCE/TasksCE/` but use the namespace `Compze.Utilities.SystemCE.ThreadingCE.TasksCE` — they are conceptually threading code. Moving them into the ThreadingCE project would eliminate the internalized-source dependency for `TaskCE.Run()`, `.caf()`, and `.WaitUnwrappingException()`.

**Difficulty: Low-Medium**. The files are self-contained. `TaskCE.Run()` depends on `AsFunc()` (Strategy A). SystemCE itself uses `TaskCE` in several files (`AsyncLockCE`, `RunOnce`, `TransactionScopeCE`, `ActionToUnitFuncConverterAsync`), so SystemCE would need to reference these through its normal `ProjectReference` to ThreadingCE (which it already has).

### Strategy C: Move `MachineWideSharedObject` out of ThreadingCE

`MachineWideSharedObject.cs` is the sole consumer of:
- All `IOCE/` types (`DirectoryCE`, `TextFile`, `PathCE` + transitive deps)
- `LazyCE<T>`

It lives in `Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess` but its actual namespace is conceptually about cross-process shared state (using `Mutex` + file-based persistence), not pure in-process threading. Moving it to SystemCE or a new project would eliminate ~6 transitive SystemCE types from ThreadingCE's dependency surface.

**Difficulty: Low**. One file, already somewhat out of place conceptually.

### Strategy D: Move `ThreadGateExtensions.FailTransactionOnPreparePostPassThrough` 

The `FailOnPrepare` dependency chain (`TransactionsCE → VolatileLambdaTransactionParticipant → IThreadShared → ...`) is heavy and used by a single testing utility method. This could be:
- Moved to a testing-only project  
- Inlined at the call site  
- Extracted to an extension method in a higher-level project that already references both SystemCE and ThreadingCE

**Difficulty: Low**. Single method, testing-only code.

---

## Recommended Approach

Combining strategies A + B + C + D eliminates **all** SystemCE dependencies from ThreadingCE:

| Step | What | Eliminates | Effort |
|---|---|---|---|
| 1 | Add `AsFunc()`, `TimeSpanCE` (4 methods), `DateTimeCE.TimeElapsedSince()`, `CompzeEnvironment`, `Disposable` to `Compze.Shared.Source` | Categories 1–4 (~80 LOC) | **Small** |
| 2 | Move `TasksCE/` files from SystemCE → ThreadingCE project | Category 6 (~170 LOC) | **Small** |
| 3 | Move `MachineWideSharedObject.cs` to SystemCE (or a new project) | Categories 7 + 9 (IOCE + LazyCE) | **Small** |
| 4 | Move `FailTransactionOnPreparePostPassThrough` out of ThreadingCE | Category 8 (TransactionsCE) | **Trivial** |
| 5 | Move `int.Through()` to shared source or replace with `Enumerable.Range` | Category 5 | **Trivial** |
| 6 | Remove `InternalizeSourceFrom` from ThreadingCE `.csproj` | — | **Trivial** |

**Overall difficulty: Low to Medium**. The dependency surface is small (18 unique types/APIs) and most are trivial utility methods. The hardest part is deciding the right home for each type — the actual code changes are mechanical. No public API changes needed since internalized types are already internal.

### Risk Assessment

- **No behavioral changes**: All code stays the same, just moves between projects.
- **Namespace preservation**: Shared source with `#if COMPZE_PUBLIC_API` maintains the same namespaces in both projects.
- **`LazyCE<T>` is tricky**: It uses `IMonitorCE` from ThreadingCE. If it stays in SystemCE (which it should), SystemCE already references ThreadingCE — no problem. The internalized copy in ThreadingCE was using an internalized `IMonitorCE` which is subtly different from the "real" one — eliminating this is actually a correctness improvement.
- **Build order**: After the change, ThreadingCE compiles independently. SystemCE depends on ThreadingCE. This is a clean one-way dependency.
