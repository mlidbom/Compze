# Compze.Utilities.SystemCE

Extensions and utilities for .NET `System` types in [Compze](https://github.com/mlidbom/Compze).

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

A comprehensive set of extension methods and utility classes enhancing common .NET types — strings, collections, enums, reflection, IO, transactions, reactive, and LINQ.

### Strings

- Ordinal-safe operations — `ContainsCE()`, `StartsWithCE()`, `ReplaceCE()` (avoid culture-dependent defaults)
- `Join()`, `Pluralize()`, `FormatInvariant()`
- `IsNullEmptyOrWhiteSpace()`, `RemoveLeadingNewLines()`, `RemoveLinesWhere()`
- `StringIndenter` — `IndentToDepth()`, `IndentTab()`, `JoinLines()`

### Collections and LINQ

- `ForEach()`, `WhereNotNull()`, `None()`, `ChopIntoSizesOf()`, `Flatten()`
- `ToReadOnlyList()`, immutable-style `AddToCopy()` / `AddRangeToCopy()`
- `GetOrAdd()`, `RemoveWhere()`, `AddRange()`
- `FlattenHierarchy()` for tree structures
- `CartesianProduct()` generator
- Int sequence generation — `1.Through(10)`, `0.Until(5)`, `0.By(2).Through(10)`

### Reflection

- Type inspection — `Implements<T>()`, `ClassInheritanceChain()`, `IsOpenGenericType()`
- Compiled constructor delegates — `Constructor.For<T>.DefaultConstructor`
- Dynamic type building via IL emit

### Time and dates

- Fluent `TimeSpan` factory — `500.Milliseconds()`, `2.Seconds()`, `1.Hours()`
- `TimeSpan` aggregation — `Min()`, `Max()`, `Sum()`, `Average()`
- `DateTime` — `ToUniversalTimeSafely()`, `ParseInvariant()`, `TimeElapsedSince()`

### Transactions

- `TransactionScopeCE.Execute()` — simplified transaction scope usage
- `VolatileLambdaTransactionParticipant` — lambda-based commit/rollback hooks
- `Transaction.OnCommittedSuccessfully()` extension

### Reactive

- `SimpleObservable<T>` — thread-safe `IObservable<T>` implementation

### Other utilities

- `Disposable` / `AsyncDisposable` — action-based disposable implementations
- `LazyCE<T>` — thread-safe lazy with reset capability
- `RunOnce` — atomic one-time execution guard
- `ScopedChange` — temporary state change with IDisposable restore
- `EnumCE` — `IsValid()`, `AssertValid()`, `Values<TEnum>()`
- `CastCE` — `CastTo<T>()` extension
- `UncatchableExceptionsGatherer` — collects finalizer exceptions for later rethrowing

## Installation

```shell
dotnet add package Compze.Utilities.SystemCE
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities.SystemCE.ThreadingCE](https://www.nuget.org/packages/Compze.Utilities.SystemCE.ThreadingCE) | Threading and synchronization |
| [Compze.Utilities.Contracts](https://www.nuget.org/packages/Compze.Utilities.Contracts) | Design-by-contract assertions |
| [Compze.Utilities.Functional](https://www.nuget.org/packages/Compze.Utilities.Functional) | Functional programming primitives |
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |

## License

Apache-2.0
