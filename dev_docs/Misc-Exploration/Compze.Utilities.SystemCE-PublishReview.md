# Compze.Internals.SystemCE — Public vs Internal Review

Review of all types in `Compze.Internals.SystemCE` to determine whether each is a good fit for a published NuGet package or should be moved to `Compze.Internals.SystemCE.Core`.

**Guiding criteria:**
- **Publish**: Genuinely useful general-purpose utility that users of the framework would benefit from. Stable API surface. Clear, unsurprising semantics.
- **Internal**: Framework implementation detail, quirky/opinionated API, unstable design, only useful inside Compze, or couples to internal infrastructure.

---

## Root Types

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `ActionCE` | internal | **Internal** ✓ | Already internal. Trivial `InvokeAll` helper + `NullOp` field. Pure implementation convenience. |
| `AsyncDisposable` | internal | **Internal** ✓ | Already internal. Mirror of `Disposable` for async. |
| `CastCE` | public | **Internal** | Single method `CastTo<T>`. Very opinionated fluent cast — not something external users need from a framework package. |
| `CompzeEnvironment` | public | **Internal** | Only checks `GITHUB_ACTIONS` env var. Pure internal CI concern. |
| `DateTimeCE` | public | **Publish** | `ToUniversalTimeSafely`, `ParseInvariant`, `TruncateToMicroseconds` — all genuinely useful, well-documented, and general-purpose. |
| `Disposable` | public | **Publish** | Action-based `IDisposable` with thread-safe one-shot semantics. Classic utility pattern. Well implemented. |
| `DisposableCECollections` | public | **Internal** | Single trivial `DisposeAll` extension. Not substantial enough for public API. |
| `EnumCE` | public | **Publish** | `IsValid<TEnum>()` with cached values. Useful, clean, general-purpose. |
| `GCCE` | internal | **Internal** ✓ | Already internal. Forces full GC — testing/diagnostic only. |
| `IntCE` | public | **Internal** | `ParseInvariant` / `ToStringInvariant` — two trivial one-liners wrapping `CultureInfo.InvariantCulture`. Barely substantial enough for a public type. |
| `IStaticInstancePropertySingleton<T>` | public | **Publish** | Well-defined interface with `static abstract`. Used by the serializer optimization path. Clear contract. |
| `LazyCE<T>` | public | **Internal** | Custom `Lazy<T>` replacement with `Reset()` and `ValueIfInitialized()`. Uses internal `IMonitor`. Opinionated alternative to BCL `Lazy<T>` — not obviously better for external users and couples to internal threading primitives. |
| `ObjectCE` | public | **Internal** | `ToStringCE`, `Repeat`, `ToStringNotNull` — grab-bag of tiny extensions on `object`. Not cohesive or substantial. |
| `ReentrancyGuard` | public | **Internal** | Niche utility for re-entrancy detection. Mutable state, `GetAndClearReentryWasAttempted` is a side-effectful getter. Internal implementation detail. |
| `ScopedChange` | public | **Publish** | Clean, well-documented pattern for temporary state changes with rollback. General-purpose. |
| `StringCE` (main) | public | **Publish** | `Join`, `IsNullEmptyOrWhiteSpace`, `Pluralize`, `RemoveLinesWhere` — useful string utilities. |
| `StringCE` (Ordinal) | public | **Publish** | `ContainsCE`, `StartsWithCE`, `ReplaceCE`, `FormatInvariant`, `Invariant` — ordinal-safe string ops filling a real gap (avoiding culture-dependent defaults). |
| `StringIndenter` | public | **Internal** | `Indent`, `IndentToDepth`, `JoinLines` — small formatting helpers. `JoinLines` is decent but the rest is niche. Borderline, but the API isn't refined enough for public consumption (magic default of 3 spaces). |
| `TimeSpanCE` (core) | public | **Publish** | `MultiplyBy`, `DivideBy`, `FormatReadable`, `None` — solid general-purpose TimeSpan utilities. |
| `TimeSpanCE.FluentFactory` | public | **Publish** | `1.Seconds()`, `500.Milliseconds()`, etc. — widely popular fluent TimeSpan pattern. Clearly useful for any user. |
| `TimeSpanCE.EnumerableCE` | public | **Publish** | `Min`, `Max`, `Sum`, `Average` for `IEnumerable<TimeSpan>` — handy aggregation. |
| `UncatchableExceptionsGatherer` | public | **Internal** | Global mutable state for gathering exceptions from finalizers. Includes `TestingMonitor`. Pure testing/diagnostic infrastructure. |

## CollectionsCE/GenericCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `CollectionCE` | public | **Internal** | Single method `RemoveWhere`. Trivial, not substantial enough for public API. |
| `DictionaryCE` | public | **Publish** | `GetOrAdd`, `GetOrAddDefault` — well-known missing methods from BCL dictionaries. Performance-conscious implementation. |
| `EnumerableCE` (main) | public | **Publish** | `Create`, `None`, `ChopIntoSizesOf`, `Flatten`, `ToReadOnlyList` — all solid general-purpose LINQ extensions. |
| `EnumerableCE.ForEach` | public | **Publish** | Standard `ForEach` with multiple overloads including indexed variant. Very commonly desired. |
| `EnumerableCE.IntSequenceGeneration` | public | **Publish** | `Through`, `Until`, `By` — elegant fluent integer sequence DSL. Well designed and useful. |
| `EnumerableCE.OfTypes` (in LinqCE/) | public | **Internal** | Multiple overloads returning `IEnumerable<Type>` from type parameters. Niche framework plumbing. |
| `EnumerableCE` (in CollectionsCE/) | public | **Internal** | Duplicate OfTypes overloads already in LinqCE. One set should be removed; either way, this is niche infrastructure. |
| `LinkedListCE` | public | **Internal** | `ValuesFrom`, `AddBefore`, `Replace` — `LinkedList<T>` is rarely used by external consumers. Very niche. |

## ComponentModelCE/DataAnnotationsCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `ValidatableObjectCE` | public | **Publish** | Type-safe `ValidationResult` creation from expressions. Fills a real gap in `IValidatableObject` usage. |

## IOCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `FileSystemInfoCE` | public | **Internal** | Base class requiring absolute paths. Used only as infrastructure for `DirectoryCE`/`FileCE`/`TextFile`. No external use case. |
| `DirectoryCE` | internal | **Internal** ✓ | Already internal. Wraps `DirectoryInfo` for `MachineWideSharedObject`. |
| `FileCE` | internal | **Internal** ✓ | Already internal. Thin wrapper over `FileInfo`. |
| `TextFile` | internal | **Internal** ✓ | Already internal. Read/write text files. |
| `PathCE` | internal | **Internal** ✓ | Already internal. Single `ReplaceInvalidCharactersWith` method. |

## LinqCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `CartesianProductGenerator` | public | **Internal** | Generates all combinations from multiple lists. Useful algorithm but niche. Primarily used for pluggable component test combinations internally. |
| `ExpressionUtil` | public | **Internal** | `ExtractFinalMemberInfo` from lambda expressions. Framework plumbing for expression analysis. Not general-purpose. |

## ReactiveCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `ObservableCE` | public | **Internal** | Single `Subscribe` extension with `Action<T>`. Trivial convenience wrapper. |
| `SimpleObservable<T>` | public | **Internal** | Lightweight `IObservable<T>` implementation for internal event infrastructure. Couples to `IThreadShared` (internal threading). Not something external users should depend on. |
| `SimpleObserver<T>` | internal | **Internal** ✓ | Already internal. Trivial `IObserver<T>` adapter. |

## ReflectionCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `Constructor` (all nested types) | public | **Internal** | `For<T>.DefaultConstructor`, `WithArguments<T>`, `ForGenericType`, `Compile` — compiled constructor factories via expression trees and IL. Powerful but heavy framework plumbing. Tightly coupled to internal patterns (`IStaticInstancePropertySingleton`). Not general-purpose utility. |
| `TypeCE` | public | **Internal** | `Implements<T>`, `ClassInheritanceChain`, `IsOpenGenericType`, `GetFullNameCompilable`, `Is<T>` — reflection helpers used throughout Compze internals. Some methods are useful but the API surface is shaped around framework needs (e.g., `GetFullNameCompilable`, `ImplementsGenericInterface` caching). |
| `TypeMethods` / `TypeCE.Methods()` | public | **Internal** | `HasMeaningfulToStringOverride()` — very niche. Only useful for serialization/logging internals. |
| `Type<T>` / `TypeTExtensions` | public | **Internal** | `DeclaredType<T>()` returning `Type<T>` with `Operators` property for reflection-based operator access. Clever but niche. Tightly shaped around testing infrastructure (`Must` assertions). |
| `AssemblyBuilderCE` | public | **Internal** | Global shared `ModuleBuilder` for dynamic assembly generation. Pure infrastructure for IL emit. |
| `TypeBuilderCE` | public | **Internal** | `ImplementProperty`, `ImplementConstructor` — IL emit helpers. Pure framework plumbing. |

## TextCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `StringBuilderCE` | public | **Publish** | `AppendInvariant` with custom `InterpolatedStringHandler` for invariant-culture StringBuilder formatting. Well-implemented, fills a genuine gap. |

## ThreadingCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `SyncOrAsyncCE` | public | **Internal** | `AsAsync` for `Func<TResult>` — one-liner Task wrapper. Trivial bridge, only the public overload is meaningful and it's tiny. |
| `IAsyncLockCE` / `AsyncLockCE` | public | **Internal** | Reentrant async lock with timeout + deadlock diagnostics. Complex and well-made, but tightly coupled to internal `LockTimeout`/`WaitTimeout` types from `Compze.Threading`. The API design (nested class implementation) is not polished for general consumption. |
| `AsyncLockTimeoutException` | public | **Internal** | Exception with deferred stack-trace gathering via internal `IAwaitableMonitor`. Coupled to framework threading internals. |
| `MachineWideSharedObject<T>` | public | **Internal** | Cross-process shared state via file + named mutex. Powerful but highly specialized for the DB pool testing infrastructure. Depends on internal IO and threading types. |
| `CorruptionAction` enum | public | **Internal** | Only used by `MachineWideSharedObject`. |

## TransactionsCE

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `TransactionCE` | public | **Publish** | `OnCommittedSuccessfully`, `OnCompleted`, `NoTransactionEscalationScope` — useful general-purpose transaction extensions. Well designed. |
| `TransactionScopeCe` | public | **Publish** | `Execute`, `SuppressAmbient`, `ExecuteAsync` — clean transaction scope wrappers with proper defaults (Serializable, AsyncFlowOption). |
| `VolatileTransactionParticipant` | public | **Publish** | Abstract base class that gets `IEnlistmentNotification` right. The comment says it well: "tricky and failures hard to diagnose." Saves real pain. |
| `VolatileLambdaTransactionParticipant` | public | **Publish** | Lambda-based concrete version. Clean API for transaction hooks without custom classes. |
| `EnlistInAmbientTransactionUsageGuard` | public | **Internal** | Bridges transactions and usage guards. Framework-specific composition. |
| `VolatileLambdaTransactionParticipantExtensions` | internal | **Internal** ✓ | Already internal. |
| `TransactionInterceptorExtensions` (Testing/) | public | **Internal** | `FailOnPrepare` — testing utility for simulating transaction failures. Should be in test infrastructure, not public API. |

## UsageGuards

| Type | Visibility | Verdict | Rationale |
|------|-----------|---------|-----------|
| `IUsageGuard` | public | **Internal** | Interface for validating usage preconditions. Part of the internal component guard system. |
| `UsageGuard<TWrapped>` | public | **Internal** | Generic wrapper invoking guard before access. Framework-specific pattern. |
| `UsageGuard` (abstract) | public | **Internal** | Base class for guards. Framework-specific. |
| `SingleThreadUseGuard` | public | **Internal** | Forces single-thread access. Framework safety net. |
| `SingleTransactionUsageGuard` | public | **Internal** | Forces single-transaction access. Framework safety net. |
| `CombinationUsageGuard` | public | **Internal** | Composes multiple guards. Framework-specific. |
| `MultiThreadedUseException` | public | **Internal** | Exception for `SingleThreadUseGuard`. |
| `ComponentUsedByMultipleTransactionsException` | internal | **Internal** ✓ | Already internal. |

---

## Summary

### Recommended to remain Public (good fit for published NuGet)

These types provide genuine, general-purpose value to external consumers:

1. **`DateTimeCE`** — Safe DateTime handling
2. **`Disposable`** — Action-based IDisposable
3. **`EnumCE`** — Enum validation
4. **`IStaticInstancePropertySingleton<T>`** — Singleton pattern interface
5. **`ScopedChange`** — Temporary state changes
6. **`StringCE`** (both partials) — String utilities + ordinal-safe ops
7. **`TimeSpanCE`** (all partials) — TimeSpan utilities + fluent factory + aggregation
8. **`DictionaryCE`** — Dictionary extensions  
9. **`EnumerableCE`** (main + ForEach + IntSequenceGeneration) — LINQ extensions
10. **`ValidatableObjectCE`** — IValidatableObject helpers
11. **`StringBuilderCE`** — Invariant-culture StringBuilder interpolation
12. **`TransactionCE`** — Transaction extensions
13. **`TransactionScopeCe`** — Transaction scope helpers
14. **`VolatileTransactionParticipant`** — Transaction participation base
15. **`VolatileLambdaTransactionParticipant`** — Lambda-based transaction hooks

### Recommended to move to Internal

These are implementation details, niche infrastructure, or not polished enough for public API:

1. **`CastCE`** — Trivial fluent cast
2. **`CompzeEnvironment`** — CI detection
3. **`DisposableCECollections`** — Single trivial method
4. **`IntCE`** — Two trivial wrappers
5. **`LazyCE<T>`** — Opinionated Lazy replacement, couples to internal threading
6. **`ObjectCE`** — Grab-bag of tiny extensions
7. **`ReentrancyGuard`** — Niche, mutable state
8. **`StringIndenter`** — Unrefined API (magic defaults)
9. **`UncatchableExceptionsGatherer`** — Global mutable testing infrastructure
10. **`CollectionCE`** — Single trivial method
11. **`EnumerableCE.OfTypes`** (both versions) — Niche type-list generation
12. **`LinkedListCE`** — Rarely-used collection type
13. **`CartesianProductGenerator`** — Niche algorithm
14. **`ExpressionUtil`** — Expression tree plumbing
15. **`ObservableCE`** — Trivial convenience wrapper
16. **`SimpleObservable<T>`** — Internal event infrastructure
17. **`Constructor`** (all nested) — Compiled constructor plumbing
18. **`TypeCE`** — Reflection helpers shaped around framework needs
19. **`TypeMethods`** — Niche ToString check
20. **`Type<T>` / `TypeTExtensions`** — Operator reflection for testing
21. **`AssemblyBuilderCE` / `TypeBuilderCE`** — IL emit infrastructure
22. **`SyncOrAsyncCE`** — Trivial async bridge
23. **`IAsyncLockCE` / `AsyncLockCE`** — Coupled to internal threading
24. **`AsyncLockTimeoutException`** — Coupled to internal threading
25. **`MachineWideSharedObject<T>` / `CorruptionAction`** — DB pool testing infrastructure
26. **`EnlistInAmbientTransactionUsageGuard`** — Framework-specific composition
27. **`TransactionInterceptorExtensions`** — Testing utility
28. **`IUsageGuard`** and all UsageGuard types — Framework-internal safety system
29. **`FileSystemInfoCE`** — Infrastructure base class

### Score: ~15 types worth publishing, ~29 types better off internal

The project has a significant amount of pure framework infrastructure mixed in with genuinely useful general-purpose utilities. Moving the infrastructure types to `Compze.Internals.SystemCE.Core` would result in a leaner, more focused public package.
