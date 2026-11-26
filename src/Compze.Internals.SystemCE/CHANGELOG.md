# Changelog

All notable changes to Compze.Internals.SystemCE will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.12.0-internal

- **Breaking: `FileReadLettingItBeReplaced`, `LinesFromTheEnd` and `LinesWrittenAfterAPoint` are removed from `Compze.Internals.SystemCE.IOCE`,** as readers for needs too unusual for Compze: their one user, the tooling they came from, keeps them itself. A reader that lets a file be replaced while it reads opens it as `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)`. `FileReplacement` and `IdsOfTheProcessesUsingAFile` stay.

## 0.11.0-internal

- **`FileReplacement.PutInPlaceUnlessThereIsOne(file, by)` writes a file once,** such as a claim that the first of several writers makes: it puts a file in place as `Replace` does, unless there is a file there, and returns false then, leaving both files as they are. Of several writers that put their files in the place at once, one finds none. Outside Windows, `File.Move` without overwriting can't promise that: it looks for a file and then renames, so two writers may both find none. Here the file is linked in place, which fails when there is one.
- **When Windows refuses a file because another process has it open, `FileReplacement`'s failure names who uses each file involved,** each process by its program's name and its id, such as `Windows counts as using it: MsMpEng (PID 4711)`.
- **`FileReplacement.Replace`'s documentation named the wrong exception:** a reader of the file that doesn't let it be deleted makes it throw an `IOException`, for Windows' sharing violation, not an `UnauthorizedAccessException`.
- **`IdsOfTheProcessesUsingAFile.Of(path)` gives the ids of the processes Windows counts as using a file or folder:** those that have it open, as a folder their current directory included, or mapped into their memory, as a program's file is. Windows only.

## 0.10.0-internal

- **Breaking: renamed for Microsoft.VisualStudio.Threading.Analyzers' VSTHRD200,** which asks a method that returns a task to end in Async, and one that doesn't not to: `TaskCE.Run` is `TaskCE.RunAsync`, `TaskCE.RunOnDedicatedThread` is `TaskCE.RunOnDedicatedThreadAsync`, `ContinueWithCE` is `ContinueWithAsync`, `WithAggregateExceptions` is `WithAggregateExceptionsAsync`, and `SyncOrAsyncCE.AsAsync`, which returns a function, is `ToCompletedTaskFunc`.
- **Breaking: `IAsyncLockCE` and `IAsyncShared<TShared>` are no longer `IDisposable`,** as nothing they hold needs disposing: their `SemaphoreSlim` allocates only a wait handle that they never ask for. Disposing one that was held, or waited for, broke its holder's release of it and its waiters, so code that owns one no longer disposes it. Code that did drops its `using` or `Dispose()`.
- **Breaking: `LazyCE<TValue>` moves to Compze.Threading,** in the namespace `Compze.Threading`, unchanged, so that applications can use it from a package meant to be referenced. Code that used it from here adds `using Compze.Threading;`. Compze.Threading is already a dependency of this package.
- **Files that other processes have open meanwhile, in `Compze.Internals.SystemCE.IOCE`:**
  - `FileReplacement.Replace(file, by)` puts a file written beside another in that one's place in one step, so that a reader finds the old file or the new one whole. On Windows it renames with POSIX semantics, which succeeds while readers have the old file open, where `File.Move` fails with "Access to the path is denied"; elsewhere it is `File.Move`, whose rename already does so.
  - `FileReadLettingItBeReplaced` reads a file letting others write, replace or delete it meanwhile, which `File.ReadAllText`, `File.ReadLines` and `File.OpenRead` don't: `AllText`, `Lines` and `Open`.
  - `LinesFromTheEnd` gives the complete lines of a file appended to a line at a time, such as a log, from the last one backwards, reading the file a chunk at a time from its end.
  - `LinesWrittenAfterAPoint` reads the complete lines written after a point in such a file, as UTF-8 bytes in a buffer of its own, and says where the next reading starts; reading on later reads the lines appended since.
  - `ExtendedLengthPath.Of(path)` gives a path as Windows' own file functions take it at any length, `\\?\` and the full path, for code that calls them directly. Windows only.
  For both line readers a line ends in LF or CRLF, and a byte order mark at the start of the file isn't part of its first line.
- **Requires newer minor versions of other Compze packages:** Compze.Threading 0.13.0-alpha (was 0.12.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.9.0-internal

- **Internal support for keeping a memory image state store's files and a database connection out of one transaction; nothing public changes:** a record of the durable resources taking part in each transaction, through which a resource that can't share its transaction with another refuses it. The new versions of Compze.Sql.Common and Compze.Teventive.MemoryImageStateStore require this one.

## 0.8.0-internal

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Threading 0.12.0-alpha (was 0.11.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.7.0-internal

- `ThreadPoolCE.DescribeState()`: the thread pool's state in one line, for the message of a wait that timed out, where the question is whether the pool starved. It replaces two descriptions of the same thing, in Compze.Tessaging's named-pipe connection pool and in the test infrastructure, and Compze.Threading.Testing's gates use it in their timeouts.
- **Requires newer minor versions of other Compze packages:** Compze.Threading 0.11.0-alpha (was 0.10.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.6.2-internal

- Fixed: `StringCE.RemoveLinesWhere` and `StringIndenter.IndentToDepth` split a string at every line ending, LF and CRLF alike, and end the lines they return in `Environment.NewLine`. They split only at `Environment.NewLine`, so on Windows, text from a raw string literal in a package compiled on Linux, whose lines end in LF, was one line to them: Compze.Must's failure messages showed the placeholder for a line to leave out, `RemoveLine`, and Compze.Internals.Logging's exception messages, which indent with `IndentToDepth`, indented only their first line of such text.

## 0.6.1-internal

- `SimpleObservable` takes no lock: subscribing and disposing a subscription swap in a new immutable array of subscribers, trying again when another thread swapped in one of its own first. An observable nobody has subscribed to is one 24-byte object instead of 416 bytes, its own `IMonitor` included, which matters to code that gives each of a great many objects one, such as every `Taggregate`. Notifying the subscribers no longer allocates.

## 0.6.0-internal

- **`TaskCE.RunOnDedicatedThread`: blocking work an async caller awaits runs on a thread of its own.** For a wait on a synchronization primitive with no async form, which `TaskCE.Run` would park on a thread-pool thread. The pool keeps about one thread per processor and adds more slowly, so each parked pool thread leaves one fewer for every async continuation in the process; a few parked at once, and continuations wait seconds for a thread.

## 0.5.0-internal

- `LazyCE`: a factory that reads the value it is creating, directly or through another `LazyCE`, makes the read throw an `InvalidOperationException`, as `Lazy<T>` does. The lock lets the thread holding it enter again, so the factory ran again and again until the stack overflowed, which ends the process. Reading the value for the first time while holding a lock on the instance from outside, which the documentation forbids, throws the same exception.

## 0.4.2-internal

- `LazyCE` is cheap enough to create in large numbers: an instance is one 32-byte object instead of ten allocations and 424 bytes, and creating one and reading it once takes about 14 ns instead of 55. It no longer allocates an `IMonitor` of its own, but locks on itself, and only while the value is still being created. It still runs its factory once when threads read the value for the first time at the same moment. A thread that waits longer than `LockTimeout.Default` for another thread's factory now throws a `TimeoutException`, without the blocking thread's stack trace that `TakeLockTimeoutException` carried. `LazyCE` is sealed, since it is its own lock, and documented: don't lock on an instance from outside.

## 0.4.1-internal

- Fixed `SimpleObservable` calling its subscribers while holding its lock. Subscribing from inside a notification threw "Collection was modified", and a thread subscribing or disposing a subscription waited for every subscriber of an in-progress notification on another thread to finish — up to the lock timeout, then threw. The subscribers are now an immutable list that subscribing and disposing replace (copy-on-write), and `OnNext` notifies the list that was current when it was called, with no lock held. So a subscription disposed during a notification may still receive that notification, and notifications published from different threads are no longer serialized — as with Rx's `Subject<T>`, that is the publisher's job.
- Fixed `SimpleObservable` collapsing repeated subscriptions of the same observer into one: disposing either subscription silently ended both. Each subscription is now independent, and subscribers are notified in the order they subscribed.

## 0.4.0-internal

- `TransactionScopeCe.ExecuteAsync` is public and gains a result form: a transaction scope with async flow enabled around awaited work — the core of the async unit-of-work envelope.
- `TaskCE.Run(Func<Task>)`: starts async work on a thread-pool thread — guaranteed not inline on the caller — returning a task that completes when the whole async work does, not when its synchronous prefix returns.
- **`SingleThreadUseGuard` and `MultiThreadedUseException` are deleted.** Thread identity stopped being an invariant of correct executions: an async unit of work legitimately migrates across pool threads, with its transaction — which flows across awaits — as its identity. The misuse that fails loud is one component serving two transactions: `SingleTransactionUsageGuard` remains, and `ComponentUsedByMultipleTransactionsException` goes public as the surface consumers now meet. Knowingly lost: loud detection of a component shared across genuinely parallel transactionless work — the thread-era proxy has no async-era equivalent short of enter/exit bracketing.
- `Transaction.OnCompletedWithoutCommitting(action)` (`TransactionsCE`): runs the action when the transaction completes any way but a successful commit — the mirror of `OnCommittedSuccessfully`, so registering with both covers every outcome exactly once. What lets a reservation taken inside a transaction be released exactly when the transaction fails to commit (Compze.Tessaging's best-effort queue bound). `TransactionCE` also converted to the extension-block syntax.
- **The prune**: the utilities nothing in the solution calls are deleted, `CollectionCE` among them, and `UsageGuard` goes with the thread-affinity guarding above.
- `IAsyncShared` and `IAsyncShared<TShared>`: the async counterparts of the shared-state containers. `ThreadCE` joins the public surface.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-internal

- `ProcessIdentity` (`DiagnosticsCE`): identifies a specific OS process well enough that any process on the machine can ask whether it is still running — the process id plus its start time (the disambiguator that detects process id reuse), with cross-OS-tolerant start-time equality (on Unix a process's start time is reconstructed from a per-reader boot-time estimate, so exact tick equality is a Windows-only luxury). What the same-machine endpoint registry records for each announcing process.
- `IBinaryFile` is `IDisposable`: a binary file abstraction whose implementation holds an OS handle must be disposable — disposing an `IInterprocessObject` now releases its backing file's memory mapping through this.
- Added `SetInCopy` to the read-only-dictionary copy helpers: like `AddToCopy` but overwrites when the key is already present.
- Fixed `Constructor.GenericTypeConstructor`: its compiled-constructor cache was shared across all instances and keyed only by the argument type, so closing two different generic type definitions over the same argument type handed the second caller the first one's constructor. Concretely: a `DogTaggregate` inheriting `AnimalTaggregate` published its tevents wrapped in the `CatTevent<>` wrapper if a cat had published first.

## 0.2.1-internal

- Refactoring.

## 0.2.1-alpha

- Refactoring.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
