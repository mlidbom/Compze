# A raw Monitor in lazy initialization

**The rule set aside:** "Always use the locking primitives in Compze.Threading. IMonitor, IAwaitableMonitor, IMutex etc.
NEVER raw lock, Monitor, Mutex etc."

**Where:** `LazyCE<TValue>` in Compze.Threading, in `CreateUnlessAnotherThreadAlreadyHas`. Nowhere else. (Until
2026-11-24 it was in a helper, `DoubleCheckedLockingOnAnExistingObject`, which `LazyCE` called; making a null value
possible gave `LazyCE` a way of its own to tell a created value, and the helper went into it.)

**What it does instead:** double-checked locking that locks, with `Monitor.TryEnter` and `LockTimeout.Default`, on an
object the caller already has: a `LazyCE` locks on itself. The lock is taken only while the value is still being
created; reading it afterwards takes no lock.

**Why:** a `LazyCE` can exist in very large numbers, each created and read once. JAStudio's parser creates one per
candidate word, millions in a run. An `IMonitor` per instance cost ten allocations and 424 bytes, and creating an
instance and reading it once took 55 ns. Locking on the instance itself costs 32 bytes, one allocation, and 14 ns
(BenchmarkDotNet, .NET 10, x64, 2026-11-12). Allocating a lock object on first read instead
(`LazyInitializer.EnsureInitialized` with a `syncLock`) costs 64 bytes and 22 ns, and still uses a raw `Monitor`.
Since `LazyCE` lets go of its factory once the value is created (2026-11-24), it is still 32 bytes: creating an
instance and reading it once took 17.5 ns against 17.2 before, and a later read 0.26 ns against 0.23, both within the
measurement's noise (`Compze.Tests.Benchmarks`, BenchmarkDotNet in process, .NET 11, x64).

**What is lost, and how much of it is kept:**

- The Compze primitives' lock timeout is kept: a thread that waits longer than `LockTimeout.Default` for another
  thread's factory throws a `TimeoutException`, so a deadlock fails loud instead of hanging.
- The blocking thread's stack trace, which `TakeLockTimeoutException` adds to its message, is lost: that needs the
  holder to record its stack trace when it releases the lock, which is the machinery that costs the memory.
- `LazyCE` is public, so code outside it could lock on an instance and share its lock. The documentation of `LazyCE`
  says not to. The alternative, a private lock object, is the allocation this exception exists to avoid.

Its specification, `LazyCE_`, locks on a `LazyCE` with a raw `lock` too: that is the outside code it specifies, which
reading the value for the first time while holding the lock makes fail with an `InvalidOperationException`.

**Approved by:** Magnus, 2026-11-12.
