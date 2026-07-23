# Changelog

All notable changes to Compze.Internals.SystemCE will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

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
