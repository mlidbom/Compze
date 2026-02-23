# Public API Inventory — Public vs Shared Decision

For each type, put an `x` in one column:
- **Public** — Part of Compze's published API for consumers
- **Shared** — Internal infrastructure, moves to shared source (internal everywhere)
- **Split** — Some members public, some shared. Fill in the member-level table below the type.

**Sample usage** = used by `samples/AccountManagement/src/` (the closest thing to real consumer code).
**Test usage** = used by `test/` or `samples/**/Tests*/` (tests are consumers too).

---

## Compze.Utilities.Functional

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 1 | `unit` | `.Value`, `.From(Action)`, `.Func(Action)` | — | Yes (1) | [ ] | [ ] | [ ] |
| 2 | `Pipe` | `._(func)`, `.tap(action)`, `.mutate(action)`, `.then(value)`, `.assert(pred, msg)`, `.mutateAsync(func)` | — | Yes (7) | [ ] | [ ] | [ ] |
| 3 | `Option` / `Option<T>` | `Option.Some(v)`, `Option.None<T>()` | **Yes** (5) | — | [ ] | [ ] | [ ] |
| 4 | `Some<T>` | `.Value` | **Yes** | — | [ ] | [ ] | [ ] |
| 5 | `None<T>` | `.Instance` | **Yes** | — | [ ] | [ ] | [ ] |
| 6 | `DiscriminatedUnion` | `.AssertValidType(...)`, `InvalidDiscriminatedUnionTypeException` | — | — | [ ] | [ ] | [ ] |
| 7 | `DiscriminatedUnion<...>` (5 arities) | (protected constructors only) | — | — | [ ] | [ ] | [ ] |
| 8 | `ObjectCE` (Functional) | `.Repeat(times)`, `.ToStringNotNull()` | — | Yes (1) | [ ] | [ ] | [ ] |
| 9 | `EnumerableCE` (Functional) | `.OfTypes<T1..T9>()` | — | Yes (8) | [ ] | [ ] | [ ] |

---

## Compze.Utilities.Contracts

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 10 | `Assert` | `.State`, `.Invariant`, `.Argument`, `.Result` | — | Yes (5) | [ ] | [ ] | [ ] |
| 11 | `ContractAsserter` | `.Is(bool)`, `.NotNull(obj)`, `.NotNullOrEmpty(str)`, `.NotNullEmptyOrWhitespace(str)`, `.IsGreaterThan(int)`, `.IsValid<TEnum>(val)`, `.ReturnNotNull<T>(val)`, `.IsNotDisposed(bool, obj)` | — | Yes (1) | [ ] | [ ] | [ ] |
| 12 | `IUsageGuard` | `.EnsureAccessValid()` | — | — | [ ] | [ ] | [ ] |
| 13 | `UsageGuard` | `.EnsureAccessValid()` | — | — | [ ] | [ ] | [ ] |
| 14 | `UsageGuard<TWrapped>` | `.Wrapped` | — | — | [ ] | [ ] | [ ] |
| 15 | `SingleTransactionUsageGuard` | ctor | — | — | [ ] | [ ] | [ ] |
| 16 | `SingleThreadUseGuard` | ctor | — | — | [ ] | [ ] | [ ] |
| 17 | `CombinationUsageGuard` | ctor | — | — | [ ] | [ ] | [ ] |
| 18 | `MultiThreadedUseException` | ctor | — | Yes (2) | [ ] | [ ] | [ ] |
| 19 | `ComponentUsedByMultipleTransactionsException` | ctor | — | — | [ ] | [ ] | [ ] |
| 20 | `InvariantViolatedException` | ctor | — | — | [ ] | [ ] | [ ] |
| 21 | `InvalidResultException` | ctor | — | — | [ ] | [ ] | [ ] |

---

## Compze.Utilities.SystemCE

### Core utilities

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 22 | `ActionCE` | `.From(Func<unit>)`, `.InvokeAll(IEnumerable<Action>)`, `.NullOp` | — | — | [ ] | [ ] | [ ] |
| 23 | `AsyncDisposable` | ctor(Action), ctor(Func\<Task\>), ctor(Func\<ValueTask\>), `.NullOp` | — | — | [ ] | [ ] | [ ] |
| 24 | `CastCE` | `.CastTo<T>(this object)` | — | — | [ ] | [ ] | [ ] |
| 25 | `CompzeEnvironment` | `.IsNCrunch` | — | — | [ ] | [ ] | [ ] |
| 26 | `DateTimeCE` | `.ToUniversalTimeSafely()`, `.ParseInvariant(str)`, `.TimeElapsedSince(dt)` | — | Yes (1) | [ ] | [ ] | [ ] |
| 27 | `Disposable` | ctor(Action), `.Create(Action)`, `.NullOp` | — | — | [ ] | [ ] | [ ] |
| 28 | `DisposableCECollections` | `.DisposeAll(IEnumerable<IDisposable>)` | — | — | [ ] | [ ] | [ ] |
| 29 | `EnumCE` | `.IsValid<T>()`, `.AssertValid<T>()`, `.Values<T>()` | — | — | [ ] | [ ] | [ ] |
| 30 | `GCCE` | `.ForceFullGcAllGenerationsAndWaitForFinalizers()` | — | — | [ ] | [ ] | [ ] |
| 31 | `IntCE` | `.ParseInvariant(str)`, `.ToStringInvariant()` | — | — | [ ] | [ ] | [ ] |
| 32 | `IStaticInstancePropertySingleton<T>` | `static abstract Instance` | **Yes** (1) | Yes (1) | [ ] | [ ] | [ ] |
| 33 | `LazyCE<T>` | `.Value`, `.ValueIfInitialized()`, `.Reset()` | — | — | [ ] | [ ] | [ ] |
| 34 | `NullableCE` | `.NotNull<T>(this T?)` (2 overloads) | — | — | [ ] | [ ] | [ ] |
| 35 | `ObjectCE` (SystemCE) | `.ToStringCE()` | — | — | [ ] | [ ] | [ ] |
| 36 | `ReentrancyGuard` | `.ExecuteIfNotReEntering(Action)`, `.GetAndClearReentryWasAttempted()` | — | — | [ ] | [ ] | [ ] |
| 37 | `RunOnce` | `.IsFirstCall()`, `.RunIfFirstCallAsync(Func<Task>)`, `.RunIfFirstCall(Action)` | — | — | [ ] | [ ] | [ ] |
| 38 | `ScopedChange` | `.Enter(Action, Action)` | — | — | [ ] | [ ] | [ ] |
| 39 | `StringCE` | `.Join()`, `.IsNullEmptyOrWhiteSpace()`, `.RemoveLeadingNewLines()`, `.RemoveLinesWhere()`, `.Pluralize()`, `.ReplaceCE()`, `.ContainsCE()`, `.GetHashcodeCE()`, `.StartsWithCE()`, `.EndsWithCE()`, `.FormatInvariant()`, `.Invariant()` | **Yes** (`.ContainsCE`) | Yes (3) | [ ] | [ ] | [ ] |
| 40 | `StringIndenter` | `.IndentToDepth()`, `.IndentTab()`, `.IndentSpaces()`, `.Indent()`, `.JoinLines()` | — | — | [ ] | [ ] | [ ] |
| 41 | `Throw<TException>` (SystemCE) | `.If(bool)` | — | — | [ ] | [ ] | [ ] |
| 42 | `TimeSpanCE` | `.MultiplyBy()`, `.DivideBy()`, `.ToStringInvariant()`, `.FormatReadable()`, `.None()`, `.Ticks()`, `.Nanoseconds()`, `.Microseconds()`, `.Milliseconds()`, `.Seconds()`, `.Minutes()`, `.Hours()`, `.Days()`, `.Min()`, `.Max()`, `.Sum()`, `.Average()` | — | Yes (27) | [ ] | [ ] | [ ] |
| 43 | `UncatchableExceptionsGatherer` | `.Register(Exception)`, `.Exceptions`, `.ConsumeAndThrowAny...()` | — | Yes (5) | [ ] | [ ] | [ ] |

### ActionFuncHarmonization

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 44 | `ActionToUnitFuncConverter` | `.AsFunc(this Action)` (3 overloads) | — | — | [ ] | [ ] | [ ] |
| 45 | `ActionToUnitFuncConverterAsync` | `.AsFunc(this Func<Task>)` (3 overloads) | — | — | [ ] | [ ] | [ ] |
| 46 | `Func` (UnitFunc) | `.From(Action)`, `.From(Func<Task>)` (6 overloads) | — | — | [ ] | [ ] | [ ] |
| 47 | `UnitFuncToActionConverter` | `.AsAction(this Func<unit>)` (3 overloads) | — | — | [ ] | [ ] | [ ] |

### CollectionsCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 48 | `CollectionCE` | `.RemoveWhere<T>()`, `.AddRange<T>()` | — | — | [ ] | [ ] | [ ] |
| 49 | `DictionaryCE` | `.GetOrAdd<K,V>()`, `.GetOrAddDefault<K,V>()` | — | Yes (2) | [ ] | [ ] | [ ] |
| 50 | `LinkedListCE` | `.ValuesFrom<T>()`, `.AddBefore<T>()`, `.Replace<T>()` | — | — | [ ] | [ ] | [ ] |
| 51 | `ReadonlyCollectionsCE` | `.AddToCopy<T>()`, `.AddRangeToCopy<T>()` (6 overloads) | — | — | [ ] | [ ] | [ ] |

### ComponentModelCE / DataAnnotationsCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 52 | `ValidatableObjectCE` | `.CreateValidationResult()` | **Yes** (1) | — | [ ] | [ ] | [ ] |

### IOCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 53 | `DirectoryCE` | ctor, `.GetOrCreateDirectory()`, `.GetOrCreateTextFile()`, `.TryGetFile()` | — | — | [ ] | [ ] | [ ] |
| 54 | `DirectoryCE.StandardDirectories` | `.LocalApplicationData` | — | — | [ ] | [ ] | [ ] |
| 55 | `FileCE` | ctor, `.GetFileInfo()` | — | — | [ ] | [ ] | [ ] |
| 56 | `FileSystemInfoCE` | `.AbsolutePath`, equality | — | — | [ ] | [ ] | [ ] |
| 57 | `PathCE` | `.ReplaceInvalidCharactersWith()` | — | — | [ ] | [ ] | [ ] |
| 58 | `TextFile` | `.WriteAllText()`, `.ReadAllText()`, `.Create()` | — | — | [ ] | [ ] | [ ] |

### LinqCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 59 | `EnumerableCE` (SystemCE) | `.WhereNotNull()`, `.Create()`, `.None()`, `.ChopIntoSizesOf()`, `.Flatten()`, `.ToReadOnlyList()`, `.ForEach()` (3 overloads), `.Through()`, `.Until()`, `.By()`, `.OfTypes()` (9 arities) | — | Yes (20) | [ ] | [ ] | [ ] |
| 60 | `EnumerableCE.IterationSpecification` | `.StartValue`, `.StepSize`, equality | — | — | [ ] | [ ] | [ ] |
| 61 | `CartesianProductGenerator` | `.CartesianProduct<T>()` | — | — | [ ] | [ ] | [ ] |
| 62 | `ExpressionUtil` | `.ExtractFinalMemberInfo()`, `.ExtractFinalMemberAccessExpression()` | — | — | [ ] | [ ] | [ ] |
| 63 | `Hierarchy` | `.FlattenHierarchy<T>()` | — | Yes (2) | [ ] | [ ] | [ ] |

### ReactiveCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 64 | `ObservableCE` | `.Subscribe<T>(Action<T>)` | — | — | [ ] | [ ] | [ ] |
| 65 | `SimpleObservable<T>` | `.OnNext(T)`, `.Subscribe(IObserver<T>)` | — | — | [ ] | [ ] | [ ] |
| 66 | `SimpleObserver<T>` | ctor(onNext, onError, onCompleted) | — | — | [ ] | [ ] | [ ] |

### ReflectionCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 67 | `Constructor` | `.ForGenericType()`, `.HasDefaultConstructor()`, `.CreateInstance()` | — | Yes (9) | [ ] | [ ] | [ ] |
| 68 | `Constructor.For<T>.DefaultConstructor` | `.Instance` | — | Yes (^) | [ ] | [ ] | [ ] |
| 69 | `Constructor.For<T>.WithArguments<A>` | `.Instance` | — | Yes (^) | [ ] | [ ] | [ ] |
| 70 | `Constructor.GenericTypeConstructor` | `.WithArgument(Type)` | — | Yes (^) | [ ] | [ ] | [ ] |
| 71 | `Constructor.Compile` | `.DefaultInstanceFactory<T>()`, `.ForType<T>()`, `.ForType(Type)`, `.ForGenericType(Type)` | — | Yes (^) | [ ] | [ ] | [ ] |
| 72 | `Constructor.Compile.ConstructorCompiler<T>` | `.DefaultConstructor()`, `.WithArguments<A>()`, `.WithArgument(Type)` | — | Yes (^) | [ ] | [ ] | [ ] |
| 73 | `Constructor.Compile.GenericTypeConstructorCompiler` | `.WithArgument(Type)` | — | Yes (^) | [ ] | [ ] | [ ] |
| 74 | `TypeCE` | `.FullNameNotNull()`, `.Implements<T>()`, `.Implements(Type)`, `.GetGenericInterface()`, `.ListGenericInterfaces()`, `.ImplementsGenericInterface()`, `.ClassInheritanceChain()`, `.InHerits()`, ... (18 methods) | — | — | [ ] | [ ] | [ ] |
| 75 | `TypeCE.TypeMethods` | `.GetToString()`, `.HasMeaningfulToStringOverride()` | — | — | [ ] | [ ] | [ ] |
| 76 | `TypeTExtensions` | `.DeclaredType<T>()` | — | — | [ ] | [ ] | [ ] |
| 77 | `Type<T>` | `.Instance`, `.Operators` | — | — | [ ] | [ ] | [ ] |
| 78 | `Type<T>.TypeOperators` | `.Instance`, `.Equality`, `.InEquality`, `.LessThan`, `.GreaterThan`, `.LessThanOrEqual`, `.GreaterThanOrEqual` | — | — | [ ] | [ ] | [ ] |
| 79 | `AssemblyBuilderCE` | `.Module` | — | Yes (1) | [ ] | [ ] | [ ] |
| 80 | `TypeBuilderCE` | `.ImplementProperty()`, `.ImplementConstructor()` | — | Yes (^) | [ ] | [ ] | [ ] |

### TextCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 81 | `StringBuilderCE` | `.AppendInvariant()` | — | — | [ ] | [ ] | [ ] |
| 82 | `StringBuilderCE.AppendInvariantHandler` | `.AppendLiteral()`, `.AppendFormatted<T>()` (5 overloads) | — | — | [ ] | [ ] | [ ] |

### ThreadingCE (in SystemCE)

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 83 | `ConfigureAwaitCE` | `.caf()` (5 overloads: Task, Task\<T\>, IAsyncDisposable, ValueTask, ValueTask\<T\>) | **Yes** (2) | Yes (2) | [ ] | [ ] | [ ] |
| 84 | `SyncOrAsyncCE` | `.AsAsync<T>(this Func<T>)`, `.AsAsync(this Action)` | — | — | [ ] | [ ] | [ ] |

### TransactionsCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 85 | `EnlistInAmbientTransactionUsageGuard` | `.EnsureAccessValid()` | — | — | [ ] | [ ] | [ ] |
| 86 | `TransactionCE` | `.OnCommittedSuccessfully()`, `.OnCompleted()`, `.NoTransactionEscalationScope()` | — | — | [ ] | [ ] | [ ] |
| 87 | `TransactionScopeCe` | `.SuppressAmbientAndExecuteInNewTransaction()`, `.Execute()`, `.SuppressAmbient()`, `.ExecuteAsync()`, `.SuppressAmbientAsync()` | — | Yes (5) | [ ] | [ ] | [ ] |
| 88 | `VolatileTransactionParticipant` | `.EnsureEnlistedInAnyAmbientTransaction()` | — | — | [ ] | [ ] | [ ] |
| 89 | `VolatileLambdaTransactionParticipant` | `.AddCommitTasks()`, `.AddPrepareTasks()`, `.AddRollbackTasks()` | — | — | [ ] | [ ] | [ ] |
| 90 | `VolatileLambdaTransactionParticipantExtensions` | `.AddCommitTasks(this Transaction)`, `.AddPrepareTasks(this Transaction)` | — | — | [ ] | [ ] | [ ] |
| 91 | `TransactionInterceptorExtensions` | `.FailOnPrepare(this Transaction)` | — | Yes (2) | [ ] | [ ] | [ ] |

---

## Compze.Utilities.SystemCE.ThreadingCE

### Async

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 92 | `IAsyncLockCE` | `.LockedAsync(Func<Task>)`, `.Locked(Action)`, factory methods | — | Yes (1) | [ ] | [ ] | [ ] |
| 93 | `AsyncLockCE` (nested impl) | ctor(TimeSpan), same members | — | Yes (^) | [ ] | [ ] | [ ] |
| 94 | `AsyncLockTimeoutException` | `.Message`, `.SetBlockingThreadsDisposeStackTrace()` | — | Yes (1) | [ ] | [ ] | [ ] |

### ResourceAccess

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 95 | `IMonitorCE` | `.TakeReadLock()`, `.TakeUpdateLock()`, `.TakeReadLockWhen()`, `.TakeUpdateLockWhen()`, `.TryTake...()`, `.Read()`, `.Update()`, `.ReadWhen()`, `.UpdateWhen()`, `.Await()`, `.TryAwait()`, factory methods | — | Yes (4) | [ ] | [ ] | [ ] |
| 96 | `IMonitorCE.MonitorCE` (nested impl) | same members | — | Yes (^) | [ ] | [ ] | [ ] |
| 97 | `IMonitorCE.MonitorCE.LockType` | `Read`, `Update` | — | — | [ ] | [ ] | [ ] |
| 98 | `IMonitorCE.MonitorCE.ThinMonitorWrapper` | `.TryTakeLock()`, `.ReleaseLock()`, `.NotifyWaitingThreadsAboutUpdates()` | — | — | [ ] | [ ] | [ ] |
| 99 | `MonitorCEExtensions` | `.DoubleCheckedLocking()` (2), `.ReadOrUpdate()` (2) | — | — | [ ] | [ ] | [ ] |
| 100 | `TakeLockTimeoutException` | `.Message`, `.SetBlockingThreadsDisposeStackTrace()` | — | Yes (1) | [ ] | [ ] | [ ] |
| 101 | `AwaitingConditionTimeoutException` | ctors | — | Yes (1) | [ ] | [ ] | [ ] |
| 102 | `ISharedObjectSerializer<T>` | `.Serialize(T)`, `.Deserialize(string)` | — | Yes (2) | [ ] | [ ] | [ ] |
| 103 | `IThreadShared` | factory methods: `.WithDefaultTimeouts<T>()`, `.WithTimeouts<T>()` | — | — | [ ] | [ ] | [ ] |
| 104 | `IThreadShared<TShared>` | `.Read()`, `.ReadWhen()`, `.Update()`, `.UpdateWhen()`, `.ReadOrUpdate()`, `.Await()` + default method overloads | — | — | [ ] | [ ] | [ ] |
| 105 | `IThreadShared.LockCEThreadShared<T>` (nested impl) | same members | — | — | [ ] | [ ] | [ ] |
| 106 | `OutReadFunc<TShared, TReturn, TOut>` | delegate | — | — | [ ] | [ ] | [ ] |
| 107 | `MachineWideSharedObject` | (abstract base) | — | Yes (2) | [ ] | [ ] | [ ] |
| 108 | `MachineWideSharedObject<T>` | `.For()`, `.Update()`, `.GetCopy()`, `.Delete()` | — | Yes (^) | [ ] | [ ] | [ ] |
| 109 | `CorruptionAction` | `ThrowException`, `ReplaceContentWithDefaultAndThrow` | — | Yes (2) | [ ] | [ ] | [ ] |

### TasksCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 110 | `TaskCE` | `.Run(Action)`, `.Run<T>(Func<T>)`, `.RunOnDedicatedThread()`, `.ContinueWithCE()`, `.ResultUnwrappingException()`, `.WaitUnwrappingException()` | — | Yes (7) | [ ] | [ ] | [ ] |
| 111 | `TaskUnit` | `.AsUnit(this Task)` | — | — | [ ] | [ ] | [ ] |
| 112 | `TaskCEExceptionHandling` | `.WithAggregateExceptions(this Task)`, `.WithAggregateExceptions(this ValueTask)` | — | Yes (1) | [ ] | [ ] | [ ] |

### Testing

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 113 | `IThreadGateVisitor` | `.AwaitPassThrough()` | — | — | [ ] | [ ] | [ ] |
| 114 | `IThreadGate` | `.Open()`, `.Close()`, `.AwaitLetOneThreadPassThrough()`, `.Set...Action()`, `.ExecuteWithExclusiveLockWhen()`, `.TryAwait()`, properties | — | Yes (18) | [ ] | [ ] | [ ] |
| 115 | `ThreadGate` (impl) | same + `.CreateClosedWithTimeout()`, `.CreateOpenWithTimeout()` | — | Yes (^) | [ ] | [ ] | [ ] |
| 116 | `ThreadGateExtensions` | `.Await()`, `.AwaitClosed()`, `.AwaitQueueLengthEqualTo()`, `.AwaitPassedThroughCountEqualTo()`, `.ThrowPostPassThrough()`, `.FailTransactionOnPreparePostPassThrough()`, `.ThrowOnNextPassThroughAsync()`, `.ExecuteWithExclusiveLockWhenAsync()` | — | — | [ ] | [ ] | [ ] |
| 117 | `IGatedCodeSection` | `.EntranceGate`, `.ExitGate`, `.Enter()` | — | Yes (3) | [ ] | [ ] | [ ] |
| 118 | `GatedCodeSection` (impl) | same + `.WithTimeout()` | — | Yes (^) | [ ] | [ ] | [ ] |
| 119 | `GatedCodeSectionExtensions` | `.Open()`, `.Close()`, `.LetOneThreadEnter()`, `.LetOneThreadEnterAndReachExit()`, `.LetOneThreadPass()`, `.Execute()`, `.IsEmpty()`, `.AssertIsEmpty()` | — | — | [ ] | [ ] | [ ] |
| 120 | `TestingTaskRunner` | `.WithTimeout()`, `.Run(params Action[])` | — | Yes (1) | [ ] | [ ] | [ ] |
| 121 | `ThreadSnapshot` | `.Thread`, `.Transaction` | — | — | [ ] | [ ] | [ ] |
| 122 | `TransactionSnapshot` | `.IsolationLevel`, `.TransactionInformation`, `.TakeSnapshot()` | — | — | [ ] | [ ] | [ ] |
| 123 | `TransactionSnapshot.TransactionInformationSnapshot` | `.LocalIdentifier`, `.DistributedIdentifier`, `.Status` | — | — | [ ] | [ ] | [ ] |
| 124 | `Disposable` (Testing) | ctor(Action) | — | — | [ ] | [ ] | [ ] |

### Other ThreadingCE

| # | Type | Notable members | Sample usage | Test usage | Public | Shared | Split |
|---|------|----------------|--------------|------------|--------|--------|-------|
| 125 | `MutexCE` | `.ExecuteWithLock(Action)`, `.ExecuteWithLock<T>(Func<T>)`, `.ForMutexNamed(string)` | — | — | [ ] | [ ] | [ ] |
| 126 | `ReadonlyCollectionsTE` | `.AddToCopy()`, `.AddRangeToCopy()`, `.AddRange()` (7 overloads) | — | — | [ ] | [ ] | [ ] |
| 127 | `OnlyWithinLocksThreadingHelpers` | `.AddToCopyAndReplace()` (6 overloads) | — | Yes (1) | [ ] | [ ] | [ ] |
| 128 | `ThreadCE` | `.InterruptAndJoin(this Thread)` | — | — | [ ] | [ ] | [ ] |
| 129 | `ThreadPoolCE` | `.TryToEnsureSufficientIdleThreadsToRunTasksConcurrently(int)` | — | — | [ ] | [ ] | [ ] |
| 130 | `Throw<TException>` (ThreadingCE) | `.If(bool)` | — | — | [ ] | [ ] | [ ] |

---

## Split details

When a type is marked **Split**, list its members here:

<!--
Example:
### #42 TimeSpanCE (Split)
| Member | Public | Shared |
|--------|--------|--------|
| `.Seconds(this int)` | [ ] | [ ] |
| `.Minutes(this int)` | [ ] | [ ] |
| `.FormatReadable()` | [ ] | [ ] |
| ... | | |
-->
